using System;
using UnityEngine;
namespace TreasureRunner
{
    [Serializable] public class IdentityConfig
    {
        public bool enabled;public string firebaseProjectId,firebaseAppId,firebaseApiKey,googleWebClientId;
        public bool Ready=>enabled && !string.IsNullOrWhiteSpace(firebaseProjectId) && !string.IsNullOrWhiteSpace(firebaseAppId) && !string.IsNullOrWhiteSpace(firebaseApiKey) && googleWebClientId!=null && googleWebClientId.EndsWith(".apps.googleusercontent.com",StringComparison.Ordinal);
    }
    public enum IdentityState { Unconfigured,Guest,Working,NeedsNickname,Ready,Error }
    public class GoogleIdentity:MonoBehaviour
    {
        [Serializable] class Reply {public int request;public string state,uid,nickname,error;}
        static GoogleIdentity instance;public static event Action Changed;
        public static GoogleIdentity Current {get{if(instance==null)new GameObject("TreasureRunner Identity").AddComponent<GoogleIdentity>();return instance;}}
        public IdentityState State {get;private set;}=IdentityState.Unconfigured;
        public string Nickname {get;private set;}="";public string PlayerId {get;private set;}="";
        public string Status {get;private set;}="Google-Anmeldung ist noch nicht eingerichtet.";
        IdentityConfig config;int request;float deadline;bool awaiting;
        public bool Busy=>State==IdentityState.Working;
        public bool HasSession=>!string.IsNullOrEmpty(PlayerId);
        public bool Supported=>Application.platform==RuntimePlatform.Android;
        public bool Configured=>config!=null && config.Ready;
        void Awake()
        {
            if(instance!=null){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);
            try{var text=Resources.Load<TextAsset>("IdentityConfig");config=text!=null ? JsonUtility.FromJson<IdentityConfig>(text.text) : null;}catch(ArgumentException){}
            if(!Configured)return;
            if(!Supported){State=IdentityState.Guest;Status="Google-Anmeldung ist auf Android verfügbar.";return;}
            Begin("Anmeldung wird wiederhergestellt …");Call("initialize",JsonUtility.ToJson(config));
        }
        void Begin(string text){request++;awaiting=true;deadline=Time.unscaledTime+90;State=IdentityState.Working;Status=text;Changed?.Invoke();}
        public void SignIn()
        {
            if(Busy || !Configured || !Supported || ProgressStore.IsReadOnly)return;
            Begin("Google-Konto auswählen …");Call("signIn","");
        }
        public void SaveNickname(string value)
        {
            if(!NicknamePolicy.Valid(value)){Status=NicknamePolicy.Hint;Changed?.Invoke();return;}
            if(Busy || !HasSession || !Configured || !Supported || ProgressStore.IsReadOnly)return;
            Begin("Nickname wird gespeichert …");Call("saveNickname",value);
        }
        public void SignOut()
        {
            if(!Configured || !Supported)return;
            request++;awaiting=true;Call("signOut","");PlayerId=Nickname="";State=IdentityState.Guest;Status="Abgemeldet. Dein lokaler Spielstand bleibt erhalten.";Changed?.Invoke();
        }
        void Call(string method,string value)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var bridge=new AndroidJavaClass("com.treasurerunner.identity.IdentityBridge"))bridge.CallStatic(method,activity,gameObject.name,request,value);}
            catch(AndroidJavaException){Fail("configuration");}
#else
            Fail("platform");
#endif
        }
        public void OnIdentityResult(string json)
        {
            // Never accept or display provider names, email, avatars or raw error strings.
            if(json==null || json.Length>2048)return;Reply reply;try{reply=JsonUtility.FromJson<Reply>(json);}catch(ArgumentException){return;}
            if(reply==null || !awaiting || request<=0 || reply.request!=request)return;
            awaiting=false;
            if(reply.state=="guest"){PlayerId=Nickname="";State=IdentityState.Guest;Status="Nicht angemeldet.";}
            else if((reply.state=="nickname" || reply.state=="ready") && ValidId(reply.uid))
            {
                if(reply.state=="ready" && !NicknamePolicy.Valid(reply.nickname)){Fail("profile");return;}
                PlayerId=reply.uid;Nickname=reply.state=="ready" ? reply.nickname : "";State=reply.state=="ready" ? IdentityState.Ready : IdentityState.NeedsNickname;
                Status=State==IdentityState.Ready ? "Angemeldet als "+Nickname : "Wähle jetzt deinen eigenen Nickname. Dein Google-Name wird nicht übernommen.";
                if(State==IdentityState.Ready && !ProgressStore.IsReadOnly){ProgressStore.Data.playerName=Nickname;ProgressStore.Save();}
            }
            else{Fail(reply.error);return;}
            Changed?.Invoke();
        }
        static bool ValidId(string id)=>id!=null && System.Text.RegularExpressions.Regex.IsMatch(id,@"\A[A-Za-z0-9_-]{1,128}\z");
        void Fail(string code)
        {
            awaiting=false;
            State=HasSession ? IdentityState.NeedsNickname : IdentityState.Error;
            Status=code=="cancelled" ? "Anmeldung abgebrochen." : code=="network" ? "Keine Verbindung. Bitte später erneut versuchen." : code=="profile" ? "Profil konnte nicht geladen oder gespeichert werden." : code=="configuration" ? "Google-Anmeldung ist noch nicht vollständig eingerichtet." : code=="platform" ? "Google-Anmeldung ist auf Android verfügbar." : "Anmeldung nicht möglich. Bitte erneut versuchen.";
            Changed?.Invoke();
        }
        void Update(){if(Busy && Time.unscaledTime>deadline){request++;Call("signOut","");PlayerId=Nickname="";Fail("network");}}
        void OnDestroy(){if(instance==this)instance=null;}
#if UNITY_EDITOR
        public void TestRequest(int value){request=value;awaiting=true;}
#endif
    }
}
