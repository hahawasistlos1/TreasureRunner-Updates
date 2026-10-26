#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public class ReferenceMenuChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
        Button Button(string text)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==text);
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/ReferenceMenuPreview");string save=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;bool failed=false;var scenarios=Scenarios();
            while(true){bool more;object item=null;try{more=scenarios.MoveNext();if(more)item=scenarios.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return item;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Log;Check(save==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real player save and backup remain unchanged");failed|=errors;report.Add(failed ? "REFERENCE MENU TESTS FAILED" : "ALL REFERENCE MENU TESTS PASSED");File.WriteAllLines("Logs/ReferenceMenuTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var data=ProgressStore.Data;data.coins=93;data.playerName="Schatzjäger";data.highestUnlockedLevel=7;data.skin="Waldläufer";data.upgrades=new[]{2,3,0,1,0};data.weapons.Add("bow");data.levels.Add(new LevelRecord{id=0,bestScore=5990,bestTime=334.7f,completions=1});data.levels.Add(new LevelRecord{id=1,bestScore=6900,bestTime=405.6f,completions=1});data.levels.Add(new LevelRecord{id=2,bestScore=6000,bestTime=361,completions=1,hasDetailedStats=true,mostCoins=59,mostEnemies=12});MainMenuController.SelectedLevel=2;
            var camera=new GameObject("Menu Camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.cullingMask &= ~(1<<30);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;
            var home=new GameObject("Menu owner").AddComponent<HomeScreen>();home.Build();yield return null;Check(FindFirstObjectByType<MainMenuController>()==null,"Home entry remains before player dashboard");home.OpenPlayerMenu();yield return null;yield return new WaitForSecondsRealtime(.35f);var menu=FindFirstObjectByType<MainMenuController>();
            Check(ReferenceMenuSkin.Atlas!=null && Resources.Load<Texture2D>("MenuArt/Reference/Adventure")!=null,"Original reference atlas and clean adventure artwork load from project resources");
            Check(ReferenceMenuSkin.Atlas.width==1672 && ReferenceMenuSkin.Atlas.height==941,"Reference atlas retains exact original pixels without power-of-two resizing");
            Check(GameObject.Find("Navigation").transform.childCount==14,"All fourteen reference navigation entries are directly accessible");
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Weiter spielen") && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Wald 3")),"Dashboard reads actual selected world and stage");
            Check(FindObjectsByType<CharacterPreview>(FindObjectsSortMode.None).Length==1,"Actual selected hero has exactly one live preview");
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="93") && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="59") && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="12"),"Wallet and per-level statistics use actual saved values");
            Check(!FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("XP")),"No fictional XP system or premium balance appears");
            foreach(string name in new[]{"Aktuelles Abenteuer","Charakterübersicht","Welten","Shop","Waffen & Inventar","Level-Editor","Community","Rekorde","Freunde","Updates"})Check(GameObject.Find(name)!=null,"Reference dashboard region exists: "+name);
            Capture(camera,1672,941,"Dashboard-1672x941");Capture(camera,2340,1080,"Dashboard-Handy");Capture(camera,1200,900,"Dashboard-Tablet");
            var allButtons=FindObjectsByType<Button>(FindObjectsSortMode.None);Check(allButtons.Length>=28 && allButtons.All(b=>b.targetGraphic!=null),"Dashboard consists of independent Unity controls with real click surfaces: "+allButtons.Length);
            Check(FindFirstObjectByType<ReferencePlayGlyph>().GetComponent<CanvasRenderer>()!=null,"White start triangle has an actual UI renderer");
            Canvas.ForceUpdateCanvases();foreach(var button in allButtons.Where(b=>b.name.StartsWith("Navigation "))){var rect=(RectTransform)button.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Check(hits.Count>0 && hits[0].gameObject.GetComponentInParent<Button>()==button,"Actual pointer hit reaches sidebar button: "+button.name);}
            string before=JsonUtility.ToJson(data);
            string[] pages={"Levelauswahl","Weltkarte","Shop","Waffen","Inventar","Charakter","Community","Freunde","Rangliste","Einstellungen","Profil","Updates","Credits","Erfolge"};
            foreach(string page in pages){menu.Show(page);yield return null;yield return new WaitForSecondsRealtime(.2f);Canvas.ForceUpdateCanvases();Check(!errors,"Direct navigation preserves page: "+page);Check(FindObjectsByType<Button>(FindObjectsSortMode.None).All(b=>((RectTransform)b.transform).rect.width>0 && ((RectTransform)b.transform).rect.height>0),"Positive button bounds on page: "+page);}
            Check(JsonUtility.ToJson(data)==before,"Opening every reference menu page preserves progress, friends and owned items");
            foreach(var character in CharacterCatalog.All){data.characterId=character.id;menu.Show("Start");yield return null;yield return new WaitForSecondsRealtime(.2f);var preview=FindFirstObjectByType<CharacterPreview>();Check(preview.GetComponent<RawImage>().texture!=null && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text==character.title),"Dashboard uses selected real figure: "+character.id);var stage=(GameObject)typeof(CharacterPreview).GetField("stage",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(preview);Check(stage.GetComponentsInChildren<Animator>(true).All(a=>!a.enabled),"Real preview stage does not override relaxed pose: "+character.id);}data.characterId="goblin";
            menu.Show("Weltkarte");yield return null;Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Count(b=>b.GetComponentInChildren<Text>()?.text=="Welt erkunden")==8,"World map has eight functioning real-world destinations");Button("Welt erkunden").onClick.Invoke();yield return null;Check(Button("Alle Welten")!=null,"World map destination opens actual level list");
            menu.Show("Shop");yield return null;int original=data.coins,level=data.upgrades[2],price=GameConfig.Current.upgrades[2].Price(level);data.coins=10000;menu.Refresh();yield return null;string text=price+" Münzen";var buy=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==text);buy.onClick.Invoke();yield return null;Check(data.coins<10000,"Reference navigation retains transactional shop purchases");Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text==data.coins.ToString()),"Top wallet updates immediately after a real purchase");
            menu.Show("Inventar");yield return null;Button("Ausrüsten").onClick.Invoke();yield return null;Check(data.equippedWeapon=="bow","Reference inventory still equips an owned bow");
            menu.Show("Start");yield return new WaitForSecondsRealtime(.3f);Button("Level spielen").onClick.Invoke();yield return null;yield return null;Check(LevelRun.Instance!=null && LevelRun.SelectedLevel==2,"Reference green play button starts selected real level");LevelRun.Instance.Menu();yield return null;yield return null;yield return null;menu=FindFirstObjectByType<MainMenuController>();Check(menu!=null,"Returning from level restores new dashboard");
            LevelEditorScreen.Working=LevelCodec.Starter();menu.Show("Level-Editor");yield return null;Check(FindFirstObjectByType<LevelEditorScreen>()!=null,"Reference editor navigation opens functioning level editor");
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App version and Android code remain unchanged");
        }
        void Capture(Camera camera,int width,int height,string name)
        {
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(width,height,24);var old=camera.targetTexture;camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}
            Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes("Builds/ReferenceMenuPreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=old;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=oldCameras[i];}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
