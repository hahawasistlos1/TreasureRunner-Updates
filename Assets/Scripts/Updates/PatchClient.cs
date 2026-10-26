using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class BoundedPatchDownload:DownloadHandlerScript
    {
        readonly MemoryStream buffer=new MemoryStream();readonly int limit;public int Received=>(int)buffer.Length;
        public BoundedPatchDownload(int max):base(new byte[8192]){limit=max;}
        protected override bool ReceiveData(byte[] data,int length){if(data==null || length<0 || buffer.Length+length>limit)return false;buffer.Write(data,0,length);return true;}
        protected override byte[] GetData()=>buffer.ToArray();
    }
    public class PatchClient:MonoBehaviour
    {
        public const string RootUrl="https://raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/TennoFarm/patches/";
        public static string ChannelRoot(string version)=>RootUrl+Uri.EscapeDataString(version)+"/";
        public static PatchClient Instance {get;private set;}
        public string LastStatus {get;private set;}="Spielpatches noch nicht geprüft.";
        public bool Busy {get;private set;}public float Progress {get;private set;}
        float nextCheck;bool enabledChannel;PatchPackage pending;UnityWebRequest request;
        public PatchManifest AvailablePatch {get;private set;}
#if UNITY_EDITOR
        public static string TestRoot;
#endif
        string SourceRoot
        {
            get{
#if UNITY_EDITOR
                if(TestRoot!=null && Uri.TryCreate(TestRoot,UriKind.Absolute,out var test) && test.Scheme=="http" && test.Host=="127.0.0.1")return TestRoot;
#endif
                return ChannelRoot(Application.version);
            }
        }
        void Awake(){if(Instance!=null){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);PatchRuntime.Initialize();}
        IEnumerator Start()
        {
            if(DesktopTools.Smoke){enabledChannel=false;yield break;}
#if UNITY_EDITOR
            if(ProgressStore.IsMemoryTest){enabledChannel=TestRoot!=null;nextCheck=float.PositiveInfinity;yield break;}
#endif
            var config=Resources.Load<TextAsset>("UpdateChannel");UpdateChannel channel=null;try{channel=config!=null ? JsonUtility.FromJson<UpdateChannel>(config.text) : null;}catch(Exception){}
            enabledChannel=channel!=null && channel.enabled && channel.owner=="hahawasistlos1" && channel.repository=="TreasureRunner-Updates";
            if(!enabledChannel){LastStatus="Spielpatch-Kanal ist nicht aktiviert.";yield break;}
            nextCheck=Time.unscaledTime+2;yield return null;
        }
        void Update()
        {
            if(LevelRun.Instance!=null || DesktopTools.Active)return;
            if(pending!=null){var package=pending;pending=null;Activate(package);return;}
            if(enabledChannel && !Busy && Time.unscaledTime>=nextCheck)CheckNow();
        }
        public void CheckNow(){if(Busy || pending!=null || !enabledChannel)return;StartCoroutine(Check());}
        void OnApplicationFocus(bool focused){if(focused)nextCheck=Mathf.Min(nextCheck,Time.unscaledTime+2);}
        void OnApplicationPause(bool paused){if(!paused)nextCheck=Mathf.Min(nextCheck,Time.unscaledTime+2);}
        void NotifyPopup(bool announce=false){if(AvailablePatch!=null)GameUpdateClient.Instance?.PresentPatch(AvailablePatch,LastStatus,Progress,announce);}
        IEnumerator Check()
        {
            Busy=true;Progress=0;nextCheck=Time.unscaledTime+300;LastStatus="Suche kleine Spielpatches …";PatchManifest manifest=null;
            using(request=Get(SourceRoot+"manifest.json?t="+DateTimeOffset.UtcNow.ToUnixTimeSeconds(),PatchCodec.MaxManifestBytes))
            {
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){LastStatus=request.responseCode==404 ? "Noch keine kleinen Spielpatches veröffentlicht." : "Spielpatches offline – vorhandener Stand bleibt spielbar.";Finish();yield break;}
                try{manifest=JsonUtility.FromJson<PatchManifest>(new UTF8Encoding(false,true).GetString(request.downloadHandler.data));}catch(Exception){}
            }
            if(!PatchCodec.Manifest(manifest,Application.version,out string reason)){LastStatus="Kein passender Spielpatch für diese App-Version.";Finish();yield break;}
            if(manifest.revision<=Mathf.Max(PatchRuntime.Revision,PatchRuntime.BlockedThrough)){LastStatus=manifest.revision<=PatchRuntime.BlockedThrough ? "Dieser Spielpatch ist als beschädigt gesperrt. Warte auf die nächste Korrektur." : "Spielpatch-Stand ist aktuell.";Finish();yield break;}
            AvailablePatch=manifest;LastStatus="Patch "+manifest.revision+" wird automatisch heruntergeladen …";NotifyPopup(true);PatchPackage package=null;
            using(request=Get(SourceRoot+manifest.payloadFile,manifest.payloadBytes))
            {
                var operation=request.SendWebRequest();var download=(BoundedPatchDownload)request.downloadHandler;
                while(!operation.isDone){Progress=Mathf.Clamp01(download.Received/(float)manifest.payloadBytes);LastStatus="Patch-Download "+Mathf.FloorToInt(Progress*100)+" %";NotifyPopup();yield return null;}
                if(request.result!=UnityWebRequest.Result.Success){LastStatus="Spielpatch-Download fehlgeschlagen. Bisheriger Stand bleibt aktiv.";Finish();yield break;}
                try{package=new PatchPackage{manifest=manifest,payload=new UTF8Encoding(false,true).GetString(request.downloadHandler.data)};}catch(Exception){}
            }
            if(!PatchCodec.Package(package,Application.version,out _,out reason)){LastStatus="Spielpatch abgelehnt: "+reason;Finish();yield break;}
            Progress=1;
            if(LevelRun.Instance!=null){pending=package;LastStatus="Patch heruntergeladen und geprüft – wird automatisch im Hauptmenü aktiviert.";Finish();yield break;}
            Activate(package);
            Finish();
        }
        UnityWebRequest Get(string url,int maximum)
        {var value=new UnityWebRequest(url,UnityWebRequest.kHttpVerbGET);value.downloadHandler=new BoundedPatchDownload(maximum);value.redirectLimit=0;value.timeout=20;value.SetRequestHeader("User-Agent","TreasureRunner-DataPatches");return value;}
        void Finish(){request=null;Busy=false;NotifyPopup();}
        void Activate(PatchPackage package)
        {
            if(!PatchRuntime.Install(package,out string reason)){LastStatus=reason;NotifyPopup();return;}
            LastStatus="Patch "+PatchRuntime.Revision+" automatisch angewendet. Du kannst weiterspielen.";NotifyPopup();
        }
        void OnDestroy(){request?.Abort();if(Instance==this)Instance=null;}
    }
}
