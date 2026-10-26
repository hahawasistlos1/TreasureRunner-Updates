#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace TreasureRunner
{
    public class NextDevChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool error;
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS: "+message);}
        static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
        Button Button(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==label);
        void Logged(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception){error=true;report.Add("ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;Directory.CreateDirectory("Builds/NextDevPreview");
            var tests=Scenarios();bool failed=false;
            while(true){bool more;object value=null;try{more=tests.MoveNext();if(more)value=tests.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return value;}
            Application.logMessageReceived-=Logged;ProgressStore.EndMemoryTest();failed|=error;Time.timeScale=1;report.Add(failed ? "NEXT DEV TESTS FAILED" : "ALL NEXT DEV TESTS PASSED");File.WriteAllLines("Logs/NextDevTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var artifactPaths=new[]{"Builds/TreasureRunner.apk","Builds/TreasureRunner-Windows-Werkzeuge.zip","Builds/TreasureRunner-Source-0.7.0.zip"};
            var hashes=artifactPaths.Select(Hash).ToArray();
            var camera=new GameObject("Menu test camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.backgroundColor=GameUI.Ink;camera.clearFlags=CameraClearFlags.SolidColor;
            yield return null;yield return null;
            var starter=LevelCodec.Starter();starter.title="Original";LevelEditorScreen.Working=starter;LevelEditorScreen.Open();yield return null;
            var editor=FindFirstObjectByType<LevelEditorScreen>();var paint=FindFirstObjectByType<EditorPaintSurface>();
            var states=new List<string>{LevelCodec.Encode(LevelEditorScreen.Working)};
            for(int i=0;i<100;i++){paint.paint(i%16,1+i/16);states.Add(LevelCodec.Encode(LevelEditorScreen.Working));}
            Button("Rückgängig").onClick.Invoke();Check(LevelCodec.Encode(LevelEditorScreen.Working)==states[99],"Undo after more than 60 changes restores the most recent edit");
            for(int i=0;i<59;i++)Button("Rückgängig").onClick.Invoke();
            Check(LevelCodec.Encode(LevelEditorScreen.Working)==states[40] && !Button("Rückgängig").interactable,"Undo retains exactly the latest 60 edits");
            paint.paint(0,1);Check(Button("Wiederholen").interactable,"Painting an unchanged cell does not discard redo history");
            Button("Wiederholen").onClick.Invoke();Check(LevelCodec.Encode(LevelEditorScreen.Working)==states[41],"Redo restores the exact next state");
            paint.paint(14,10);Check(!Button("Wiederholen").interactable,"A real new edit invalidates redo");
            editor.LoadDocument(starter);var title=Field<InputField>(editor,"titleInput");var author=Field<InputField>(editor,"authorInput");
            title.onEndEdit.Invoke("Neuer Titel");Button("Rückgängig").onClick.Invoke();Check(title.text=="Original" && LevelEditorScreen.Working.title=="Original","Undo synchronizes title field and actual level data");
            Button("Wiederholen").onClick.Invoke();Check(title.text=="Neuer Titel","Redo synchronizes title field");
            Button("Breite +16").onClick.Invoke();Button("Rückgängig").onClick.Invoke();Check(LevelEditorScreen.Working.width==64,"Width change can be undone");
            var second=LevelCodec.Starter();second.title="Zweiter Entwurf";second.author="Andere Person";editor.LoadDocument(second);
            Check(!Button("Rückgängig").interactable && !Button("Wiederholen").interactable && title.text==second.title && author.text==second.author,"Loading another draft clears cross-level history and updates both header fields");
            var heart=BuilderPalette.Get("pickup.heart");typeof(LevelEditorScreen).GetField("selectedAsset",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(editor,heart);typeof(LevelEditorScreen).GetField("tool",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(editor,(int)heart.kind);paint.paint(4,4);
            Check(LevelEditorScreen.Working.format==3 && LevelCodec.Validate(LevelEditorScreen.Working,out _),"New pickup paints a valid explicitly versioned level");
            Check(LevelModeration.CanPublish(LevelEditorScreen.Working)==GameUpdateClient.IsNewer(Application.version,"0.7.0") && LevelModeration.PathFor(LevelEditorScreen.Working)==LevelModeration.CurrentPath,"New mechanics use their version-compatible catalog");
            var old=LevelCodec.Decode(LevelCodec.Encode(LevelEditorScreen.Working));old.format=2;Check(!LevelCodec.Validate(old,out _),"New mechanics cannot masquerade as the old format");
            Canvas.ForceUpdateCanvases();Check(Button("Rückgängig").GetComponentInChildren<Text>().preferredWidth<Button("Rückgängig").GetComponentInChildren<Text>().rectTransform.rect.width,"Undo label fits without breaking inside its word");
            var selectedLabel=Field<Text>(editor,"toolText");var pageLabel=Field<Text>(editor,"pageText");
            Check(selectedLabel.rectTransform.rect.width>180 && pageLabel.rectTransform.rect.width>180 && selectedLabel.preferredHeight<=selectedLabel.rectTransform.rect.height+2 && pageLabel.preferredHeight<=pageLabel.rectTransform.rect.height+2,"Selected tool and page labels fit their available widths and heights: "+selectedLabel.rectTransform.rect.size+", "+pageLabel.rectTransform.rect.size);
            Capture("Editor-Verlauf");
            Button("Radierer").onClick.Invoke();paint.paint(4,4);Check(LevelEditorScreen.Working.format<=2 && LevelModeration.CanPublish(LevelEditorScreen.Working),"Removing every development object restores compatibility with existing clients");
            var capped=LevelCodec.Starter();capped.width=96;capped.tiles.Clear();for(int i=0;i<650;i++)capped.tiles.Add(new LevelBlock{x=i%96,y=i/96,kind=i==61 ? LevelTile.Goal : LevelTile.Ground});editor.LoadDocument(capped);
            Button("Start setzen").onClick.Invoke();paint.paint(14,10);Check(LevelEditorScreen.Working.tiles.Count==650 && LevelEditorScreen.Working.spawnX==2 && LevelEditorScreen.Working.spawnY==0,"Start tool cannot bypass the 650-tile limit");Button("Menü").onClick.Invoke();yield return null;

            var mock=new GameObject("Inactive update preview");mock.SetActive(false);var updater=mock.AddComponent<GameUpdateClient>();
            string notes=string.Join("\n",Enumerable.Range(0,75).Select(i=>"Änderung "+i+": Ein ausführlicher Text zu Bauteilen, Welten und Gegnern."))+"\nENDE DER ÄNDERUNGEN";
            typeof(GameUpdateClient).GetMethod("Show",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(updater,new object[]{new UpdateRelease{tag_name="VORSCHAU",body=notes}});yield return null;Canvas.ForceUpdateCanvases();
            var area=GameObject.Find("Changelog");var scroll=area.GetComponentInChildren<ScrollRect>();LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);Canvas.ForceUpdateCanvases();
            Check(scroll.content.rect.height>scroll.viewport.rect.height*4,"Long update notes produce a real scrollable content height");
            scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;var corners=new Vector3[4];var view=new Vector3[4];scroll.content.GetWorldCorners(corners);scroll.viewport.GetWorldCorners(view);
            Check(Mathf.Abs(corners[0].y-view[0].y)<3,"Last update lines can scroll to the bottom of the viewport");
            var noteText=scroll.content.GetComponentInChildren<Text>();var vertices=noteText.cachedTextGenerator.verts;
            float lastBottom=vertices.Skip(Mathf.Max(0,vertices.Count-8)).Take(4).Min(v=>noteText.transform.TransformPoint(v.position/noteText.pixelsPerUnit).y);
            Check(lastBottom>=view[0].y-2,"Last rendered update marker is inside the visible scroll area");Capture("Update-Text-Scrollen");
            Destroy(Field<RectTransform>(updater,"overlay").parent.gameObject);Destroy(mock);yield return null;

            for(int stage=0;stage<10;stage++)
            {
                var signatures=new HashSet<string>();
                for(int world=0;world<8;world++)
                {
                    var route=WorldRoute.Create(world,stage,WorldRoute.SectionCount(world,stage));
                    Check(route.Zip(route.Skip(1),(a,b)=>JumpProfile.CanCross(a.gap,b.height-a.height)).All(ok=>ok),"World "+world+" stage "+stage+" keeps physically reachable main jumps");
                    signatures.Add(string.Join(",",Enumerable.Range(0,9).Select(z=>WorldRoute.RoomFor(world,stage,z))));
                }
                Check(signatures.Count==8,"Stage "+stage+" has eight distinct world encounter sequences");
            }
            Check(Enumerable.Range(0,8).Select(w=>{var r=WorldRoute.Create(w,0,WorldRoute.SectionCount(w,0))[0];return r.room+":"+r.width;}).Distinct().Count()==8,"All eight worlds begin with different usable room shapes instead of an identical opening");
            foreach(var a in BuilderPalette.Entries.Where(a=>a.format==3))
            {
                var image=EditorTilePreview.Get(new LevelBlock{kind=a.kind,asset=a.id},7);Check(image.GetPixels32().Any(p=>p.a>10),"New asset preview is visible: "+a.id);
            }
            var level=LevelCodec.Starter(4);level.format=3;level.tiles.Add(new LevelBlock{x=5,y=2,kind=LevelTile.Coin,asset="pickup.heart"});level.tiles.Add(new LevelBlock{x=10,y=2,kind=LevelTile.Coin,asset="pickup.ward"});
            level.tiles.RemoveAll(t=>t.x==30 && t.y==0);level.tiles.Add(new LevelBlock{x=30,y=0,kind=LevelTile.Shooter,asset="enemy.skirmisher.cave"});
            for(int x=49;x<=52;x++)level.tiles.RemoveAll(t=>t.x==x && t.y==0);level.tiles.Add(new LevelBlock{x=50,y=0,kind=LevelTile.Shooter,asset="enemy.skirmisher.castle"});
            Check(LevelCodec.Validate(level,out _),"Mixed new-mechanics level validates and keeps old data intact");LevelRun.StartCustom(level);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();body.useGravity=false;body.linearVelocity=Vector3.zero;player.GetComponent<PlayerMovement>().ControlLocked=true;
            var heal=FindFirstObjectByType<HealthPickup>();body.position=heal.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.12f);Check(heal!=null,"Full-heart pickup is preserved instead of wasted");
            body.position=new Vector3(2,1.05f,0);Physics.SyncTransforms();yield return new WaitForFixedUpdate();player.TakeDamage(1);body.position=heal.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.12f);Check(heal==null && player.HeartUnits==6 && player.health==3,"Imported healing heart collects through physics and restores one heart");
            player.RestoreFullHealth();var wardPickup=FindFirstObjectByType<WardPickup>();body.position=wardPickup.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.12f);var ward=player.GetComponent<PlayerWard>();Check(ward.Active && wardPickup==null,"Imported shield pickup activates through actual player contact");
            player.TakeDamage(1);Check(!ward.Active && player.HeartUnits==6,"Shield absorbs exactly one accepted attack without heart loss");player.TakeDamage(1);Check(player.HeartUnits==6,"Shield break has brief overlap protection");
            yield return new WaitForSeconds(.6f);player.invulnerabilitySeconds=0;player.TakeDamage(1);Check(player.HeartUnits==5,"Next unprotected attack removes half a heart");Check(ward.Activate(),"Another shield can be collected after the old one breaks");
            body.position=new Vector3(2,1.05f,0);Physics.SyncTransforms();Capture("Schutzschild");player.TakeFallDamage();Check(player.HeartUnits==4 && ward.Active,"Falls still cost half a heart and do not consume attack shield");yield return new WaitForSeconds(.6f);
            run.Pause();float remaining=ward.Remaining;player.TakeDamage(1);yield return new WaitForSecondsRealtime(.12f);Check(Mathf.Abs(remaining-ward.Remaining)<.01f && ward.Active,"Pause neither consumes nor ages the shield");run.Resume();player.GrantInvulnerability(99);
            body.position=new Vector3(28,1.05f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();var scout=FindObjectsByType<EnemyAwareness>(FindObjectsSortMode.None).First(a=>a.keepDistance && a.transform.position.x<40);float start=scout.transform.position.x;float deadline=Time.time+4;
            while(scout.transform.position.x<start+.2f && Time.time<deadline)yield return null;
            Check(scout.HasTarget && scout.transform.position.x>start+.2f,"Skirmisher notices nearby player and physically retreats while facing them");
            var edge=FindObjectsByType<EnemyAwareness>(FindObjectsSortMode.None).First(a=>a.transform.position.x>40);body.position=new Vector3(48,1.05f,0);Physics.SyncTransforms();yield return new WaitForSeconds(2);
            Check(edge.transform.position.x<50.2f && Mathf.Abs(edge.transform.position.y-.6f)<.05f,"Retreating shooter does not run off its narrow platform");Capture("Schütze-mit-Rückzug");
            Time.timeScale=30;yield return new WaitForSeconds(19);Time.timeScale=1;yield return null;
            Check(!ward.Active && !ward.GetComponentInChildren<LineRenderer>().enabled,"Shield expires after its duration and removes its visible contour");
            LevelRun.CustomLevel=null;LevelRun.SelectedLevel=CampaignCatalog.Id(4,2);UnityEngine.SceneManagement.SceneManager.LoadScene("CampaignLevel");yield return null;yield return null;yield return null;
            Check(FindObjectsByType<WardPickup>(FindObjectsSortMode.None).Length>0 && FindObjectsByType<EnemyAwareness>(FindObjectsSortMode.None).Any(a=>a.keepDistance),"Campaign uses shield supply and cave skirmishers");
            var campaignRoute=FindFirstObjectByType<LevelFactory>().Route;
            int primary=campaignRoute.Select((s,i)=>new{s,i}).Count(a=>a.i>1 && a.i<campaignRoute.Count-1 && (a.s.room==EncounterRoom.Run || a.s.room==EncounterRoom.Cannons || a.s.room==EncounterRoom.SwitchGate || a.s.room==EncounterRoom.Tunnel || (a.s.width>=5 && a.i%2==0 && a.s.room!=EncounterRoom.SplitPath && a.s.room!=EncounterRoom.Collapse)));
            Check(FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length>primary+1,"Broad later encounters contain additional guards instead of the unreachable old width threshold");
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Game and Android versions unchanged during local development");
            Check(artifactPaths.Select(Hash).SequenceEqual(hashes),"All three published 0.7 artifacts remain byte-for-byte unchanged");
        }
        static string Hash(string path){using(var algorithm=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(algorithm.ComputeHash(stream));}
        void Capture(string name)
        {
            var camera=Camera.main??FindFirstObjectByType<Camera>();var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}var rt=new RenderTexture(1280,800,24);var old=camera.targetTexture;var bottom=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).Where(s=>s.verticalNormalizedPosition<.001f).ToArray();camera.targetTexture=rt;Canvas.ForceUpdateCanvases();foreach(var s in bottom)s.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes("Builds/NextDevPreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=old;rt.Release();Destroy(rt);Destroy(image);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
