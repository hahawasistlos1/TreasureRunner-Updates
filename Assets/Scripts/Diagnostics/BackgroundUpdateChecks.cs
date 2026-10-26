#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class BackgroundUpdateChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool condition,string label){if(!condition)throw new Exception(label);report.Add("PASS: "+label);}
        void Logged(string message,string trace,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+message+"\n"+trace);}}
        string Hash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file));}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;
            string save=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");
            string apk="Builds/Publish-0.8.0/TreasureRunner.apk",before=Hash(apk);
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;
            bool failed=false;var scenarios=Scenarios();
            while(true){bool more;object item=null;try{more=scenarios.MoveNext();if(more)item=scenarios.Current;}catch(Exception exception){report.Add("FAIL: "+exception);failed=true;break;}if(!more)break;yield return item;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Logged;
            try {
                Check(PlayerPrefs.GetString(ProgressStore.SaveKey,"")==save && PlayerPrefs.GetString(ProgressStore.BackupKey,"")==backup,"Real progress and backup unchanged");
                Check(Hash(apk)==before,"Published APK unchanged");
                Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"No app or Android version change");
            } catch(Exception exception){failed=true;report.Add("FAIL: "+exception);}
            failed|=errors;report.Add(failed ? "BACKGROUND UPDATE CHECKS FAILED" : "ALL BACKGROUND UPDATE CHECKS PASSED");
            File.WriteAllLines("Logs/BackgroundUpdates/UnityChecks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            Check(BackgroundUpdateNotifications.IsUpdateLink("treasurerunner://updates"),"Notification target accepted");
            Check(BackgroundUpdateNotifications.IsUpdateLink("TREASURERUNNER://UPDATES"),"URI scheme and host case accepted");
            foreach(string url in new[]{null,"","treasurerunner://invite","https://example.com/updates","treasurerunner://updates?url=https://evil.test","treasurerunner://updates#payload","treasurerunner://updates/","treasurerunner://user@updates"})
                Check(!BackgroundUpdateNotifications.IsUpdateLink(url),"Unrelated or modified URL does not open updater: "+url);
            Check(!BackgroundUpdateNotifications.Supported && !BackgroundUpdateNotifications.Enabled && !BackgroundUpdateNotifications.Permitted,"Editor does not call native Android notification APIs");
            BackgroundUpdateNotifications.Enable();BackgroundUpdateNotifications.Disable();BackgroundUpdateNotifications.EnsureScheduled();BackgroundUpdateNotifications.Acknowledge("v0.9.0");
            Check(!BackgroundUpdateNotifications.Enabled,"Non-Android calls do not enable background jobs or alter save state");
            var xml=XDocument.Load("Assets/Plugins/Android/TreasureUpdates.androidlib/AndroidManifest.xml");XNamespace android="http://schemas.android.com/apk/res/android";
            var permissions=xml.Root.Elements("uses-permission").Select(p=>(string)p.Attribute(android+"name")).ToArray();
            foreach(string required in new[]{"INTERNET","ACCESS_NETWORK_STATE","POST_NOTIFICATIONS","RECEIVE_BOOT_COMPLETED"})Check(permissions.Contains("android.permission."+required),"Required background permission declared: "+required);
            Check(!permissions.Any(p=>p.Contains("EXACT_ALARM") || p.Contains("FOREGROUND_SERVICE")),"Background checks require no exact alarm or persistent foreground service");
            var service=xml.Descendants("service").Single(p=>(string)p.Attribute(android+"name")=="com.treasurerunner.updates.UpdateNotificationJob");
            Check((string)service.Attribute(android+"permission")=="android.permission.BIND_JOB_SERVICE","Only Android scheduler may bind job service");
            Check(xml.Descendants("provider").Any(p=>(string)p.Attribute(android+"authorities")=="${applicationId}.updates" && (string)p.Attribute(android+"exported")=="false"),"Existing protected APK provider retained");
            var camera=new GameObject("Menu Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.cullingMask &= ~(1<<30);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;
            var home=new GameObject("Home").AddComponent<HomeScreen>();home.Build();yield return null;home.OpenPlayerMenu();yield return null;yield return new WaitForSecondsRealtime(.4f);
            var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Updates");yield return null;Canvas.ForceUpdateCanvases();
            var panel=FindFirstObjectByType<BackgroundUpdatePanel>();Check(panel!=null,"Updates page contains background notification controls");
            Check(panel.GetComponentsInChildren<Button>().Length==3,"Enable, disable and Android-settings actions available");
            Check(panel.GetComponentsInChildren<Button>().All(button=>!button.interactable),"Windows/editor does not advertise functional Android permissions");
            Check(panel.GetComponentsInChildren<Text>().Any(text=>text.text.Contains("geschlossenem Spiel") && text.text.Contains("Android")),"Platform limitation visible in editor");
            var scroll=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First();scroll.verticalNormalizedPosition=.49f;yield return null;Capture(camera,"Updates");
            menu.Show("Einstellungen");yield return null;Check(FindFirstObjectByType<BackgroundUpdatePanel>()!=null,"Settings page contains the same notification choice");
            menu.Show("Start");yield return null;Check(FindFirstObjectByType<BackgroundUpdatePanel>()==null,"Leaving page cleans up panel");
            var client=GameUpdateClient.Instance;Check(client!=null,"Existing updater remains attached");
            var link=typeof(GameUpdateClient).GetMethod("OnUpdateLink",BindingFlags.NonPublic|BindingFlags.Instance);
            var pending=typeof(GameUpdateClient).GetField("openRequested",BindingFlags.NonPublic|BindingFlags.Instance);
            pending.SetValue(client,false);link.Invoke(client,new object[]{"treasurerunner://invite"});Check(!(bool)pending.GetValue(client),"Social link does not request update popup");
            link.Invoke(client,new object[]{BackgroundUpdateNotifications.OpenUrl});Check((bool)pending.GetValue(client),"Warm notification tap requests fresh release check");
            pending.SetValue(client,false);
            Check(GameUpdateClient.IsNewer("v0.9.0","0.8.0") && !GameUpdateClient.IsNewer("v0.8.0","0.8.0"),"Foreground version policy retained");
        }
        void Capture(Camera camera,string name)
        {
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();
            var rt=new RenderTexture(2340,1080,24);var previous=camera.targetTexture;camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}
            Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(2340,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2340,1080),0,0);image.Apply();
            File.WriteAllBytes("Logs/BackgroundUpdates/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=oldCameras[i];}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
