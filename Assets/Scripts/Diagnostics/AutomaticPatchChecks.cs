#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class AutomaticPatchChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;PatchTestServer server;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Logged(string text,string stack,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        RectTransform Overlay=>(RectTransform)typeof(GameUpdateClient).GetField("overlay",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(GameUpdateClient.Instance);
        Text[] PopupTexts()=>Overlay.GetComponentsInChildren<Text>();
        Button Button(string text)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==text);
        PatchPackage Package(int revision)
        {var data=new DataPatch{revision=revision,appVersion=Application.version,weapons=new[]{new PatchWeapon{id="bow",damage=2,cooldown=1.05f}}};string payload=JsonUtility.ToJson(data);return new PatchPackage{payload=payload,manifest=new PatchManifest{revision=revision,appVersion=Application.version,title="Trefferkorrektur "+revision,changes="Bogen-Trefferflächen korrigiert.\nWaffenpause angepasst.",payloadFile="patch-"+revision+".json",payloadBytes=Encoding.UTF8.GetByteCount(payload),sha256=PatchCodec.Hash(Encoding.UTF8.GetBytes(payload))}};}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/AutomaticPatchFlow");
            string save=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");var artifacts=Directory.GetFiles("Builds/Publish-0.8.0").Where(p=>p.EndsWith(".apk") || p.EndsWith(".zip")).ToDictionary(p=>p,p=>PatchCodec.Hash(File.ReadAllBytes(p)));
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;bool failed=false;var scenarios=Scenarios();
            while(true){bool more;object item=null;try{more=scenarios.MoveNext();if(more)item=scenarios.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return item;}
            server?.Dispose();PatchClient.TestRoot=null;PatchRuntime.SetTestStore(null);ProgressStore.EndMemoryTest();Time.timeScale=1;Application.logMessageReceived-=Logged;
            try{Check(save==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real save and backup untouched");Check(artifacts.All(p=>PatchCodec.Hash(File.ReadAllBytes(p.Key))==p.Value),"Published APK, Windows and source archives unchanged");Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App and Android versions unchanged");}catch(Exception e){failed=true;report.Add("FAIL: "+e);}
            failed|=errors;report.Add(failed ? "AUTOMATIC PATCH CHECKS FAILED" : "ALL AUTOMATIC PATCH CHECKS PASSED");File.WriteAllLines("Logs/AutomaticPatchFlow/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            Check(typeof(PatchRuntime).GetMethod("Rollback")==null && typeof(PatchStore).GetMethod("Rollback")==null,"No manual patch withdrawal API");
            Check(!File.Exists("Assets/Scripts/UI/PatchStatusPanel.cs"),"Separate patch settings component removed");
            var store=new PatchStore(Path.GetFullPath("Logs/AutomaticPatchFlow/Temp/"+Guid.NewGuid().ToString("N")),Application.version);store.Load();PatchRuntime.SetTestStore(store);server=new PatchTestServer{PayloadDelay=300,ChunkDelay=700};server.Publish(Package(1));PatchClient.TestRoot=server.Root;
            var camera=new GameObject("Menu Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;camera.cullingMask &= ~(1<<30);
            var patch=new GameObject("Automatic Patches").AddComponent<PatchClient>();var home=new GameObject("Home").AddComponent<HomeScreen>();home.Build();yield return null;yield return null;yield return null;
            var updates=GameUpdateClient.Instance;patch.SendMessage("OnApplicationFocus",true);float deadline=Time.realtimeSinceStartup+12;
            while((patch.AvailablePatch==null || !patch.Busy) && Time.realtimeSinceStartup<deadline)yield return null;
            Check(patch.Busy && patch.AvailablePatch?.revision==1,"Resume triggers automatic manifest check and download without pressing a button");
            Check(Overlay!=null && Overlay.parent.gameObject.activeSelf && Overlay.parent.name=="Spielupdate","Patch uses existing update window during download");
            Check(PopupTexts().Any(t=>t.text=="PATCH · "+Application.version+" / 1") && PopupTexts().Any(t=>t.text.Contains("Was wurde gepatcht?") && t.text.Contains(Package(1).manifest.changes)),"Patch-only title and exact change notes shown before activation");
            Check(Overlay.GetComponentsInChildren<Button>().Length==1 && Overlay.GetComponentsInChildren<Button>().All(b=>b.GetComponentInChildren<Text>().text=="Schließen"),"Patch popup offers only close; no install, decline or withdrawal");
            bool intermediate=false;while(patch.Busy && Time.realtimeSinceStartup<deadline){intermediate|=patch.Progress>0 && patch.Progress<1;if(intermediate)break;yield return null;}
            Check(intermediate,"Real chunked HTTP payload advances fractional progress");Capture(camera,"Patch-Download");Button("Schließen").onClick.Invoke();Check(!Overlay.parent.gameObject.activeSelf && patch.Busy,"Closing popup leaves automatic download running");
            while(patch.Busy && Time.realtimeSinceStartup<deadline)yield return null;
            Check(!patch.Busy && PatchRuntime.Revision==1 && patch.Progress==1,"Closed popup download verifies and applies without confirmation");
            Check(!Overlay.parent.gameObject.activeSelf,"Completion does not reopen a deliberately closed popup");
            updates.OpenStatus();yield return null;Check(Overlay.parent.gameObject.activeSelf && PopupTexts().Any(t=>t.text.Contains("automatisch angewendet")),"Common update-status action reopens applied patch notes");Capture(camera,"Patch-Angewendet");Button("Schließen").onClick.Invoke();
            var reloaded=new PatchStore(store.DirectoryPath,Application.version);reloaded.Load();Check(reloaded.Revision==1 && !reloaded.Install(Package(1),out _),"Installed patch persists and equal revision cannot downgrade it");
            home.OpenPlayerMenu();yield return null;var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Updates");yield return null;Canvas.ForceUpdateCanvases();
            Check(!FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="KLEINE SPIELPATCHES" || t.text=="Patch zurücknehmen" || t.text=="Spielpatches prüfen"),"Updates menu contains no separate patch section or rollback button");Capture(camera,"Updates-ohne-Patchbereich");
            var normal=new UpdateRelease{tag_name="v0.9.0",body="Neue Landschaften und bessere Steuerung.",assets=Array.Empty<UpdateAsset>()};var type=typeof(GameUpdateClient);var releaseField=type.GetField("availableRelease",BindingFlags.Instance|BindingFlags.NonPublic);releaseField.SetValue(updates,normal);type.GetMethod("Show",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(updates,new object[]{normal});yield return null;
            updates.PresentPatch(Package(2).manifest,"Patch lädt automatisch",.5f,true);yield return null;
            Check(PopupTexts().Any(t=>t.text=="NEUES UPDATE VERFÜGBAR · v0.9.0") && !PopupTexts().Any(t=>t.text.Contains("PATCH") || t.text.Contains("gepatcht")),"Normal APK update contains no patch label, notes or section");
            Check(Overlay.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="Manuell installieren"),"Normal APK update retains manual Android installation");
            Check(FindObjectsByType<Canvas>(FindObjectsSortMode.None).Count(c=>c.name=="Spielupdate")==1,"Simultaneous APK/patch notices use one popup with APK priority");Capture(camera,"Normales-Update");Button("Später / schließen").onClick.Invoke();updates.OpenStatus();yield return null;Check(PopupTexts().Any(t=>t.text.StartsWith("NEUES UPDATE")),"Reopening status still prioritizes available normal update");Button("Später / schließen").onClick.Invoke();releaseField.SetValue(updates,null);
            server.Publish(Package(2));server.PayloadDelay=500;server.ChunkDelay=500;patch.CheckNow();LevelRun.StartLevel(0);yield return null;yield return null;deadline=Time.realtimeSinceStartup+12;while(patch.Busy && Time.realtimeSinceStartup<deadline)yield return null;
            Check(PatchRuntime.Revision==1 && patch.LastStatus.Contains("Hauptmenü") && LevelRun.Instance.IsPlaying,"In-flight patch waits for main menu and does not interrupt gameplay");
            LevelRun.Instance.Pause();Check(PatchRuntime.Revision==1,"Pausing a level does not apply a pending patch");LevelRun.Instance.Menu();yield return null;yield return null;deadline=Time.realtimeSinceStartup+5;while(PatchRuntime.Revision!=2 && Time.realtimeSinceStartup<deadline)yield return null;
            Check(PatchRuntime.Revision==2,"Pending patch activates automatically on return to menu");
            updates.OpenStatus();yield return null;Check(PopupTexts().Any(t=>t.text=="PATCH · "+Application.version+" / 2"),"Common window switches back to current patch with correct number");Button("Schließen").onClick.Invoke();
            server.Publish(Package(3),Encoding.UTF8.GetBytes("invalid"));patch.CheckNow();deadline=Time.realtimeSinceStartup+8;while(patch.Busy && Time.realtimeSinceStartup<deadline)yield return null;Check(PatchRuntime.Revision==2 && patch.LastStatus.Contains("abgelehnt"),"Unverified payload never replaces applied patch");
            server.Dispose();patch.CheckNow();deadline=Time.realtimeSinceStartup+8;while(patch.Busy && Time.realtimeSinceStartup<deadline)yield return null;Check(PatchRuntime.Revision==2 && patch.LastStatus.Contains("offline"),"Offline preserves applied patch and keeps game playable");
        }
        void Capture(Camera camera,string name)
        {
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(2340,1080,24);var target=camera.targetTexture;camera.targetTexture=rt;
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;var scaler=c.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(2340,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2340,1080),0,0);image.Apply();File.WriteAllBytes("Logs/AutomaticPatchFlow/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=target;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
