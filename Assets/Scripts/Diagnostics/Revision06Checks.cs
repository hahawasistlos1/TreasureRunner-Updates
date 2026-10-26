#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class Revision06Checks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool error;
        void Check(bool ok,string label){if(!ok)throw new Exception(label);report.Add("PASS: "+label);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){error=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
            var scenarios=Scenarios();bool failed=false;
            while(true){bool more;object value=null;try{more=scenarios.MoveNext();if(more)value=scenarios.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return value;}
            failed|=error;Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
            report.Add(failed ? "REVISION 0.6 TESTS FAILED" : "ALL REVISION 0.6 TESTS PASSED");Directory.CreateDirectory("Logs");File.WriteAllLines(SessionState.GetBool("TR.Revision06.Focused",false) ? "Logs/Revision06Focused.txt" : "Logs/Revision06Tests.txt",report);Debug.Log(report[report.Count-1]);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            Directory.CreateDirectory("Builds/Revision06Preview");var data=ProgressStore.Data;var config=GameConfig.Current;
            var legacy=new ProgressData{coins=900,highestUnlockedLevel=23,playerName="Testprofil",skin="Waldläufer",musicVolume=.2f};legacy.weapons.Add("staff");legacy.levels.Add(new LevelRecord{id=4,completions=1});legacy.upgrades[2]=5;
            Check(CampaignMigration.Apply(legacy,false) && legacy.coins==0 && legacy.weapons.Count==1 && legacy.levels.Count==0 && legacy.upgrades[2]==0,"Requested one-time campaign reset clears campaign inventory and records");
            Check(legacy.playerName=="Testprofil" && legacy.skin=="Waldläufer" && legacy.musicVolume==.2f,"Reset preserves profile and audio settings");
            legacy.coins=25;Check(!CampaignMigration.Apply(legacy,false) && legacy.coins==25,"Serialized migration stamp prevents a second reset");
            var recovered=new ProgressData{coins=36};Check(!CampaignMigration.Apply(recovered,true) && recovered.coins==36,"Independent reset marker protects recovered saves");
            data.coins=100000;data.highestUnlockedLevel=23;for(int id=0;id<80;id++)data.levels.Add(new LevelRecord{id=id,completions=1});
            foreach(var id in new[]{"axe","bow","staff"})ShopService.BuyWeapon(config,id);
            var bow=Array.Find(config.weapons,w=>w.id=="bow");var staff=Array.Find(config.weapons,w=>w.id=="staff");
            data.upgrades[3]=5;Check(WeaponUpgrades.Damage(bow)==1 && WeaponUpgrades.Damage(staff)==2,"General attack upgrades cannot amplify ranged damage");data.upgrades[3]=0;
            int wallet=data.coins;int price=WeaponUpgrades.Price("bow");WeaponUpgrades.Buy("bow");Check(data.coins==wallet-price && WeaponUpgrades.Level("bow")==1 && WeaponUpgrades.Level("staff")==0,"Weapon-specific purchase charges once and improves only selected weapon");
            WeaponUpgrades.Buy("bow");Check(WeaponUpgrades.Damage(bow)==2,"Second bow rank grants bounded damage milestone");data.weaponUpgrades[2]=3;wallet=data.coins;WeaponUpgrades.Buy("bow");Check(wallet==data.coins,"Weapon cap prevents spending at maximum rank");data.weaponUpgrades=new int[4];
            var catalog=JsonUtility.FromJson<CommunityCatalog>(Resources.Load<TextAsset>("CommunityStarter").text);Check(catalog.levels.Length==0,"Four bundled demonstration levels removed");
            var userLevel=LevelCodec.Decode(File.ReadAllText("Documentation/UserLevel-Issue-1.trl"));Check(LevelCodec.Validate(userLevel,out var why),"Actual GitHub issue #1 decodes and passes structural reachability: "+why);
            Check(CommunityIssueImporter.IsReference("https://github.com/hahawasistlos1/TreasureRunner-Updates/issues/1",out int number) && number==1 && !CommunityIssueImporter.IsReference("https://other.example/issues/1",out _),"Issue import accepts only the intended public repository");
            var importer=new GameObject("Actual issue import transport test").AddComponent<CommunityIssueImporter>();CommunityLevel downloaded=null;string importError=null;
            importer.Fetch(1,l=>downloaded=l,s=>importError=s);float deadline=Time.realtimeSinceStartup+25;
            while(downloaded==null && Time.realtimeSinceStartup<deadline)yield return null;
            Check(downloaded!=null && downloaded.id==userLevel.id,"Native Unity HTTPS importer retrieves user's real GitHub level: "+importError);Destroy(importer.gameObject);
            var signatures=new HashSet<string>();int[] trapCounts=new int[4];
            for(int id=0;id<(SessionState.GetBool("TR.Revision06.Focused",false) ? 0 : 80);id++)
            {
                LevelRun.StartLevel(id);yield return null;yield return null;
                var run=LevelRun.Instance;var factory=FindFirstObjectByType<LevelFactory>();var route=factory.Route;
                Check(route.Count>=44,"Long campaign route builds: "+id);
                Check(signatures.Add(string.Join(";",route.Select(s=>s.width+"/"+s.height+"/"+s.gap))),"Unique main-path geometry: "+id);
                bool reachable=true;int gaps=0;float total=route.Last().x+route.Last().width;
                for(int i=1;i<route.Count;i++){var a=route[i-1];var b=route[i];float gap=b.x-a.x-a.width;reachable&=JumpProfile.CanCross(gap,b.height-a.height) && Mathf.Abs(gap-a.gap)<.01f;if(gap>=2.3f)gaps++;}
                Check(reachable && gaps>=route.Count/2,"Main-path gaps fit unupgraded physical jump and are genuine open gaps: "+id);
                Check(total>360,"Sustained route length above 360 metres: "+id);
                var enemies=FindObjectsByType<Enemy>(FindObjectsSortMode.None);Check(enemies.Length>=8 && enemies.All(e=>e.GetComponent<EnemyAwareness>()!=null && e.maxHealth>=3),"Aware enemies require multiple base ranged hits: "+id);
                Check(GameObject.Find("Goal Portal")!=null,"Goal exists: "+id);
                foreach(var trap in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None)){trapCounts[(int)trap.kind]++;if(trap.kind==TrapKind.Cannon || trap.kind==TrapKind.Flame)Check(trap.GetComponent<Collider>()!=null && !trap.GetComponent<Collider>().isTrigger,"Trap has a solid matching body: "+id+"/"+trap.kind);}
                if(CampaignCatalog.Stage(id)==0)
                {
                    var s=route[route.Count/2];var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(s.Center,s.height+4,-20);camera.transform.rotation=Quaternion.Euler(8,0,0);
                    Capture(camera,"World-"+CampaignCatalog.World(id));
                }
            }
            if(!SessionState.GetBool("TR.Revision06.Focused",false))Check(trapCounts.All(n=>n>0),"All four trap types remain distributed across the rebuilt campaign");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var levelRun=LevelRun.Instance;var player=levelRun.Player;var movement=player.GetComponent<PlayerMovement>();var body=player.GetComponent<Rigidbody>();
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);foreach(var trap in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))trap.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(40,1,3);
            body.position=new Vector3(1000,1.02f,0);body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.2f);
            float baseline=body.position.y,start=Time.time,peak=baseline;movement.SetTouchDirection(1,true);movement.Bounce(movement.jumpForce);
            for(int frame=0;frame<70;frame++){yield return new WaitForFixedUpdate();peak=Mathf.Max(peak,body.position.y);if(Time.time-start>.2f && movement.IsGrounded)break;}
            float flight=Time.time-start;movement.ClearInput();Check(flight>.6f && flight<.9f && peak-baseline>1.35f && peak-baseline<1.7f,"Measured jump arc: "+flight.ToString("0.00")+" s, "+(peak-baseline).ToString("0.00")+" m rise");
            Check(body.position.x-1000>3.4f && body.position.x-1000<4.8f,"Measured horizontal jump distance is bounded: "+(body.position.x-1000).ToString("0.00"));
            movement.ControlLocked=true;body.useGravity=false;body.position=new Vector3(1000,1,0);body.linearVelocity=Vector3.zero;player.GrantInvulnerability(30);
            ShopService.Equip("sword");player.GetComponent<CharacterPresentation>().RefreshWeapon();yield return null;
            var near=Target("Blade contact target",new Vector3(1001.15f,.65f,0),20);var far=Target("Out of reach target",new Vector3(1002.8f,.65f,0),20);yield return null;
            var combat=player.GetComponent<PlayerCombat>();movement.Face(1);combat.Attack();Check(near.CurrentHealth==20 && far.CurrentHealth==20,"Sword wind-up never deals instant area damage");
            yield return new WaitForSeconds(.3f);Check(near.CurrentHealth<20 && far.CurrentHealth==20,"Animated visible blade hits nearby target and misses distant target");
            yield return new WaitForSeconds(.5f);near.transform.position=new Vector3(1001.15f,.65f,0);int before=near.CurrentHealth;
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1000.76f,1,0);wall.transform.localScale=new Vector3(.15f,2,2);Physics.SyncTransforms();combat.Attack();yield return new WaitForSeconds(.4f);Check(near.CurrentHealth==before,"Sword cannot damage through solid cover");Destroy(wall);Destroy(near.gameObject);Destroy(far.gameObject);yield return null;
            var ranged=Target("Ranged balance target",new Vector3(1004,.65f,0),12);ShopService.Equip("bow");player.GetComponent<CharacterPresentation>().RefreshWeapon();yield return new WaitForSeconds(.6f);combat.Attack();yield return new WaitForSeconds(.7f);Check(ranged.CurrentHealth==11,"Actual base arrow removes one HP rather than killing a healthy enemy");
            ShopService.Equip("staff");player.GetComponent<CharacterPresentation>().RefreshWeapon();yield return new WaitForSeconds(.7f);combat.Attack();yield return new WaitForSeconds(.7f);Check(ranged.CurrentHealth==9,"Actual base rune projectile removes two HP");Destroy(ranged.gameObject);
            var mat=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
            foreach(var kind in new[]{TrapKind.Cannon,TrapKind.Flame})
            {
                var cannon=TrapModel.Build(null,kind,new Vector3(1006,0,0),0,mat,mat);body.useGravity=true;body.position=new Vector3(1006,3.5f,0);body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.7f);
                float feet=player.GetComponent<Collider>().bounds.min.y;Check(feet>=.76f && feet<1f,"Player lands ON solid "+kind+" housing: feet="+feet.ToString("0.00"));Destroy(cannon);yield return null;
            }
            Destroy(mat);body.useGravity=false;body.position=new Vector3(1000,1,0);body.linearVelocity=Vector3.zero;
            var first=AdventureMusic.Compose(0);var second=AdventureMusic.Compose(1);Check(first.name!=second.name && first.samples>500000 && second.samples>500000,"Original multibar music arrangements differ per level");Destroy(first);Destroy(second);
            LevelRun.CustomLevel=null;EditorTestSession.Clear();HomeScreen.ReturnToPlayerMenu=false;SceneManager.LoadScene("MainMenu");yield return null;yield return null;
            var home=FindFirstObjectByType<HomeScreen>();Check(home!=null && FindFirstObjectByType<MainMenuController>()==null,"Home opens first and player menu waits for a button");UIShot("Home");home.OpenPlayerMenu();yield return null;
            var menu=FindFirstObjectByType<MainMenuController>();Check(menu!=null,"Start button opens player menu");menu.Show("Shop");yield return null;UIShot("Shop");
            LevelEditorScreen.Working=LevelCodec.Starter();LevelEditorScreen.Open();yield return null;
            for(int i=0;i<14;i++){var icon=EditorTilePreview.Get((LevelTile)i,0);Check(icon!=null && icon.GetPixels().Any(c=>c.a>.5f),"Actual game-built tile preview renders: "+(LevelTile)i);}
            UIShot("Editor");var draft=LevelEditorScreen.Working;Check(!EditorTestSession.Passed(draft),"New draft has no optional completion proof");
            var editor=FindFirstObjectByType<LevelEditorScreen>();editor.SendMessage("Submit");Check(GameObject.Find("Dialog")!=null,"Untested valid draft opens submission dialog");
            data.weaponUpgrades[2]=2;EditorTestSession.Begin(draft);LevelRun.StartCustom(draft);yield return null;yield return null;
            Check(WeaponUpgrades.Level("bow")==0,"Creator test uses base weapon ranks without changing saved upgrades");
            levelRun=LevelRun.Instance;int savedWallet=data.coins;levelRun.Complete();Check(EditorTestSession.Passed(draft) && data.coins==savedWallet,"Completing exact editor-test draft grants certification without campaign currency");
            draft.revision++;Check(!EditorTestSession.Passed(draft),"Editing a tested draft invalidates its certification");levelRun.Menu();yield return null;yield return null;Check(FindFirstObjectByType<LevelEditorScreen>()!=null,"Test exit returns directly to editor");
            Check(!LevelRun.IsCustom && WeaponUpgrades.Level("bow")==2,"Leaving creator test restores real shop ranks and cannot cause repeated rank-zero purchases");
            EditorTestSession.Clear();LevelRun.StartCustom(userLevel);yield return null;yield return null;
            Check(LevelRun.CustomLevel.id==userLevel.id && GameObject.Find("Goal Portal")!=null,"User's actual GitHub test world opens with intact goal and original ID");
            LevelRun.Instance.Menu();yield return null;yield return null;
            Check(Resources.Load<Texture2D>("Brand/TreasureRunnerIcon")!=null,"Original launcher and Home icon included");
        }
        Enemy Target(string name,Vector3 position,int hp)
        {
            var o=new GameObject(name);o.SetActive(false);o.transform.position=position;var e=o.AddComponent<Enemy>();e.maxHealth=hp;e.speed=0;e.deathDelay=2;o.GetComponent<BoxCollider>().size=new Vector3(.65f,1.2f,.7f);o.SetActive(true);o.GetComponent<EnemyPatrol>().enabled=false;return e;
        }
        void UIShot(string name)
        {
            var camera=FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera");camera.cullingMask=1<<5;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;}
            Canvas.ForceUpdateCanvases();Capture(camera,name);
        }
        void Capture(Camera camera,string name)
        {
            var target=new RenderTexture(1560,720,24);camera.targetTexture=target;camera.aspect=1560f/720;Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var image=new Texture2D(1560,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1560,720),0,0);image.Apply();File.WriteAllBytes("Builds/Revision06Preview/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Destroy(image);target.Release();Destroy(target);
        }
    }
}
#endif
