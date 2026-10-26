#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class IdentityChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/GoogleIdentity");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");var hashes=Directory.GetFiles("Builds/Publish-0.8.0").Where(p=>p.EndsWith(".apk") || p.EndsWith(".zip")).ToDictionary(p=>p,p=>PatchCodec.Hash(File.ReadAllBytes(p)));
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;bool failed=false;var scenarios=Scenarios();
            while(true){bool more;object item=null;try{more=scenarios.MoveNext();if(more)item=scenarios.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return item;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Log;
            try{Check(saved==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real save and backup unchanged");Check(hashes.All(p=>PatchCodec.Hash(File.ReadAllBytes(p.Key))==p.Value),"Published APK, Windows and source archives unchanged");Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"No app or Android version change");}catch(Exception e){failed=true;report.Add("FAIL: "+e);}
            failed|=errors;report.Add(failed ? "IDENTITY CHECKS FAILED" : "ALL IDENTITY CHECKS PASSED");File.WriteAllLines("Logs/GoogleIdentity/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            foreach(string value in new[]{"Schatzjäger","Held_123","Mia-Luna","Özlem 7","abc",new string('a',20)})Check(NicknamePolicy.Valid(value),"Valid chosen nickname accepted: "+value);
            foreach(string value in new[]{null,"","ab",new string('a',21)," person","person ","   ","___","a@b.de","https://site","<b>Hero</b>","a\nb","a\rb","a\tb","a\u202eb","玩家猫"})Check(!NicknamePolicy.Valid(value),"Unsafe, email-like or invalid nickname rejected");
            var configuration=JsonUtility.FromJson<IdentityConfig>(Resources.Load<TextAsset>("IdentityConfig").text);Check(configuration.Ready && configuration.firebaseProjectId=="treasurerunner-2689b" && !new IdentityConfig().Ready,"Real owner configuration is ready; empty configuration stays disabled");
            var identity=GoogleIdentity.Current;Check(identity.State==IdentityState.Guest && !identity.HasSession && !identity.Supported,"Editor does not fabricate a Google login");string before=JsonUtility.ToJson(ProgressStore.Data);identity.SignIn();Check(before==JsonUtility.ToJson(ProgressStore.Data) && identity.State==IdentityState.Guest,"Unsupported editor login leaves all progress untouched");
            identity.OnIdentityResult("{\"request\":0,\"state\":\"ready\",\"uid\":\"sampleuid\",\"nickname\":\"BadHero\"}");Check(!identity.HasSession,"Unsolicited callback cannot sign in");
            identity.TestRequest(1);identity.OnIdentityResult("{\"request\":2,\"state\":\"ready\",\"uid\":\"sampleuid\",\"nickname\":\"StaleHero\"}");Check(!identity.HasSession,"Mismatched callback cannot sign in");
            identity.OnIdentityResult("{\"request\":1,\"state\":\"nickname\",\"uid\":\"sampleuid\",\"email\":\"private@example.org\",\"displayName\":\"Private Google Name\"}");Check(identity.State==IdentityState.NeedsNickname && identity.Nickname=="" && before==JsonUtility.ToJson(ProgressStore.Data),"New account requires explicit nickname and never imports provider identity");
            identity.TestRequest(2);identity.OnIdentityResult("{\"request\":2,\"state\":\"ready\",\"uid\":\"sampleuid\",\"nickname\":\"private@example.org\"}");Check(identity.State==IdentityState.NeedsNickname && identity.Nickname=="" && before==JsonUtility.ToJson(ProgressStore.Data),"Invalid server nickname cannot leak an email to progress");
            identity.TestRequest(3);identity.OnIdentityResult("{\"request\":3,\"state\":\"ready\",\"uid\":\"sampleuid\",\"nickname\":\"TestHeld\",\"email\":\"private@example.org\",\"displayName\":\"Private Google Name\"}");Check(identity.State==IdentityState.Ready && identity.Nickname=="TestHeld" && ProgressStore.Data.playerName=="TestHeld","Only chosen nickname reaches displayed player name");
            identity.OnIdentityResult("{\"request\":3,\"state\":\"ready\",\"uid\":\"changeduid\",\"nickname\":\"ReplayHero\"}");Check(identity.PlayerId=="sampleuid" && identity.Nickname=="TestHeld","Completed callbacks cannot be replayed");
            var camera=new GameObject("Menu Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.cullingMask &= ~(1<<30);var home=new GameObject("Home").AddComponent<HomeScreen>();home.Build();yield return null;home.OpenPlayerMenu();yield return null;yield return null;var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Profil");yield return null;Canvas.ForceUpdateCanvases();
            var panel=FindFirstObjectByType<GoogleAccountPanel>();Check(panel!=null && panel.GetComponentsInChildren<InputField>().Single().text=="TestHeld","Actual account panel displays selected nickname only");Check(!FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("private@example.org") || t.text.Contains("Private Google Name")),"Google email and profile name absent from all UI text");
            var column=(RectTransform)panel.transform.GetChild(0);Check(LayoutUtility.GetPreferredHeight(column)<=column.rect.height+2,"Nickname onboarding controls fit the account card");Capture(camera,"Nickname-Konto");
            identity.TestRequest(4);identity.OnIdentityResult("{\"request\":4,\"state\":\"guest\"}");yield return null;Check(!identity.HasSession && ProgressStore.Data.playerName=="TestHeld","Signing out retains chosen local nickname and campaign progress");
            menu.Show("Profil");yield return null;var button=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Mit Google anmelden");Check(!button.interactable,"Android login is not advertised as working in Unity editor");
            var local=FindObjectsByType<InputField>(FindObjectsSortMode.None).Single();local.onEndEdit.Invoke("private@example.org");Check(ProgressStore.Data.playerName=="TestHeld","Local profile input also rejects email-like nickname");local.onEndEdit.Invoke("WaldHeld");Check(ProgressStore.Data.playerName=="WaldHeld","Local nickname remains editable without a Google account");Capture(camera,"Google-Noch-Nicht-Konfiguriert");
            identity.TestRequest(5);identity.OnIdentityResult("{\"request\":5,\"state\":\"error\",\"error\":\"private@example.org Private Google Name\"}");Check(!identity.Status.Contains("private@example.org") && !identity.Status.Contains("Private Google Name"),"Provider error text is never displayed verbatim");
            string native=File.ReadAllText("Assets/Plugins/Android/TreasureIdentity.androidlib/src/com/treasurerunner/identity/IdentityBridge.java");Check(!native.Contains("getEmail(") && !native.Contains("getDisplayName(") && !native.Contains("getPhotoUrl(") && !native.Contains("printStackTrace") && !native.Contains("Log."),"Native bridge never reads or logs Google profile fields");Check(native.Contains("signInWithCredential") && native.Contains("Source.SERVER") && native.Contains("\"nickname\""),"Native bridge uses server-verified credentials and separate nickname profile");
            Check(!JsonUtility.ToJson(ProgressStore.Data).Contains("private@example.org") && !JsonUtility.ToJson(ProgressStore.Data).Contains("Private Google Name"),"No provider identity leaked to persisted progress");
        }
        void Capture(Camera camera,string name)
        {
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var old=canvases.Select(c=>c.worldCamera).ToArray();var target=new RenderTexture(2340,1080,24);var previous=camera.targetTexture;camera.targetTexture=target;foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(2340,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2340,1080),0,0);image.Apply();File.WriteAllBytes("Logs/GoogleIdentity/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=previous;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=old[i];}target.Release();Destroy(target);Destroy(image);
        }
    }
}
#endif
