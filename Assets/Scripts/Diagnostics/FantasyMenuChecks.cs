#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class FantasyMenuChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string description){if(!ok)throw new Exception(description);report.Add("PASS: "+description);}
        void Logged(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
        Button Button(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==label);
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/FantasyMenuPreview");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;
            var sequence=Scenarios();bool failed=false;
            while(true){object value=null;bool more;try{more=sequence.MoveNext();if(more)value=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return value;}
            ProgressStore.EndMemoryTest();if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real save or backup changed");}else report.Add("PASS: Real player save and backup unchanged by tests");Time.timeScale=1;Application.logMessageReceived-=Logged;failed|=errors;report.Add(failed ? "FANTASY MENU TESTS FAILED" : "ALL FANTASY MENU TESTS PASSED");File.WriteAllLines("Logs/FantasyMenuTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var data=ProgressStore.Data;data.coins=1573;data.highestUnlockedLevel=7;data.playerName="Schatzjäger";data.skin="Waldläufer";data.totalEnemies=37;data.lifetimeCoins=2480;data.levels.Add(new LevelRecord{id=0,bestScore=5990,bestTime=334.7f,completions=1});data.levels.Add(new LevelRecord{id=1,bestScore=6900,bestTime=405.6f,completions=2});data.upgrades[0]=2;data.upgrades[1]=3;data.weapons.Add("bow");MainMenuController.SelectedLevel=2;
            Check(GameUI.Font.name.Contains("Lato"),"Lato readable body font imports");Check(FantasyMenu.TitleFont.name.Contains("Cinzel"),"Cinzel fantasy title font imports");
            foreach(string license in new[]{"Fonts/Cinzel-License","Fonts/Lato-License","OpenArt/Dungeon/LICENSE","OpenArt/Skeletons/LICENSE","OpenArt/Platformer/License","OpenArt/Particles/License","OpenArt/Nature/License"})Check(Resources.Load<TextAsset>(license)!=null,"Complete bundled license: "+license);
            // Capture small, genuine in-game world images. HUD is excluded; no imagined progress.
            Directory.CreateDirectory("Assets/Resources/MenuArt");
            for(int w=0;w<8;w++)
            {LevelRun.StartLevel(CampaignCatalog.Id(w,0));yield return null;yield return null;yield return null;var run=LevelRun.Instance;run.Player.GrantInvulnerability(100);run.Player.GetComponent<PlayerMovement>().ControlLocked=true;var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(9,3.6f,-12);camera.transform.rotation=Quaternion.Euler(8,0,0);
                foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;yield return null;Capture(camera,512,288,"Assets/Resources/MenuArt/World-"+w+".png",false);AssetDatabase.ImportAsset("Assets/Resources/MenuArt/World-"+w+".png",ImportAssetOptions.ForceSynchronousImport);Check(File.Exists("Assets/Resources/MenuArt/World-"+w+".png"),"Genuine campaign preview for world "+w);}
            data.coins=1573;LevelRun.CustomLevel=null;HomeScreen.ReturnToPlayerMenu=false;EditorTestSession.Clear();SceneManager.LoadScene("MainMenu");yield return null;yield return null;yield return new WaitForSeconds(.4f);
            var home=FindFirstObjectByType<HomeScreen>();Check(home!=null && FindFirstObjectByType<MainMenuController>()==null,"Home still precedes dashboard");UIShot("01-Home",1600,900);home.OpenPlayerMenu();yield return null;yield return new WaitForSeconds(.4f);
            var menu=FindFirstObjectByType<MainMenuController>();string before=JsonUtility.ToJson(data);
            Check(menu!=null && FindObjectsByType<CharacterPreview>(FindObjectsSortMode.None).Length==1,"Dashboard displays actual character preview");
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("1573")),"Dashboard reads actual wallet");Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Wurzelfestung")),"Dashboard reads selected real campaign stage");
            UIShot("02-Dashboard",1600,900);UIShot("02-Dashboard-Phone-Wide",2340,1080);UIShot("02-Dashboard-Tablet",1200,900);
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.text=="Weiter spielen").cachedTextGenerator.characterCountVisible>5,"Dashboard heading remains visibly rendered");
            foreach(var image in FindObjectsByType<RawImage>(FindObjectsSortMode.None).Where(i=>i.name=="Logo-Grafik" || i.name=="Objekt-Grafik")){var r=(RectTransform)image.transform;var parent=(RectTransform)r.parent;var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(parent,r);Check(bounds.size.x<=parent.rect.width+.1f && bounds.size.y<=parent.rect.height+.1f,"Fitted artwork stays inside its own reserved frame: "+image.name);}
            foreach(string page in new[]{"Levelauswahl","Shop","Inventar","Charakter","Freunde","Rangliste","Einstellungen","Profil","Credits","Updates","Community","Erfolge"})
            {menu.Show(page);yield return null;yield return new WaitForSeconds(.2f);Check(!errors,"Menu page works: "+page);UIShot("Page-"+page,1600,900);Check(FindObjectsByType<Button>(FindObjectsSortMode.None).All(b=>((RectTransform)b.transform).rect.width>0 && ((RectTransform)b.transform).rect.height>0),"Positive button sizes: "+page);}
            Check(JsonUtility.ToJson(data)==before,"Navigating all menu pages preserves the entire progress data");
            menu.Show("Community");yield return null;var community=FindFirstObjectByType<CommunityLevelsPage>();community.StopAllCoroutines();var sample=LevelCodec.Starter();sample.title="Suchtest";sample.author="Bauprüfer";string catalog="{\"version\":1,\"levels\":[null,"+JsonUtility.ToJson(sample)+"]}";
            typeof(CommunityLevelsPage).GetMethod("ShowCatalog",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(community,new object[]{catalog});var searchBox=FindFirstObjectByType<InputField>();searchBox.text="Bauprüfer";yield return null;
            var communityStatus=(Text)typeof(CommunityLevelsPage).GetField("status",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(community);Check(communityStatus.text.StartsWith("1 "),"Community search matches real author data and ignores malformed entries");searchBox.text="Keine Übereinstimmung";yield return null;Check(communityStatus.text.StartsWith("Keine passenden"),"Community search reports empty results without invented levels");
            Check(!data.FindLevel(0).hasDetailedStats && data.FindLevel(0).bestScore==5990,"Legacy records keep real scores and do not invent per-level coin or enemy counts");
            ProgressStore.CompleteLevel(0,10,500,80,34,7);ProgressStore.CompleteLevel(0,10,500,80,10,3);var detailed=data.FindLevel(0);Check(detailed.hasDetailedStats && detailed.mostCoins==34 && detailed.mostEnemies==7 && detailed.bestScore==5990,"Detailed completion stats capture actual maxima without losing old best scores");
            string serialized=JsonUtility.ToJson(data);var restored=JsonUtility.FromJson<ProgressData>(serialized);Check(JsonUtility.ToJson(restored)==serialized,"Complete existing progress and optional new fields round-trip through save JSON");
            menu.ShowWorld(0);yield return null;Canvas.ForceUpdateCanvases();Check(Button("Gesperrt").interactable==false,"Locked stages have disabled controls");UIShot("03-Levelauswahl",1600,900);
            menu.SelectLevel(1);yield return null;Check(MainMenuController.ContinueLevel==1,"Selecting unlocked stage changes dashboard continuation");menu.SelectLevel(23);Check(MainMenuController.ContinueLevel==1,"Cannot select locked stage");
            menu.Show("Shop");yield return null;int coins=data.coins;int level=data.upgrades[0];string price=GameConfig.Current.upgrades[0].Price(level)+" Münzen";Button(price).onClick.Invoke();yield return null;Check(data.upgrades[0]==level+1 && data.coins==coins-GameConfig.Current.upgrades[0].Price(level),"Actual shop button buys one upgrade and charges correct existing price");Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains(data.coins.ToString())),"Wallet updates immediately after purchase");
            data.coins=0;menu.Refresh();yield return null;Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Where(b=>b.GetComponentInChildren<Text>().text.Contains("Münzen")).All(b=>!b.interactable),"Insufficient wallet disables all paid purchase controls");data.coins=1573;
            menu.Show("Inventar");yield return null;Button("Ausrüsten").onClick.Invoke();yield return null;Check(data.equippedWeapon=="bow" && data.weapons.Contains("sword"),"Real inventory button equips owned bow without losing sword");
            menu.Show("Einstellungen");yield return null;Button("Stummschalten").onClick.Invoke();yield return null;Check(data.muted,"Mute toggle persists in progress data");Button("Ton einschalten").onClick.Invoke();yield return null;Check(!data.muted,"Unmute retains volume settings");
            foreach(var slider in FindObjectsByType<Slider>(FindObjectsSortMode.None))slider.value=.42f;Check(Mathf.Abs(data.musicVolume-.42f)<.001f && Mathf.Abs(data.effectsVolume-.42f)<.001f,"Both audio sliders update stored settings");
            Button("Hoch").onClick.Invoke();yield return null;Check(data.graphicsQuality==2,"Graphics quality control stores selection");
            menu.Show("Start");yield return null;Button("Level spielen").onClick.Invoke();yield return null;yield return null;yield return null;Check(LevelRun.SelectedLevel==1 && LevelRun.Instance!=null,"Dashboard green button starts chosen level");LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
            menu=FindFirstObjectByType<MainMenuController>();Check(menu!=null,"Returning from level restores dashboard");
            LevelEditorScreen.Working=LevelCodec.Starter();menu.Show("Level-Editor");yield return null;yield return null;Check(FindFirstObjectByType<LevelEditorScreen>()!=null,"Editor remains reachable from grouped navigation");
            foreach(string label in new[]{"Speichern","Testspielen","Start setzen","Radierer","Rückgängig","Wiederholen","Bauplatz","Teilen","Importieren","Einreichen","Level einrichten","Simulation: an"})Check(Button(label)!=null,"Editor operation preserved: "+label);
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="BAUTEIL"),"Editor has separate brush properties panel");UIShot("04-Editor",1600,900);UIShot("04-Editor-Phone",2340,1080);
            Button("Gegner").onClick.Invoke();yield return null;Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("alle Welten")),"Editor category opens unified palette");UIShot("05-Palette",1600,900);
            foreach(var id in new[]{"sword","axe","bow","staff"})Check(MenuWeaponPreview.Get(id).GetPixels32().Count(p=>p.a>10)>30,"Visible genuine weapon thumbnail: "+id);
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Menu work never increments app or Android version");
        }
        void UIShot(string name,int width,int height){var camera=FindObjectsByType<Camera>(FindObjectsSortMode.None).FirstOrDefault(c=>c.name=="Menu Camera")??Camera.main;Capture(camera,width,height,"Builds/FantasyMenuPreview/"+name+".png",true);}
        void Capture(Camera camera,int width,int height,string path,bool ui)
        {
            var canvases=ui ? FindObjectsByType<Canvas>(FindObjectsSortMode.None) : new Canvas[0];var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var target=camera.targetTexture;var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=target;rt.Release();Destroy(rt);Destroy(image);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
