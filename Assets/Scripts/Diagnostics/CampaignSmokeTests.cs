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
    public class CampaignSmokeTests : MonoBehaviour
    {
        private readonly List<string> results = new List<string>();
        private readonly Dictionary<string,string> original = new Dictionary<string,string>();
        private readonly string[] keys = { ProgressStore.SaveKey,ProgressStore.BackupKey,"TreasureRunner.Progress.recovery" };
        private int originalCoins;
        private bool hadCoins;
        private bool unexpectedError;
        private IEnumerator Start()
        {
            bool previousBackground = Application.runInBackground;
            Application.runInBackground = true;
            DontDestroyOnLoad(gameObject);
            hadCoins = PlayerPrefs.HasKey("Coins"); originalCoins = PlayerPrefs.GetInt("Coins",0);
            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) original[key] = PlayerPrefs.GetString(key);
                PlayerPrefs.DeleteKey(key);
            }
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
            foreach (string key in keys)
                if (original.TryGetValue(key,out string value)) PlayerPrefs.SetString(key,value);
                else PlayerPrefs.DeleteKey(key);
            if (hadCoins) PlayerPrefs.SetInt("Coins",originalCoins); else PlayerPrefs.DeleteKey("Coins");
            PlayerPrefs.Save(); ProgressStore.Reload();
            GameAudio.Instance?.ApplyVolumes();
            Time.timeScale = 1;
            Application.runInBackground = previousBackground;
            if (unexpectedError) { failed = true; results.Add("FAIL: unexpected Unity error was logged"); }
            if (!failed) results.Add("ALL CAMPAIGN TESTS PASSED");
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/CampaignTests.txt",results);
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
        private void CheckWeapon(GameObject owner,string id,string context)
        {
            Check(owner != null,"Weapon owner exists: " + context + " " + id);
            Transform weapon = Array.Find(owner.GetComponentsInChildren<Transform>(),t => t.name == "Equipped " + id);
            Check(weapon != null,"Equipped model exists: " + context + " " + id);
            var renderers = weapon.GetComponentsInChildren<Renderer>();
            Check(renderers.Length > 0,"Equipped model has renderers: " + context + " " + id);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float size = Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            Check(size > .3f && size < 1f && Vector3.Distance(bounds.center,weapon.parent.position) < 1f &&
                Array.TrueForAll(weapon.GetComponentsInChildren<Collider>(),c => !c.enabled),
                "Weapon fits hand without active collision: " + context + " " + id + " size=" + size.ToString("0.000"));
        }
        private IEnumerator Scenarios()
        {
            GameConfig config = GameConfig.Current;
            Check(config != null && config.upgrades.Length == 5 && config.weapons.Length == ArsenalProgression.WeaponCount,"Central configuration has all upgrades and weapons");
            PlayerPrefs.SetInt("Coins",245); ProgressStore.Reload();
            Check(ProgressStore.Data.coins == 245,"Legacy Coins balance migrates without loss");
            string purchase = ShopService.BuyUpgrade(config,0);
            Check(ProgressStore.Data.upgrades[0] == 1 && ProgressStore.Data.coins == 145,"Upgrade and exact price commit together");
            ProgressStore.Reload();
            Check(ProgressStore.Data.upgrades[0] == 1 && ProgressStore.Data.coins == 145,"Upgrades and balance survive reload");
            ProgressStore.Data.coins = 0; ProgressStore.Save();
            ShopService.BuyUpgrade(config,0);
            Check(ProgressStore.Data.coins == 0 && ProgressStore.Data.upgrades[0] == 1,"Insufficient funds leave balance and item unchanged");
            ProgressStore.Data.coins = 10000; ProgressStore.Data.highestUnlockedLevel = 23; ProgressStore.Save();
            ShopService.BuyWeapon(config,"bow");
            int afterPurchase = ProgressStore.Data.coins;
            ShopService.BuyWeapon(config,"bow");
            Check(ProgressStore.Data.weapons.Contains("bow") && ProgressStore.Data.coins == afterPurchase,"Owned weapons cannot be charged twice");
            ShopService.Equip("bow"); ProgressStore.Reload();
            Check(ProgressStore.Data.equippedWeapon == "bow","Equipped weapon persists");
            ShopService.Equip("sword");
            int bonus = ProgressStore.CompleteLevel(0,1000,60,config.TotalLevels);
            int afterBonus = ProgressStore.Data.coins;
            int repeat = ProgressStore.CompleteLevel(0,2000,50,config.TotalLevels);
            Check(bonus == 100 && repeat == 0 && ProgressStore.Data.coins == afterBonus,"First-clear bonus cannot be farmed by replay");
            Check(ProgressStore.Data.FindLevel(0).bestScore == 2000 && ProgressStore.Data.FindLevel(0).bestTime == 50,
                "Per-level best score and time update independently");
            string valid = JsonUtility.ToJson(ProgressStore.Data);
            PlayerPrefs.SetString(ProgressStore.BackupKey,valid);
            PlayerPrefs.SetString(ProgressStore.SaveKey,"broken-json"); ProgressStore.Reload();
            Check(ProgressStore.Data.coins == afterBonus && ProgressStore.LoadNotice != null,"Damaged save recovers from valid backup");
            ProgressStore.Save();
            ProgressStore.Data.version = 2; ProgressStore.Save();
            ProgressStore.Reload();
            Check(ProgressStore.Data.version == 2 && ProgressStore.IsReadOnly,"Future save version is protected against overwrite");
            PlayerPrefs.SetString(ProgressStore.SaveKey,valid); ProgressStore.Reload();
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            var menu = FindFirstObjectByType<MainMenuController>();
            Check(menu != null && EventSystem.current != null,"Main menu and modern input module load");
            foreach (string page in new[] { "Shop","Inventar","Charakter","Einstellungen","Profil","Freunde","Rangliste","Levelauswahl","Start" })
            { menu.Show(page); yield return null; }
            Check(true,"All nine menu pages open without runtime errors");
            // Generated axe/staff models must also fit imported hand bone scales.
            foreach (var weapon in config.weapons)
            {
                if (!ProgressStore.Data.weapons.Contains(weapon.id)) ProgressStore.Data.weapons.Add(weapon.id);
                ProgressStore.Data.equippedWeapon = weapon.id;
                ProgressStore.Save();
                menu.Show("Charakter");
                yield return null; yield return null;
                CheckWeapon(GameObject.Find("Preview stage"),weapon.id,"preview");
                menu.Show("Start");
                yield return null;
            }
            foreach (var weapon in config.weapons)
            {
                ProgressStore.Data.equippedWeapon = weapon.id;
                LevelRun.StartLevel(0);
                yield return null; yield return null; yield return null;
                CheckWeapon(LevelRun.Instance.Player.gameObject,weapon.id,"gameplay");
                if(weapon.id=="sword") foreach(int facing in new[] {-1,1})
                {
                    var player=LevelRun.Instance.Player;
                    player.GetComponent<PlayerMovement>().Face(facing);
                    player.GetComponent<CharacterPresentation>().AttackPose();
                    yield return new WaitForSeconds(.17f);
                    var animator=player.GetComponent<CharacterPresentation>().visual.GetComponentInChildren<Animator>();
                    var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
                    var shoulder=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                    Check((hand.position.x-shoulder.position.x)*facing>.2f,"Sword strike reaches forward: " + facing);
                }
            }
            foreach (string rangedId in new[] { "bow","staff" })
            foreach (int facing in new[] { -1,1 })
            {
                ProgressStore.Data.equippedWeapon = rangedId;
                LevelRun.StartLevel(0);
                yield return null; yield return null; yield return null;
                var rangedRun = LevelRun.Instance;
                Enemy target = Array.Find(FindObjectsByType<Enemy>(FindObjectsSortMode.None),
                    e => e.name == "Goblin" && e.transform.position.x < 20);
                Check(target != null,"Ranged target exists: " + rangedId + " facing " + facing);
                target.speed = 0;
                var rangedBody = rangedRun.Player.GetComponent<Rigidbody>();
                rangedBody.position = target.transform.position + new Vector3(-facing * 4,.55f,0);
                rangedBody.linearVelocity = Vector3.zero;
                rangedRun.Player.GetComponent<PlayerMovement>().Face(facing);
                for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
                rangedRun.Player.GetComponent<PlayerCombat>().Attack();
                yield return new WaitForSeconds(.6f);
                Check((target == null || target.IsDefeated) && rangedRun.DefeatedEnemies == 1,
                    "Normal projectile hits short goblin: " + rangedId + " facing " + facing);
            }
            LevelRun.StartLevel(0);
            yield return null;yield return null;yield return null;
            var awareRun=LevelRun.Instance;
            var awareEnemy=Array.Find(FindObjectsByType<Enemy>(FindObjectsSortMode.None),e=>e.name=="Goblin" && e.transform.position.x<20);
            var awareness=awareEnemy.GetComponent<EnemyAwareness>();
            Check(awareness!=null,"Melee enemy has player detection");
            foreach(var other in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if(other!=awareEnemy) other.gameObject.SetActive(false);
            Vector3 home=awareEnemy.transform.position;
            var awareBody=awareRun.Player.GetComponent<Rigidbody>();
            awareBody.position=home+new Vector3(-3,.55f,0);awareBody.linearVelocity=Vector3.zero;
            float detectionDeadline=Time.time+.6f;
            while(!awareness.HasTarget && Time.time<detectionDeadline) yield return null;
            Vector3 detectedPosition=awareEnemy.transform.position;
            yield return new WaitForSeconds(.15f);
            Check(awareness.HasTarget && Vector3.Distance(detectedPosition,awareEnemy.transform.position)<.03f,"Enemy signals detection before charging");
            yield return new WaitForSeconds(.5f);
            Check(awareness.HasTarget && awareness.TargetFacing==-1 && awareEnemy.transform.position.x<home.x-.1f,"Nearby player is detected and pursued to the left");
            awareRun.Pause();Vector3 pausedEnemy=awareEnemy.transform.position;
            yield return new WaitForSecondsRealtime(.3f);
            Check(Vector3.Distance(pausedEnemy,awareEnemy.transform.position)<.01f,"Pause freezes pursuit");
            awareRun.Resume();
            var sightWall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            sightWall.transform.position=new Vector3((awareEnemy.transform.position.x+awareBody.position.x)*.5f,1,0);sightWall.transform.localScale=new Vector3(.25f,3,3);
            yield return new WaitForSeconds(1.3f);
            Check(!awareness.HasTarget,"Solid wall breaks melee detection after short memory");
            awareBody.position=new Vector3(1.5f,1,0);awareBody.linearVelocity=Vector3.zero;Destroy(sightWall);
            yield return new WaitForSeconds(2.2f);
            Check(awareness.State==EnemyAwarenessState.Patrolling && Mathf.Abs(awareEnemy.transform.position.x-home.x)<1.3f,"Lost target returns enemy to its own patrol");
            awareBody.position=home+new Vector3(3,.55f,0);awareBody.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(.3f);
            Check(awareness.HasTarget && awareness.TargetFacing==1,"Enemy reacquires player on the right");
            awareBody.position=home+new Vector3(4.8f,.55f,0);awareBody.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(2.6f);
            Check(awareEnemy.transform.position.x<=awareness.MaxX+.03f,"Pursuit respects the configured leash");
            awareEnemy.TakeDamage(1000);Vector3 defeatedPosition=awareEnemy.transform.position;
            yield return new WaitForSeconds(.12f);
            Check(!awareness.HasTarget && Vector3.Distance(defeatedPosition,awareEnemy.transform.position)<.01f,"Defeated enemy stops pursuit");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            awareRun=LevelRun.Instance;
            awareEnemy=Array.Find(FindObjectsByType<Enemy>(FindObjectsSortMode.None),e=>Mathf.Abs(e.transform.position.x-62.3f)<.2f);
            Check(awareEnemy!=null,"Ledge pursuit test enemy exists");
            foreach(var other in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if(other!=awareEnemy) other.gameObject.SetActive(false);
            awareBody=awareRun.Player.GetComponent<Rigidbody>();awareBody.position=new Vector3(66.6f,1,0);awareBody.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(2);
            Check(awareEnemy.GetComponent<EnemyAwareness>().HasTarget && awareEnemy.transform.position.x<63.8f,"Enemy notices player across gap but stops before the ledge");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var checkpointRun=LevelRun.Instance;
            checkpointRun.Player.TakeDamage(1);
            var checkpointBody=checkpointRun.Player.GetComponent<Rigidbody>();checkpointBody.position=new Vector3(68,4,0);checkpointBody.linearVelocity=Vector3.zero;
            for(int tick=0;tick<3;tick++) yield return new WaitForFixedUpdate();
            Check(checkpointRun.HasCheckpoint && checkpointRun.Player.health==checkpointRun.Player.MaxHealth,"Airborne checkpoint entry activates recovery point and heals once");
            checkpointRun.Player.RestoreFullHealth();checkpointRun.Player.TakeDamage(1);
            checkpointBody.position=new Vector3(1.5f,1,0);checkpointBody.linearVelocity=Vector3.zero;
            for(int tick=0;tick<3;tick++) yield return new WaitForFixedUpdate();
            checkpointBody.position=new Vector3(68,4,0);checkpointBody.linearVelocity=Vector3.zero;
            for(int tick=0;tick<3;tick++) yield return new WaitForFixedUpdate();
            Check(checkpointRun.Player.health==checkpointRun.Player.MaxHealth-1,"Checkpoint healing cannot be farmed by re-entry");
            LevelRun.StartLevel(0);
            yield return null; yield return null; yield return null;
            var shotRun=LevelRun.Instance;
            var shooter=FindFirstObjectByType<EnemyRangedAttack>();
            Check(shooter!=null,"Archer exists in first forest");
            shooter.GetComponent<Enemy>().speed=0;
            var shotBody=shotRun.Player.GetComponent<Rigidbody>();
            shotBody.position=shooter.transform.position+new Vector3(-3,.55f,0);shotBody.linearVelocity=Vector3.zero;
            // Isolate the attack from other contact enemies in this targeting test.
            foreach(var other in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
                if(other.gameObject!=shooter.gameObject) other.gameObject.SetActive(false);
            var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.name="Ranged cover test";cover.transform.position=shooter.transform.position+new Vector3(-1.5f,1,0);cover.transform.localScale=new Vector3(.4f,3,3);
            yield return new WaitForSeconds(1.4f);
            Check(!shooter.IsPreparing && shooter.ShotsFired==0,"Archer cannot target through solid cover");
            Destroy(cover);yield return null;yield return null;
            float deadline=Time.time+3;
            while(!shooter.IsPreparing && Time.time<deadline) yield return null;
            Check(shooter.IsPreparing,"Archer warns before shooting");
            int hp=shotRun.Player.health;
            shotRun.Pause();int shots=shooter.ShotsFired;
            yield return new WaitForSecondsRealtime(.3f);
            Check(shooter.ShotsFired==shots && shotRun.Player.health==hp,"Pause freezes archer windup and damage");
            shotRun.Resume();
            yield return new WaitForSeconds(1.7f);
            Check(shooter.ShotsFired==1 && shotRun.Player.health<hp,"Visible enemy projectile deals normal damage");
            shooter.GetComponent<Enemy>().TakeDamage(1000);
            shots=shooter.ShotsFired;
            yield return new WaitForSeconds(.1f);
            Check(!shooter.IsPreparing && shooter.ShotsFired==shots,"Defeated archer cancels preparation");
            ProgressStore.Data.equippedWeapon = "sword";
            ProgressStore.Data.upgrades = new int[5]; ProgressStore.Data.highestUnlockedLevel = 23; ProgressStore.Save();
            LevelRun.StartLevel(0);
            yield return null; yield return null;
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            var run = LevelRun.Instance;
            Check(run != null && run.Player != null && run.Player.MaxHealth == 3,"Campaign starts with base player health");
            var movement = run.Player.GetComponent<PlayerMovement>();
            Check(run.Player.GetComponent<Renderer>().enabled == false &&
                run.Player.GetComponent<CharacterPresentation>().visual != null,"Imported animated character replaces capsule rendering");
            var buttons = FindObjectsByType<TouchActionButton>(FindObjectsSortMode.None);
            Check(buttons.Length == 4,"Four independent touch controls exist");
            TouchActionButton right = Array.Find(buttons,b => b.direction == 1);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 5 };
            float before = run.Player.transform.position.x;
            float moveUntil = Time.time + .35f;
            while (Time.time < moveUntil)
            {
                right.OnPointerDown(pointer);
                yield return null;
            }
            right.OnPointerUp(pointer);
            Check(run.Player.transform.position.x > before + .3f,"Touch movement works without a physical keyboard: " +
                before.ToString("0.00") + " -> " + run.Player.transform.position.x.ToString("0.00"));
            TouchActionButton jumpButton = Array.Find(buttons,b => b.jump);
            jumpButton.OnPointerDown(pointer);
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            jumpButton.OnPointerUp(pointer);
            Check(run.Player.GetComponent<Rigidbody>().linearVelocity.y > 0,"Touch jump produces a single ground jump");
            run.Pause(); Check(Time.timeScale == 0 && run.State == RunState.Paused,"Pause stops simulation");
            run.Resume(); Check(Time.timeScale == 1 && run.IsPlaying,"Resume restores simulation");
            run.SetCheckpoint(run.Spawn);
            run.Player.Kill(); yield return null;
            Check(run.State == RunState.Dead,"Campaign death displays recovery actions");
            run.ReviveAtCheckpoint();
            Check(run.Player.health == 3 && run.Revives == 1 && Time.timeScale == 1,"Checkpoint revival restores life and consumes one revive");
            // Use the normally purchased bow for the complete route; sword strikes are covered above.
            ShopService.Equip("bow");
            // Start a fresh attempt with the same two revives available to the player.
            run.Retry();yield return null;yield return null;yield return null;
            for(int settle=0;settle<15;settle++) yield return new WaitForFixedUpdate();
            run=LevelRun.Instance;movement=run.Player.GetComponent<PlayerMovement>();
            // Complete a real level using normal movement, combat and checkpoint recovery.
            movement.SetTouchDirection(1,true);
            var combat = run.Player.GetComponent<PlayerCombat>();
            var nearby = new Collider[24];
            float nextDiagnostic = 0;
            File.WriteAllText("Logs/CampaignTraversal.txt", "Traversal started\n");
            int steps = 0;
            bool gapJump = false;
            float jumpStarted = 0;
            while ((run.IsPlaying || (run.State==RunState.Dead && run.HasCheckpoint && run.Revives>0)) && steps++ < 18000)
            {
                if(run.State==RunState.Dead)
                {
                    run.ReviveAtCheckpoint();movement.ClearInput();yield return null;continue;
                }
                movement.SetTouchDirection(1,true);
                Vector3 origin = run.Player.transform.position;
                bool floorAhead = Physics.Raycast(origin + Vector3.right * 1.05f,Vector3.down,out RaycastHit floorHit,6f,~0,QueryTriggerInteraction.Ignore);
                bool obstacle = Physics.Raycast(origin + Vector3.down * .4f,Vector3.right,out RaycastHit obstacleHit,.95f,~0,QueryTriggerInteraction.Ignore);
                int nearCount = Physics.OverlapBoxNonAlloc(origin,
                    new Vector3(7.7f,4f,.6f),nearby,Quaternion.identity,~0,QueryTriggerInteraction.Collide);
                bool enemyAhead = false;
                bool shotWarning=false;
                Enemy nearestEnemy = null;
                float enemyDistance = float.MaxValue;
                for (int i = 0; i < nearCount; i++)
                {
                    var candidate = nearby[i].GetComponentInParent<Enemy>();
                    if (candidate == null || candidate.IsDefeated) continue;
                    var archer=candidate.GetComponent<EnemyRangedAttack>();
                    if(archer!=null && archer.IsPreparing) shotWarning=true;
                    float distance = Mathf.Abs(candidate.transform.position.x - origin.x);
                    if (distance < enemyDistance)
                    { nearestEnemy = candidate; enemyDistance = distance; enemyAhead = true; }
                }
                int targetFacing=nearestEnemy!=null && nearestEnemy.transform.position.x<origin.x ? -1 : 1;
                bool landingOverFloor=Physics.Raycast(origin,Vector3.down,6f,~0,QueryTriggerInteraction.Ignore);
                if (enemyAhead && (!gapJump || movement.IsGrounded || (landingOverFloor && run.Player.GetComponent<Rigidbody>().linearVelocity.y<0)) &&
                    (movement.IsGrounded ? Mathf.Abs(nearestEnemy.transform.position.y-origin.y)<1.1f : enemyDistance<3))
                {
                    movement.SetTouchDirection(1,enemyDistance>4.5f);
                    movement.SetTouchDirection(-1,false);
                    int away=-targetFacing;
                    bool safeRetreat=Physics.Raycast(origin+Vector3.right*away*.85f,Vector3.down,6f,~0,QueryTriggerInteraction.Ignore);
                    if(enemyDistance<2 && safeRetreat && movement.IsGrounded)
                    { movement.SetTouchDirection(1,false);movement.SetTouchDirection(away,true); }
                    movement.Face(targetFacing);
                }
                else movement.SetTouchDirection(-1,false);
                if ((movement.IsGrounded && Time.time - jumpStarted > .2f) || Time.time - jumpStarted > 1.6f)
                    gapJump = false;
                if (movement.IsGrounded && (!floorAhead || obstacle))
                {
                    gapJump = !floorAhead;
                    jumpStarted = Time.time;
                    movement.RequestJump();
                }
                // Land a stomp/obstacle jump before starting the next gap jump.
                // A player can release the direction button in mid-air as well.
                if (!movement.IsGrounded && !gapJump && !floorAhead &&
                    run.Player.GetComponent<Rigidbody>().linearVelocity.y < 0)
                    movement.SetTouchDirection(1,false);
                if (Time.unscaledTime > nextDiagnostic)
                {
                    nextDiagnostic = Time.unscaledTime + 1;
                    File.AppendAllText("Logs/CampaignTraversal.txt", "t=" + run.Elapsed.ToString("0.0") + " position=" + origin +
                        " velocity=" + run.Player.GetComponent<Rigidbody>().linearVelocity + " hp=" + run.Player.health + " revives=" + run.Revives + " grounded=" + movement.IsGrounded +
                        " floor=" + (floorHit.collider != null ? floorHit.collider.name : "none") +
                        " obstacle=" + (obstacleHit.collider != null ? obstacleHit.collider.name : "none") +
                        " target=" + (nearestEnemy!=null ? nearestEnemy.name+"/"+nearestEnemy.CurrentHealth+" distance="+enemyDistance.ToString("0.00") : "none")+" weapon="+config.EquippedWeapon.id+"\n");
                }
                if(shotWarning && movement.IsGrounded) movement.RequestJump();
                if (nearestEnemy != null) { movement.Face(targetFacing);combat.Attack(); }
                yield return null;
            }
            movement.ClearInput();
            Check(run.State == RunState.Complete,"Automated player traverses forest gaps, fights enemies and reaches portal: " + run.State + " at " + run.Player.transform.position);
            int completedBalance = ProgressStore.Data.coins;
            run.Complete();
            Check(ProgressStore.Data.coins == completedBalance,"Repeated portal callback cannot grant another reward");
            Check(ProgressStore.Data.FindLevel(0) != null && run.FinalScore > 0,"Level result stores a highscore and statistics");
            Directory.CreateDirectory("Builds/WorldPreviews");
            // Load every planned variant to catch world-specific setup failures.
            for (int level = 1; level < config.TotalLevels; level++)
            {
                LevelRun.StartLevel(level);
                yield return null; yield return null;
                if(level%3==0)
                {
                    for(int settle=0;settle<12;settle++) yield return new WaitForFixedUpdate();
                    ScreenCapture.CaptureScreenshot("Builds/WorldPreviews/World-"+(level/3)+".png");
                    yield return null;
                }
                Check(FindFirstObjectByType<LevelFactory>().SegmentCount>=24,"Extended route has at least 24 sections: " + config.LevelName(level));
                Check(FindObjectsByType<EnemyRangedAttack>(FindObjectsSortMode.None).Length>=3,"Ranged enemies populate route: " + config.LevelName(level));
                Check(LevelRun.Instance != null && GameObject.Find("Goal Portal") != null &&
                    FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length > 0,"World layout loads: " + config.LevelName(level));
            }
            run = LevelRun.Instance;
            run.BossAlive = true; run.Complete();
            Check(run.State == RunState.Playing,"Boss must be defeated before the portal completes a world");
            run.BossAlive = false; run.Complete();
            Check(run.State == RunState.Complete,"Defeated boss unlocks portal completion");
        }
    }
}
#endif
