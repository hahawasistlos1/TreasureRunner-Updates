#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class ToolsChecks:MonoBehaviour
    {
        public static string ReportPath="Logs/ToolsTests.txt",PreviewDirectory="Builds/ToolsPreview";
        readonly List<string> report=new List<string>();bool error;
        void Check(bool condition,string message){if(!condition)throw new Exception(message);report.Add("PASS: "+message);}
        void Reject(Action action,string message){bool rejected=false;try{action();}catch(Exception){rejected=true;}Check(rejected,message);}
        void Logged(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){error=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;
            var scenarios=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=scenarios.MoveNext();if(more)current=scenarios.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            Application.logMessageReceived-=Logged;ProgressStore.EndMemoryTest();Time.timeScale=1;failed|=error;
            report.Add(failed ? "TOOLS TESTS FAILED" : "ALL TOOLS TESTS PASSED");File.WriteAllLines(ReportPath,report);foreach(var s in report)Debug.Log(s);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            Directory.CreateDirectory(PreviewDirectory);
            foreach(var result in BuilderChecks.Data())Check(true,result);
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;
            foreach(var entry in BuilderPalette.Entries)
            {
                for(int w=0;w<8;w+=7)
                {
                    var tile=new LevelBlock{kind=entry.kind,asset=entry.id.StartsWith("base.") ? "" : entry.id};
                    var preview=EditorTilePreview.Get(tile,w);
                    if(entry.id=="art.Platformer_block-grass")File.WriteAllBytes(PreviewDirectory+"/Grass-"+w+".png",preview.EncodeToPNG());
                    Check(preview!=null && preview.GetPixels32().Count(p=>p.a>10)>20,"Visible preview in world "+w+": "+entry.id);
                }
                yield return null;
            }
            EditorTilePreview.Clear();
ProgressStore.Data.highestUnlockedLevel=79;
            for(int id=0;id<80;id++)ProgressStore.Data.levels.Add(new LevelRecord{id=id,completions=1});
            string empty=LevelModeration.Empty;var level=LevelCodec.Starter();string hash=EditorTestSession.Fingerprint(level);
            Check(LevelCodec.Validate(level,out _),"New level uses game validator");
            string merged=LevelModeration.Merge(empty,level,hash);Check(LevelModeration.ReadCatalog(merged).levels.Length==1,"Approval prepares a catalog compatible with the game");
            Reject(()=>LevelModeration.Merge(merged,level,hash),"Duplicate revision rejected");level.revision++;Reject(()=>LevelModeration.Merge(merged,level,hash),"Changed revision invalidates the approval snapshot");
            hash=EditorTestSession.Fingerprint(level);string updated=LevelModeration.Merge(merged,level,hash);Check(LevelModeration.ReadCatalog(updated).levels.Length==1 && LevelModeration.ReadCatalog(updated).levels[0].revision==2,"Higher approved revision replaces old entry instead of duplicating it");
            level.tiles.RemoveAll(t=>t.kind==LevelTile.Goal);Reject(()=>LevelModeration.Merge(empty,level,EditorTestSession.Fingerprint(level)),"Unplayable level cannot be approved");
            var user=LevelCodec.Decode(File.ReadAllText("Documentation/UserLevel-Issue-1.trl"));Check(LevelCodec.Validate(user,out _),"Original user test level remains importable unchanged");
            Check(LevelModeration.FromIssue(new LevelIssue{number=1,body=LevelCodec.Encode(user)}).id==user.id,"Issue queue decodes the user's real submission");
            Reject(()=>LevelModeration.FromIssue(new LevelIssue{number=2,body="No level"}),"Unrelated GitHub issues cannot become levels");
            Reject(()=>LevelModeration.FromIssue(new LevelIssue{number=3,body=LevelCodec.Encode(user),pull_request=new IssuePullRequest{url="https://example.invalid"}}),"Pull requests excluded from level queue");
            for(int w=0;w<8;w++)for(int stage=0;stage<10;stage++)
            {
                var route=WorldRoute.Create(w,stage,WorldRoute.SectionCount(w,stage));Check(route.Any(s=>s.room==EncounterRoom.Rest && s.width>=8),"World "+w+" stage "+stage+" retains breathing spaces for optional exploration");
            }
            foreach(string asset in new[]{"Skeleton_Mage","Skeleton_Rogue","Skeleton_Warrior","chest_gold","key","tree_oak","tree_palmTall","mushroom_red","pillar_decorated"})Check(OpenWorldArt.Asset(asset)!=null,"Prepared runtime asset "+asset);
            foreach(string texture in new[]{"circle_05","spark_05","star_04","fire_01","smoke_04"})Check(Resources.Load<Texture2D>("OpenArt/Particles/"+texture)!=null,"Effect texture "+texture);
            for(int world=0;world<8;world++)
            {
                int expected=CampaignCatalog.Id(world,0);LevelRun.StartLevel(expected);yield return null;yield return null;yield return null;
                Check(LevelRun.SelectedLevel==expected && CampaignCatalog.World(LevelRun.SelectedLevel)==world,"Requested world "+world+" actually loaded");
                Check(FindFirstObjectByType<OpenWorldArt>().transform.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Kulisse ·")),"World "+world+" uses the new scenery assets");
                if(world==2 || world==4 || world==6)
                {
                    var skeletons=FindObjectsByType<SkeletonMotion>(FindObjectsSortMode.None);Check(skeletons.Length>0,"World "+world+" has skeletal enemies");
                    Check(skeletons.All(s=>s.GetComponentInChildren<SkinnedMeshRenderer>()!=null && s.GetComponentInChildren<Animation>()!=null && s.GetComponentInChildren<Animation>()["Walking_A"]!=null && s.GetComponentInChildren<Animation>()["Idle"]!=null),"Skeletons have skinned models and walking/idle clips");
                }
                Capture("Welt-"+world);
            }
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            Check(LevelRun.Instance.Player.GetComponentsInChildren<SkinnedMeshRenderer>().All(r=>r.bounds.size.y<3 && r.bounds.size.x<3),"Player model stays aligned to its capsule instead of expanding across screen");
            BuilderChecks.Joystick(FindFirstObjectByType<VirtualJoystick>());Check(true,"Joystick analog direction, dead zone, owner finger and pause release");Capture("Joystick");
            var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();player.GrantInvulnerability(999);
            foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
            foreach(var t in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))t.enabled=false;
            var room=FindFirstObjectByType<SecretRoom>();Check(room!=null,"First campaign level includes an optional secret room");
            Vector3 before=body.position;room.Interact(1,player);Check(body.position==before && !room.HasKey,"Locked door cannot be entered without its key");
            room.Interact(0,player);Check(room.HasKey,"Key collected once");int coins=ProgressStore.Data.coins;room.Interact(1,player);yield return new WaitForSeconds(.2f);
            Check(body.position.x<-100 && Mathf.Abs(body.position.z)<.01f,"Secret chamber is outside route scenery and preserves the 2.5D movement plane");
            player.GetComponent<PlayerMovement>().SetTouchDirection(1,true);yield return new WaitForSeconds(.35f);player.GetComponent<PlayerMovement>().SetTouchDirection(1,false);
            Check(body.position.x<-100 && Mathf.Abs(body.position.z)<.01f && body.position.y>run.Spawn.y-6,"Player remains supported inside secret room");
            room.Interact(2,player);room.Interact(2,player);Check(room.Claimed && ProgressStore.Data.coins==coins+30,"Secret reward paid exactly once, including physical chest contact");
            Check(SpecialTreasure.Claim("secret.0")==0,"Reloading cannot farm an already claimed special treasure");Capture("Geheimraum");
            room.Interact(3,player);yield return new WaitForSeconds(.1f);Check(body.position.x>=0 && Mathf.Abs(body.position.z)<.01f,"Secret exit returns to the main path");
            LevelRun.StartLevel(CampaignCatalog.Id(0,9));yield return null;yield return null;yield return null;
            run=LevelRun.Instance;player=run.Player;body=player.GetComponent<Rigidbody>();var boss=FindFirstObjectByType<WorldBoss>();Check(boss!=null && run.BossAlive,"World finale has a living world guardian");
            foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(e.gameObject!=boss.gameObject)e.gameObject.SetActive(false);
            foreach(var t in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))t.enabled=false;
            body.useGravity=false;player.GetComponent<PlayerMovement>().ControlLocked=true;body.position=boss.transform.position+Vector3.left*3;player.GrantInvulnerability(999);
            float deadline=Time.time+8;while(boss.Phase!=BossPhase.Warning && Time.time<deadline)yield return null;
            Check(boss.Phase==BossPhase.Warning,"Boss visibly warns before area attack");Capture("Bosswarnung");
            yield return new WaitForSeconds(.6f);Check(boss.Phase==BossPhase.Warning,"Boss warning gives more than half a second to react");
            deadline=Time.time+5;while(boss.Phase!=BossPhase.Recovery && Time.time<deadline)yield return null;
            Check(boss.Phase==BossPhase.Recovery,"Boss has an attack recovery window");
            coins=ProgressStore.Data.coins;boss.GetComponent<Enemy>().TakeDamage(999);Check(!run.BossAlive && ProgressStore.Data.coins>=coins+75,"Defeating boss opens goal and awards unique special treasure");
            Check(SpecialTreasure.Claim("boss."+CampaignCatalog.Id(0,9))==0,"Boss reward cannot be repeatedly farmed");
            foreach(var step in BuilderChecks.Gameplay(Check))yield return step;
            string savedWeapon=ProgressStore.Data.equippedWeapon;int owned=ProgressStore.Data.weapons.Count;
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;
            var available=new HashSet<string>();for(int i=0;i<4;i++){available.Add(GameConfig.Current.EquippedWeapon.id);LevelRun.Instance.Player.GetComponent<PlayerCombat>().CycleWeapon();}
            Check(available.Count==4 && ProgressStore.Data.equippedWeapon==savedWeapon && ProgressStore.Data.weapons.Count==owned,"Custom tests have four base weapons and preserve campaign purchases/equipment");
            LevelRun.Instance.Menu();yield return null;yield return null;
            LevelEditorScreen.Open();yield return null;
            var choose=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Bauteil wählen");choose.onClick.Invoke();yield return null;Capture("Palette-Boden");
            foreach(string category in new[]{"Gegner","Fallen","Aufzüge"})
            {var tab=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==category);tab.onClick.Invoke();yield return null;Capture("Palette-"+category);}
            var closePalette=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Schließen");closePalette.onClick.Invoke();yield return null;
            FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Menü").onClick.Invoke();yield return null;
            CreditsPage.Open();yield return null;Capture("Credits");
            var texts=FindObjectsByType<Text>(FindObjectsSortMode.None);Check(texts.Any(t=>t.text.Contains("Kay Lousberg")) && texts.Any(t=>t.text.Contains("Kenney")) && texts.Any(t=>t.text.Contains("DM Dungeon Mason")),"Credits visibly identify new and existing creators");
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Authorized current release identity");
        }
        void Capture(string name)
        {
            var camera=Camera.main ?? FindFirstObjectByType<Camera>();if(camera==null)return;
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            Canvas.ForceUpdateCanvases();var target=new RenderTexture(1280,800,24);var old=camera.targetTexture;camera.targetTexture=target;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();File.WriteAllBytes(PreviewDirectory+"/"+name+".png",texture.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=old;target.Release();Destroy(target);Destroy(texture);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
