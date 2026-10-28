#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TreasureRunner
{
    public class AdventureSmokeTests : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private bool unexpectedError;
        private IEnumerator Start()
        {
            bool previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);
            ProgressStore.BeginMemoryTest();
            Application.logMessageReceived += ErrorLog;
            IEnumerator scenarios = Scenarios();
            bool failed = false;
            while (true)
            {
                object next = null; bool more;
                try { more = scenarios.MoveNext(); if (more) next = scenarios.Current; }
                catch (Exception error) { results.Add("FAIL: " + error); failed = true; break; }
                if (!more) break;
                yield return next;
            }
            Application.logMessageReceived -= ErrorLog;
            ProgressStore.EndMemoryTest();
            GameAudio.Instance?.ApplyVolumes();
            Time.timeScale = 1;
            Application.runInBackground = previousBackground;
            if (unexpectedError) { failed = true; results.Add("FAIL: unexpected Unity error was logged"); }
            if (!failed) results.Add("ALL ADVENTURE TESTS PASSED");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/AdventureTests.txt",results);
            foreach (string result in results) Debug.Log(result);
            if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
            else EditorApplication.ExitPlaymode();
        }
        private void ErrorLog(string message,string stack,LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            unexpectedError = true;
            results.Add("UNITY ERROR: " + message + "\n" + stack);
        }
        private void Check(bool condition,string label)
        {
            if (!condition) throw new Exception(label);
            results.Add("PASS: " + label);
        }
        private IEnumerator Scenarios()
        {
            var config=GameConfig.Current;
            ExpansionChecks.Run(Check);
            Check(config!=null,"Game configuration loads");
            ProgressStore.Reload();ProgressStore.Data.coins=10000;ProgressStore.Data.highestUnlockedLevel=23;ProgressStore.Data.upgrades=new int[5];ProgressStore.Save();
            for(int i=0;i<80;i++)ProgressStore.Data.levels.Add(new LevelRecord{id=i,completions=1});
            ShopService.BuyWeapon(config,"bow");ShopService.BuyWeapon(config,"staff");
            var signatures=new HashSet<string>();int[] traps=new int[4];
            for(int level=0;level<config.TotalLevels && !SessionState.GetBool("TreasureRunner.Tests.Focused",false);level++)
            {
                LevelRun.StartLevel(level);yield return null;yield return null;yield return null;
                var factory=FindFirstObjectByType<LevelFactory>();var route=factory.Route;
                Check(route!=null && route.Count>=24,"Long route loads: "+config.LevelName(level));
                string fingerprint="";bool valid=true;
                for(int i=0;i<route.Count;i++)
                {
                    var p=route[i];fingerprint+=p.width.ToString("0.00")+"/"+p.height.ToString("0.00")+";";
                    if(i==0)continue;var prev=route[i-1];float delta=p.height-prev.height;
                    float g=9.81f;float discriminant=49-2*g*delta;
                    float flight=discriminant>=0 ? (7+Mathf.Sqrt(discriminant))/g : 0;
                    float gap=p.x-prev.x-prev.width;
                    // Conservative take-off half a metre before edge, with 0.6 m landing margin.
                    valid &= gap>=-.01f && gap+1.1f<5*flight && p.width>=3.5f;
                }
                Check(valid,"Base jump envelope reaches every adjacent platform: "+config.LevelName(level));
                Check(signatures.Add(fingerprint),"Route geometry differs from previous layouts: "+config.LevelName(level));
                var features=new HashSet<RouteFeature>();float minY=100,maxY=-100,minWidth=100,maxWidth=0;
                foreach(var section in route){features.Add(section.feature);minY=Mathf.Min(minY,section.height);maxY=Mathf.Max(maxY,section.height);minWidth=Mathf.Min(minWidth,section.width);maxWidth=Mathf.Max(maxWidth,section.width);}
                Check(features.Count>=4 && maxWidth-minWidth>4,"Different encounter families and platform rhythms: "+config.LevelName(level));
                Check(maxY-minY>=.9f && (CampaignCatalog.Stage(level)!=1 || maxY-minY>4),"Height profile fits the level family: "+config.LevelName(level));
                var firstSurface=GameObject.Find("Platform 0");
                Check(firstSurface!=null && (CampaignCatalog.World(level)==6 || firstSurface.GetComponent<MeshFilter>().sharedMesh.name.Contains("Biome terrain")),"Actual terrain silhouette applied: "+config.LevelName(level));
                Check(FindFirstObjectByType<ComicWorldArt>()!=null && ComicPalette.Shader!=null && ComicPalette.Shader.isSupported,"Comic renderer available: "+config.LevelName(level));
                var enemies=FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                Check(enemies.Length>=8 && Array.TrueForAll(enemies,e=>e.GetComponent<EnemyAwareness>()!=null),"Populated aware enemies: "+config.LevelName(level));
                Check(FindObjectsByType<EnemyRangedAttack>(FindObjectsSortMode.None).Length>=2,"Ranged positions exist: "+config.LevelName(level));
                foreach(var t in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))traps[(int)t.kind]++;
                Check(GameObject.Find("Goal Portal")!=null,"Portal exists: "+config.LevelName(level));
            }
            if(!SessionState.GetBool("TreasureRunner.Tests.Focused",false))Check(Array.TrueForAll(traps,n=>n>0),"All four trap types are distributed across worlds");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();var movement=player.GetComponent<PlayerMovement>();
            foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
            var testFloor=GameObject.CreatePrimitive(PrimitiveType.Cube);testFloor.transform.position=new Vector3(1000,-.5f,0);testFloor.transform.localScale=new Vector3(20,1,3);
            var testEnemy=new GameObject("Isolated awareness and arrow target");testEnemy.SetActive(false);testEnemy.transform.position=new Vector3(1000,.6f,0);
            var enemy=testEnemy.AddComponent<Enemy>();enemy.speed=1.3f;enemy.maxHealth=1;testEnemy.AddComponent<EnemyAwareness>();testEnemy.SetActive(true);yield return null;
            var home=enemy.transform.position;var awareness=enemy.GetComponent<EnemyAwareness>();
            body.position=home+new Vector3(-3,.4f,0);body.linearVelocity=Vector3.zero;
            Physics.SyncTransforms();yield return new WaitForSeconds(.3f);Check(awareness.HasTarget && awareness.TargetFacing==-1,"Enemy notices nearby player and turns");
            yield return new WaitForSeconds(.6f);Check(enemy.transform.position.x<home.x-.1f,"Enemy pursues player after reaction delay");
            run.Pause();var paused=enemy.transform.position;yield return new WaitForSecondsRealtime(.2f);Check(Vector3.Distance(paused,enemy.transform.position)<.01f,"Pause freezes pursuit");run.Resume();
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=(enemy.transform.position+body.position)*.5f;wall.transform.localScale=new Vector3(.2f,4,3);
            yield return new WaitForSeconds(1.3f);Check(!awareness.HasTarget,"Cover breaks enemy awareness");
            body.position=new Vector3(1.5f,1,0);body.linearVelocity=Vector3.zero;Destroy(wall);yield return new WaitForSeconds(2.8f);
            Check(awareness.State==EnemyAwarenessState.Patrolling,"Lost target returns enemy to patrol");
            // Isolated projectile scenarios keep the player in the air without test movement.
            enemy.speed=0;enemy.GetComponent<EnemyPatrol>().enabled=false;body.useGravity=false;
            ShopService.Equip("bow");player.GetComponent<CharacterPresentation>().RefreshWeapon();
            body.position=enemy.transform.position+new Vector3(-4,3.4f,0);body.linearVelocity=Vector3.zero;movement.Face(1);yield return new WaitForFixedUpdate();
            player.GetComponent<PlayerCombat>().Attack();
            Check(player.GetComponent<PlayerCombat>().AimDirection.y<-.2f,"Airborne bow aims down toward short enemy");
            yield return new WaitForSeconds(.7f);Check(enemy==null || enemy.IsDefeated,"Visible arrow hits enemy below airborne player");
            var bow=FindFirstObjectByType<ProjectilePool>();Check(bow!=null,"Projectile pool created for ranged attack");
            player.GetComponent<PlayerCombat>().CycleWeapon();Check(config.EquippedWeapon.id=="staff","Purchased staff is carried and can be equipped during level");
            Check(Array.Exists(player.GetComponentsInChildren<Transform>(),t=>t.name=="Equipped staff"),"Weapon model refreshes after quick switch");
            Check(PlayerCombat.ElementColor(0)!=PlayerCombat.ElementColor(1) && PlayerCombat.ElementColor(1)!=PlayerCombat.ElementColor(7),"Staff projectile element varies between worlds");
            var shielded=new GameObject("Isolated magic shield");shielded.SetActive(false);shielded.transform.position=new Vector3(1000,1,0);
            var shieldEnemy=shielded.AddComponent<Enemy>();shieldEnemy.maxHealth=2;shieldEnemy.speed=0;shielded.AddComponent<EnemyWeaponGuard>().kind=WeaponGuardKind.Magic;shielded.SetActive(true);
            Check(!EnemyWeaponGuard.Apply(shieldEnemy,3,"bow") && shieldEnemy.CurrentHealth==2,"Magical enemy shield resists ordinary arrows");
            Check(EnemyWeaponGuard.Apply(shieldEnemy,3,"staff") && shieldEnemy.IsDefeated,"Carried staff defeats magical shield enemy");
            var spellproof=new GameObject("Isolated spellproof shield");spellproof.SetActive(false);spellproof.transform.position=new Vector3(1000,1,0);
            var proofEnemy=spellproof.AddComponent<Enemy>();proofEnemy.maxHealth=2;proofEnemy.speed=0;spellproof.AddComponent<EnemyWeaponGuard>().kind=WeaponGuardKind.Spellproof;spellproof.SetActive(true);
            Check(!EnemyWeaponGuard.Apply(proofEnemy,3,"staff") && proofEnemy.CurrentHealth==2,"Spellproof enemy requires a different weapon than staff");
            Check(EnemyWeaponGuard.Apply(proofEnemy,2,"bow") && proofEnemy.IsDefeated,"Bow defeats spellproof enemy");
            LevelRun.StartLevel(6);yield return null;yield return null;yield return null;
            run=LevelRun.Instance;player=run.Player;body=player.GetComponent<Rigidbody>();movement=player.GetComponent<PlayerMovement>();
            Enemy balcony=null;foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(e.name.Contains("Höhenposten")){balcony=e;break;}
            Check(balcony!=null,"Raised ranged enemy balcony exists");
            foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(e!=balcony)e.gameObject.SetActive(false);
            balcony.speed=0;balcony.GetComponent<EnemyRangedAttack>().enabled=false;
            body.useGravity=false;body.position=balcony.transform.position+new Vector3(-6,-1.8f,0);body.linearVelocity=Vector3.zero;movement.Face(1);yield return new WaitForFixedUpdate();
            player.GetComponent<PlayerCombat>().Attack();Check(player.GetComponent<PlayerCombat>().AimDirection.y>.2f,"Staff aims upward to a different platform");
            yield return new WaitForSeconds(.85f);Check(balcony==null || balcony.IsDefeated,"Element projectile reaches enemy on high balcony");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            run=LevelRun.Instance;player=run.Player;body=player.GetComponent<Rigidbody>();foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
            player.TakeDamage(8);Check(player.health==2 && run.State==RunState.Recovering,"Any accepted hit costs one life and begins recovery animation");
            yield return new WaitForSeconds(.6f);Check(run.IsPlaying && Vector3.Distance(body.position,run.Spawn)<.8f,"Nonlethal hit returns player to start");
            run.SetCheckpoint(new Vector3(3,1.1f,0));player.RestoreFullHealth();body.position=new Vector3(7,1,0);player.TakeDamage(2);yield return new WaitForSeconds(.6f);
            Check(player.health==2 && Mathf.Abs(body.position.x-3)<.2f,"Later hit keeps remaining lives and returns to checkpoint");
            player.TakeDamage(1);Check(player.health==2,"Respawn immunity prevents repeated overlapping damage");
            player.RestoreFullHealth();player.GrantInvulnerability(2);body.position=new Vector3(3,-9,0);yield return null;yield return new WaitForSeconds(.6f);
            Check(player.health==2 && Mathf.Abs(body.position.x-3)<.2f && body.position.y>0,"Falling during respawn immunity still costs one life and recovers safely");
            player.RestoreFullHealth();player.health=1;player.TakeDamage(1);yield return null;Check(run.State==RunState.Dead && Time.timeScale==0,"Last life opens normal Game Over");
            run.Retry();yield return null;yield return null;yield return null;run=LevelRun.Instance;
            foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
            foreach(TrapKind kind in Enum.GetValues(typeof(TrapKind)))
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Isolated trap "+kind;go.transform.position=new Vector3(1000,0,0);
                var trap=go.AddComponent<WorldTrap>();trap.kind=kind;trap.floorY=0;yield return null;
                if(kind==TrapKind.CrumblingFloor){run.Player.GetComponent<Rigidbody>().position=new Vector3(1000,1.6f,0);for(int settle=0;settle<12;settle++)yield return new WaitForFixedUpdate();}
                trap.Trigger();
                Check(trap.IsWarning && !trap.IsActive,"Trap warns before activation: "+kind);
                run.Pause();yield return new WaitForSecondsRealtime(.15f);Check(trap.IsWarning && !trap.IsActive,"Pause freezes trap warning: "+kind);run.Resume();
                yield return new WaitForSeconds(1.25f);Check(trap.Activations==1 && trap.IsActive,"Trap activates after warning: "+kind);
                if(kind==TrapKind.CrumblingFloor){Check(!go.GetComponent<Collider>().enabled,"Cracked floor actually removes its collision");Check(!run.Player.GetComponent<PlayerMovement>().IsGrounded && run.Player.transform.position.y<1.5f,"Player actually falls after the floor collapses");}
                yield return new WaitForSeconds(3.8f);Check(!trap.IsActive && (kind!=TrapKind.CrumblingFloor || go.GetComponent<Collider>().enabled),"Trap resets safely: "+kind);Destroy(go);
            }
            Check(GameUpdateClient.IsNewer("v0.3.1","0.3.0") && !GameUpdateClient.IsNewer("0.2.9","0.3.0") && !GameUpdateClient.IsNewer("broken","0.3.0"),"Update versions reject downgrade and malformed tags");
            Check(GameUpdateClient.AllowedDownload("https://github.com/hahawasistlos1/TreasureRunner-Updates/releases/download/v0.3.0/TreasureRunner.apk","hahawasistlos1","TreasureRunner-Updates") && !GameUpdateClient.AllowedDownload("https://evil.example/TreasureRunner.apk","hahawasistlos1","TreasureRunner-Updates"),"Updater accepts only the intended HTTPS release location");
            var expansion=ExpansionRuntimeChecks.Run(Check);while(expansion.MoveNext())yield return expansion.Current;
            run=LevelRun.Instance;run.Menu();yield return null;yield return null;yield return null;
            var menu=FindFirstObjectByType<MainMenuController>();Check(menu!=null,"Redesigned menu opens");
            foreach(var page in new[]{"Start","Levelauswahl","Shop","Inventar","Charakter","Freunde","Rangliste","Einstellungen","Profil","Community"}){menu.Show(page);yield return null;Check(true,"Menu page opens without Unity errors: "+page);}
            LevelEditorScreen.OpenDocument(LevelCodec.Starter());yield return null;
            var editor=FindFirstObjectByType<LevelEditorScreen>();var painting=FindFirstObjectByType<EditorLevelScene>();
            Check(editor!=null && painting!=null,"In-game level editor opens with touch paint surface");
            painting.paint(4,1);Check(LevelEditorScreen.Working.tiles.Exists(t=>t.x==4 && t.y==1),"Editor paints a platform on the selected cell");
            GameObject.Find("Button Level einrichten").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            GameObject.Find("Button Version +1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(LevelEditorScreen.Working.revision==2,"Editor can publish a higher revision of the same level");
            int wallet=ProgressStore.Data.coins,unlocked=ProgressStore.Data.highestUnlockedLevel;
            LevelRun.StartCustom(LevelEditorScreen.Working);yield return null;yield return null;yield return null;
            run=LevelRun.Instance;Check(LevelRun.IsCustom && GameObject.Find("Goal Portal")!=null,"User-authored level loads with a playable goal");
            CoinManager.Instance.AddCoins(25);run.Complete();
            Check(run.State==RunState.Complete && ProgressStore.Data.coins==wallet && ProgressStore.Data.highestUnlockedLevel==unlocked,"Custom play and completion cannot farm campaign currency or unlocks");
            run.Menu();yield return null;yield return null;LevelRun.CustomLevel=null;
        }
    }
}
#endif
