using System;
using System.IO;
using System.Collections;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace TreasureRunner
{
    [Serializable] public class UpdateChannel { public string owner;public string repository;public bool enabled; }
    [Serializable] public class UpdateAsset { public string name;public string browser_download_url;public string digest;public long size; }
    [Serializable] public class UpdateRelease { public string tag_name;public string body;public bool draft;public bool prerelease;public UpdateAsset[] assets; }
    [Serializable] public class PendingUpdate { public string version; }
    public class GameUpdateClient:MonoBehaviour
    {
        private static GameUpdateClient instance;private UpdateChannel channel;private RectTransform overlay;private Text status;private Image fill;private Button install;
        private UnityWebRequest downloading;private string directory,apk,pending;private bool busy,ready;private float nextCheck;private LevelRun pausedRun;
        public string LastStatus { get; private set; }="Noch nicht geprüft";
        public static GameUpdateClient Instance=>instance;
        public string AvailableVersion { get; private set; }
        private string notifiedVersion;
        private UpdateRelease availableRelease;private bool openRequested;
        private bool patchMode,patchNoticeRequested;private int displayedPatchRevision,announcedPatchRevision;
        private PatchManifest patchAvailable;private string patchStatus;private float patchProgress;
        private void Awake()
        {
            if(instance!=null){Destroy(this);return;}instance=this;DontDestroyOnLoad(gameObject);
            Application.deepLinkActivated+=OnUpdateLink;
            openRequested=BackgroundUpdateNotifications.IsUpdateLink(Application.absoluteURL);
            directory=Path.Combine(Application.temporaryCachePath,"treasurerunner-updates");
#if UNITY_EDITOR
            if(ProgressStore.IsMemoryTest)directory=Path.Combine(Environment.CurrentDirectory,"Logs/BackgroundUpdates/TempData");
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var bridge=new AndroidJavaClass("com.treasurerunner.updates.UpdateBridge")) directory=bridge.CallStatic<string>("directory",activity);
#endif
            Directory.CreateDirectory(directory);apk=Path.Combine(directory,"treasurerunner-update.apk");pending=Path.Combine(directory,"pending.json");
            Debug.Log("UPDATE_CACHE_EMPTY="+(!File.Exists(apk))+" VERSION="+Application.version);
            try
            {
                if(File.Exists(pending) && !IsNewer(JsonUtility.FromJson<PendingUpdate>(File.ReadAllText(pending)).version,Application.version)) Cleanup();
                if(File.Exists(apk+".partial"))File.Delete(apk+".partial");
            }catch(Exception e){Debug.LogWarning("Update cache: "+e.GetType().Name);}
        }
        private IEnumerator Start()
        {
            if(instance!=this) yield break;
            var config=Resources.Load<TextAsset>("UpdateChannel");channel=config!=null ? JsonUtility.FromJson<UpdateChannel>(config.text) : null;
            BackgroundUpdateNotifications.Initialize(channel);
#if UNITY_EDITOR
            if(ProgressStore.IsMemoryTest){nextCheck=float.PositiveInfinity;yield break;}
#endif
            if(channel==null || !channel.enabled){LastStatus="Update-Kanal wird eingerichtet";yield break;}
            yield return null;yield return Check();
        }
        public static bool IsNewer(string candidate,string current)
        {
            if(string.IsNullOrEmpty(candidate)) return false;
            candidate=candidate.TrimStart('v','V');
            return Version.TryParse(candidate,out var a) && Version.TryParse(current,out var b) && a>b;
        }
        public static bool AllowedDownload(string url,string owner,string repository)
        {
            return Uri.TryCreate(url,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.Host=="github.com" && uri.AbsolutePath.StartsWith("/"+owner+"/"+repository+"/releases/download/",StringComparison.Ordinal) && uri.AbsolutePath.EndsWith(".apk",StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(uri.UserInfo);
        }
        private void OnApplicationPause(bool paused){BackgroundUpdateNotifications.SetVisible(!paused);if(!paused)nextCheck=Mathf.Min(nextCheck,Time.unscaledTime+2);}
        private void OnApplicationFocus(bool focused){BackgroundUpdateNotifications.SetVisible(focused);}
        private void OnUpdateLink(string url){if(BackgroundUpdateNotifications.IsUpdateLink(url)){openRequested=true;nextCheck=0;}}
        private void Update()
        {
            if(patchNoticeRequested && CanShowPatch)ShowPatch();
            if(channel==null || !channel.enabled || busy || (LevelRun.Instance!=null && !openRequested))return;
            if(openRequested || Time.unscaledTime>=nextCheck)CheckNow();
        }
        public void CheckNow()
        {
            PatchClient.Instance?.CheckNow();
#if UNITY_EDITOR
            if(ProgressStore.IsMemoryTest)return;
#endif
            if(!busy && channel!=null && channel.enabled)StartCoroutine(Check());
        }
        private bool CanShowPatch=>!busy && availableRelease==null && LevelRun.Instance==null && patchAvailable!=null;
        public void PresentPatch(PatchManifest manifest,string state,float progress,bool announce=false)
        {
            if(manifest==null || manifest.appVersion!=Application.version)return;
            patchAvailable=manifest;patchStatus=state;patchProgress=Mathf.Clamp01(progress);
            if(announce && announcedPatchRevision!=manifest.revision){announcedPatchRevision=manifest.revision;patchNoticeRequested=true;}
            if(CanShowPatch && (patchNoticeRequested || (patchMode && overlay!=null && overlay.parent.gameObject.activeSelf && displayedPatchRevision!=manifest.revision)))ShowPatch();
            if(patchMode && overlay!=null && displayedPatchRevision==manifest.revision){SetProgress(patchProgress);if(status!=null)status.text=patchStatus;}
        }
        private void ShowPatch()
        {
            if(!CanShowPatch)return;
            patchNoticeRequested=false;
            if(overlay==null || !patchMode || displayedPatchRevision!=patchAvailable.revision)
            {patchMode=true;displayedPatchRevision=patchAvailable.revision;BuildPopup("PATCH · "+Application.version+" / "+patchAvailable.revision,"Automatischer Download und Anwendung · keine Installation nötig",patchAvailable.title+"\n\nWas wurde gepatcht?\n"+patchAvailable.changes,true);}
            overlay.parent.gameObject.SetActive(true);SetProgress(patchProgress);if(status!=null)status.text=patchStatus;
        }
        private IEnumerator Check()
        {
            bool showRequested=openRequested;openRequested=false;
            busy=true;nextCheck=Time.unscaledTime+300;LastStatus="Prüfe Updates …";
            using(var request=UnityWebRequest.Get("https://api.github.com/repos/"+channel.owner+"/"+channel.repository+"/releases/latest"))
            {
                request.SetRequestHeader("User-Agent","TreasureRunner-Android");request.SetRequestHeader("Accept","application/vnd.github+json");request.timeout=20;
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){LastStatus=request.responseCode==404 ? "Noch kein Update veröffentlicht" : "Offline – Updates später prüfen";busy=false;yield break;}
                UpdateRelease release=null;try{release=JsonUtility.FromJson<UpdateRelease>(request.downloadHandler.text);}catch(ArgumentException){}
                if(release==null || release.draft || release.prerelease || !IsNewer(release.tag_name,Application.version)){LastStatus="Version "+Application.version+" ist aktuell";busy=false;yield break;}
                UpdateAsset asset=release.assets==null ? null : Array.Find(release.assets,a=>a.name=="TreasureRunner.apk");
                if(asset==null || asset.size<=0 || asset.size>268435456 || !AllowedDownload(asset.browser_download_url,channel.owner,channel.repository) || string.IsNullOrEmpty(asset.digest) || !asset.digest.StartsWith("sha256:") || asset.digest.Length!=71)
                {LastStatus="Update unvollständig – kein geprüfter APK-Download";busy=false;yield break;}
                AvailableVersion=release.tag_name;availableRelease=release;
                if(notifiedVersion!=release.tag_name || showRequested){Show(release);notifiedVersion=release.tag_name;BackgroundUpdateNotifications.Acknowledge(release.tag_name);}
                bool cached=false;
                try
                {
                    if(File.Exists(apk) && File.Exists(pending) && JsonUtility.FromJson<PendingUpdate>(File.ReadAllText(pending)).version==release.tag_name.TrimStart('v','V'))
                        using(var sha=SHA256.Create())using(var file=File.OpenRead(apk))cached=file.Length==asset.size && BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").Equals(asset.digest.Substring(7),StringComparison.OrdinalIgnoreCase);
                }catch(Exception){cached=false;}
                if(cached){ready=true;SetProgress(1);LastStatus="Update "+release.tag_name+" bereit – Installation bestätigen";if(status!=null)status.text=LastStatus;if(install!=null)install.interactable=true;}
                else yield return Download(asset,release.tag_name.TrimStart('v','V'));
            }
            busy=false;
        }
        private void Show(UpdateRelease release)
        {
            if(LevelRun.Instance!=null && LevelRun.Instance.IsPlaying){pausedRun=LevelRun.Instance;pausedRun.Pause();}
            patchMode=false;
            BuildPopup("NEUES UPDATE VERFÜGBAR · "+release.tag_name,"Installiert: "+Application.version+" · Download startet automatisch",string.IsNullOrEmpty(release.body) ? "Verbesserungen und Fehlerkorrekturen." : release.body.Substring(0,Mathf.Min(8000,release.body.Length)),false);
        }
        private void BuildPopup(string title,string subtitle,string changes,bool isPatch)
        {
            if(overlay!=null)Destroy(overlay.parent.gameObject);
            overlay=GameUI.Canvas("Spielupdate");overlay.parent.GetComponent<Canvas>().sortingOrder=80;
            DontDestroyOnLoad(overlay.parent.gameObject);
            var shade=GameUI.Panel("Update backdrop",overlay,new Color(.015f,.025f,.05f,.98f));GameUI.Stretch(shade);
            var card=GameUI.Panel("Update card",overlay,GameUI.Card);card.anchorMin=new Vector2(.15f,.1f);card.anchorMax=new Vector2(.85f,.9f);card.offsetMin=card.offsetMax=Vector2.zero;
            var column=GameUI.Column(card,12,24);GameUI.Stretch(column);
            GameUI.Text(title,column,32,GameUI.Gold);
            GameUI.Text(subtitle,column,21,GameUI.Muted);
            var area=GameUI.Rect("Changelog",column);var areaLayout=area.gameObject.AddComponent<LayoutElement>();areaLayout.preferredHeight=180;areaLayout.flexibleHeight=1;
            var contents=GameUI.Scroll(area);contents.GetComponent<VerticalLayoutGroup>().padding.bottom=24;GameUI.WrappedText(changes,contents,22);
            var track=GameUI.Panel("Download progress",column,new Color(.03f,.06f,.1f));track.gameObject.AddComponent<LayoutElement>().preferredHeight=20;
            var bar=GameUI.Panel("Progress fill",track,GameUI.Gold);GameUI.Stretch(bar);fill=bar.GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;SetProgress(0);
            status=GameUI.Text("Download wird vorbereitet …",column,22);
            var row=GameUI.Row(column,64);row.GetComponent<LayoutElement>().flexibleHeight=0;install=null;if(!isPatch)install=GameUI.Button("Manuell installieren",row,Install,false,true);GameUI.Button(isPatch ? "Schließen" : "Später / schließen",row,Close);
        }
        private void SetProgress(float progress)
        {
            if(fill==null)return;fill.fillAmount=1;
            fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=new Vector2(Mathf.Clamp01(progress),1);
            fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        }
        private IEnumerator Download(UpdateAsset asset,string version)
        {
            ready=false;string part=apk+".partial";
            using(var request=new UnityWebRequest(asset.browser_download_url,UnityWebRequest.kHttpVerbGET))
            {
                downloading=request;request.downloadHandler=new DownloadHandlerFile(part){removeFileOnAbort=true};request.timeout=180;
                var op=request.SendWebRequest();
                while(!op.isDone)
                {
                    float progress=Mathf.Clamp01((float)request.downloadedBytes/asset.size);SetProgress(progress);
                    LastStatus="Download "+Mathf.FloorToInt(progress*100)+" %";if(status!=null)status.text=LastStatus+" · "+(request.downloadedBytes/1048576f).ToString("0.0")+" / "+(asset.size/1048576f).ToString("0.0")+" MB";
                    yield return null;
                }
                downloading=null;
                if(request.result!=UnityWebRequest.Result.Success){LastStatus="Download fehlgeschlagen. Erneut unter Einstellungen prüfen.";if(status!=null)status.text=LastStatus;yield break;}
            }
            bool valid=false;try{using(var sha=SHA256.Create())using(var file=File.OpenRead(part))valid=file.Length==asset.size && BitConverter.ToString(sha.ComputeHash(file)).Replace("-","").Equals(asset.digest.Substring(7),StringComparison.OrdinalIgnoreCase);}catch(IOException){}
            if(!valid){if(File.Exists(part))File.Delete(part);LastStatus="Prüfsumme stimmt nicht. Installation gesperrt.";if(status!=null)status.text=LastStatus;yield break;}
            if(File.Exists(apk))File.Delete(apk);File.Move(part,apk);File.WriteAllText(pending,JsonUtility.ToJson(new PendingUpdate{version=version}));ready=true;
            SetProgress(1);LastStatus="Download geprüft. Installation bitte selbst bestätigen.";if(status!=null)status.text=LastStatus;if(install!=null)install.interactable=true;
        }
        public void OpenStatus()
        {
            if(availableRelease!=null){if(overlay==null || patchMode)Show(availableRelease);overlay.parent.gameObject.SetActive(true);if(status!=null)status.text=LastStatus;if(install!=null)install.interactable=ready;return;}
            if(patchAvailable==null && PatchRuntime.Manifest!=null){patchAvailable=PatchRuntime.Manifest;patchStatus="Patch "+PatchRuntime.Revision+" automatisch angewendet.";patchProgress=1;}
            if(patchAvailable!=null){patchNoticeRequested=true;ShowPatch();return;}
            CheckNow();
        }
        private void Close(){if(patchMode)patchNoticeRequested=false;if(overlay!=null)overlay.parent.gameObject.SetActive(false);if(pausedRun!=null && pausedRun.State==RunState.Paused)pausedRun.Resume();pausedRun=null;}
        private void Install()
        {
            if(patchMode || !ready || !File.Exists(apk))return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var bridge=new AndroidJavaClass("com.treasurerunner.updates.UpdateBridge"))
                {
                    bool opened=bridge.CallStatic<bool>("install",activity);
                    if(!opened && status!=null)status.text="Android: Installation aus dieser Quelle erlauben, zurückkehren und erneut installieren.";
                }
            }catch(AndroidJavaException){if(status!=null)status.text="Android-Installer konnte nicht geöffnet werden.";}
#else
            if(status!=null)status.text="Die Installation steht auf Android zur Verfügung.";
#endif
        }
        private void Cleanup(){if(File.Exists(apk))File.Delete(apk);if(File.Exists(pending))File.Delete(pending);}
        private void OnDestroy(){if(instance==this){Application.deepLinkActivated-=OnUpdateLink;downloading?.Abort();if(overlay!=null)Destroy(overlay.parent.gameObject);instance=null;}}
    }
}
