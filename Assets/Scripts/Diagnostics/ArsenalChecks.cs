#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class ArsenalChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Logged(string text,string stack,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/Arsenal");
            string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");var published=Directory.GetFiles("Builds/Publish-0.8.0").Where(p=>p.EndsWith(".apk") || p.EndsWith(".zip")).ToDictionary(p=>p,p=>PatchCodec.Hash(File.ReadAllBytes(p)));
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;var sequence=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            ProgressStore.EndMemoryTest();Time.timeScale=1;Application.logMessageReceived-=Logged;
            try{Check(saved==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real save and backup untouched");Check(published.All(p=>PatchCodec.Hash(File.ReadAllBytes(p.Key))==p.Value),"Published APK and archives unchanged");Check(Application.version=="0.8.0" && PlayerSettings.Android.bundleVersionCode==9,"No app or Android version change");}catch(Exception e){failed=true;report.Add("FAIL: "+e);}
            failed|=errors;report.Add(failed ? "ARSENAL CHECKS FAILED" : "ALL ARSENAL CHECKS PASSED");File.WriteAllLines("Logs/Arsenal/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        void Cleared(int count){var d=ProgressStore.Data;d.levels.Clear();for(int i=0;i<count;i++)d.levels.Add(new LevelRecord{id=i,completions=1,firstBonusClaimed=true});}
        void BuyAvailable()
        {
            var config=GameConfig.Current;var d=ProgressStore.Data;
            foreach(var w in config.weapons)ShopService.BuyWeapon(config,w.id);
            foreach(var c in CharacterCatalog.All)for(int n=0;n<3;n++)CharacterSkills.Buy(c.id);
            for(int i=0;i<config.upgrades.Length;i++)for(int n=0;n<5;n++)ShopService.BuyUpgrade(config,i);
            for(int i=0;i<AdventureUpgrades.Items.Length;i++)for(int n=0;n<5;n++)AdventureUpgrades.Buy(i);
            foreach(var w in config.weapons)for(int n=0;n<3;n++)WeaponUpgrades.Buy(w.id);
        }
        IEnumerator Scenarios()
        {
            LevelRun.CustomLevel=null;var d=ProgressStore.Data;var config=GameConfig.Current;
            Check(config.weapons.Length==8 && config.weapons.Select(w=>w.id).Distinct().Count()==8,"Eight distinct weapon definitions");
            var visualHashes=new HashSet<string>();
            foreach(var w in config.weapons.Where(w=>ArsenalProgression.Owner(w.id)!=null)){var image=MenuWeaponPreview.Get(w.id);Check(image.GetPixels32().Count(p=>p.a>10)>100,"Visible rendered signature weapon: "+w.id);byte[] png=image.EncodeToPNG();visualHashes.Add(PatchCodec.Hash(png));File.WriteAllBytes("Logs/Arsenal/Waffe-"+w.id+".png",png);var patch=new DataPatch{revision=1,appVersion=Application.version,weapons=new[]{new PatchWeapon{id=w.id,damage=w.damage,cooldown=w.cooldown}}};Check(PatchCodec.Validate(patch,Application.version,out _),"Data patch accepts new weapon values: "+w.id);}
            Check(visualHashes.Count==4,"All four signature weapon renders are distinct");
            d.coins=100000;d.highestUnlockedLevel=79;d.levels.Add(new LevelRecord{id=0,completions=999});d.levels.Add(new LevelRecord{id=0,completions=8});d.levels.Add(new LevelRecord{id=99,completions=8});
            Check(ArsenalProgression.Completed==1,"Duplicates, repeats and invalid IDs do not accelerate milestones");
            foreach(var w in config.weapons.Where(w=>ArsenalProgression.Owner(w.id)!=null))
            {
                int required=ArsenalProgression.SignatureUnlock(w.id);Cleared(required-1);string before=JsonUtility.ToJson(d);ShopService.BuyWeapon(config,w.id);Check(before==JsonUtility.ToJson(d),"Signature purchase locked one clear before milestone: "+w.id);
                Cleared(required);int wallet=d.coins;ShopService.BuyWeapon(config,w.id);Check(d.weapons.Contains(w.id) && d.coins==wallet-w.price,"Signature unlocks and charges once at exact milestone: "+w.id);wallet=d.coins;ShopService.BuyWeapon(config,w.id);Check(d.coins==wallet,"Duplicate purchase never charges: "+w.id);
                for(int rank=1;rank<=3;rank++){int gate=ArsenalProgression.WeaponRankRequirement(w.id,rank);Cleared(gate-1);before=JsonUtility.ToJson(d);WeaponUpgrades.Buy(w.id);Check(before==JsonUtility.ToJson(d),"Weapon rank locked at gate minus one: "+w.id+" / "+rank);Cleared(gate);WeaponUpgrades.Buy(w.id);Check(WeaponUpgrades.Level(w.id)==rank,"Weapon rank available at exact gate: "+w.id+" / "+rank);}
            }
            foreach(var c in CharacterCatalog.All)for(int rank=1;rank<=3;rank++)
            {int gate=ArsenalProgression.AbilityRequirement(rank);Cleared(gate-1);string before=JsonUtility.ToJson(d);CharacterSkills.Buy(c.id);Check(before==JsonUtility.ToJson(d),"Ability locked before milestone: "+c.id+" / "+rank);Cleared(gate);CharacterSkills.Buy(c.id);Check(CharacterSkills.StoredLevel(c.id)==rank,"Ability rank bought at exact milestone: "+c.id+" / "+rank);}
            Cleared(71);d.upgrades=Enumerable.Repeat(4,5).ToArray();d.adventureUpgrades=AdventureUpgrades.Items.Select(i=>i.maxLevel-1).ToArray();string locked=JsonUtility.ToJson(d);for(int i=0;i<5;i++){ShopService.BuyUpgrade(config,i);AdventureUpgrades.Buy(i);}Check(locked==JsonUtility.ToJson(d),"Final general and adventure ranks remain locked at 71 clears");
            d.coins=0;d.levels.Clear();d.highestUnlockedLevel=0;d.weapons=new List<string>{"sword"};d.weaponUpgrades=new int[8];d.characterSkills=new int[4];d.upgrades=new int[5];d.adventureUpgrades=new int[5];
            long income=0;for(int i=0;i<72;i++){income+=ProgressStore.CompleteLevel(i,1000,60,80);BuyAvailable();Check(d.coins>=0,"Campaign economy wallet remains nonnegative after clear "+(i+1));if(i==70)Check(d.weaponUpgrades.All(n=>n<3) && d.characterSkills.All(n=>n<3),"No final weapon or ability rank purchased before 72 clears");}
            Check(d.weapons.Count==8 && d.weaponUpgrades.All(n=>n==3) && d.characterSkills.All(n=>n==3) && d.upgrades.All(n=>n==5) && d.adventureUpgrades.Select((n,i)=>n==AdventureUpgrades.Items[i].maxLevel).All(n=>n),"All weapons, skills and upgrades affordable by 72 clears using first-clear bonuses only");
            File.WriteAllText("Logs/Arsenal/Economy.txt","Simulation: unique IDs 0..71; first-clear rewards only, no replay/coin collection/treasure bonus.\nIncome="+income+"\nSpent="+(income-d.coins)+"\nRemaining="+d.coins+"\nFinal purchases after 72 clears; 8 campaign levels remain.\n");
            int replay=d.coins;ProgressStore.CompleteLevel(0,1200,50,80);Check(ArsenalProgression.Completed==72 && d.coins==replay,"Replay neither adds a clear nor repeats the completion bonus");
            var legacy=JsonUtility.FromJson<ProgressData>("{\"version\":1,\"coins\":123,\"weapons\":[\"sword\",\"bow\"],\"weaponUpgrades\":[1,2,3,1]}");Check(legacy.weaponUpgrades.SequenceEqual(new[]{1,2,3,1}) && legacy.coins==123,"Legacy four upgrade entries retain their index and wallet");
            Check(!CharacterSkills.Buy("bad").Contains("verbessert") && !WeaponUpgrades.Buy("bad").Contains("verbessert"),"Unknown skill and weapon cannot be purchased");
            d.weaponUpgrades=new[]{1,2,3,1};int oldWallet=d.coins;WeaponUpgrades.Buy("spear");Check(d.weaponUpgrades.Length==8 && d.weaponUpgrades.Take(4).SequenceEqual(new[]{1,2,3,1}) && d.weaponUpgrades[4]==1 && d.coins==oldWallet-350,"Buying a new weapon rank expands old array while retaining all four ranks");
            Cleared(0);Check(WeaponUpgrades.Level("bow")==3 && CharacterSkills.StoredLevel("goblin")==3,"Already bought weapon and ability ranks are never revoked by new gates");Cleared(72);
            d.characterSkills=new int[4];d.weaponUpgrades=new int[8];d.coins=12345;d.weapons=config.weapons.Select(w=>w.id).ToList();
            foreach(var character in CharacterCatalog.All)
            {
                CharacterCatalog.Select(character.id);string signature=config.weapons.Single(w=>ArsenalProgression.Owner(w.id)==character.id).id;
                ShopService.Equip(signature);Check(d.equippedWeapon==signature,"Owner can equip signature: "+character.id);
                foreach(var w in config.weapons.Where(w=>ArsenalProgression.Owner(w.id)!=null && ArsenalProgression.Owner(w.id)!=character.id)){ShopService.Equip(w.id);Check(d.equippedWeapon==signature,"Other class cannot equip "+w.id+" as "+character.id);}
                LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
                var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();body.useGravity=false;body.position=new Vector3(1000,2,0);body.linearVelocity=Vector3.zero;player.GetComponent<PlayerMovement>().ControlLocked=true;
                yield return new WaitForFixedUpdate();yield return null;Physics.SyncTransforms();
                var combat=player.GetComponent<PlayerCombat>();var presentation=player.GetComponent<CharacterPresentation>();var ability=player.GetComponent<CharacterAbility>();Check(ability.CharacterId==character.id && ability.Rank==0,"Actual player receives selected class with unbought ability disabled");
                var carried=new HashSet<string>();for(int n=0;n<5;n++){carried.Add(config.EquippedWeapon.id);combat.CycleWeapon();}Check(carried.Count==5 && carried.Contains(signature) && carried.All(id=>ArsenalProgression.CanUse(id,character.id)) && config.EquippedWeapon.id==signature,"Actual weapon cycle contains shared weapons and own signature only: "+character.id);
                var enemy=Target(body.position+Vector3.right*(config.EquippedWeapon.ranged ? 3 : signature=="spear" ? 1.65f : 1.25f));int expected=WeaponUpgrades.Damage(config.EquippedWeapon);combat.Attack();yield return new WaitForSeconds(.7f);Check(enemy.CurrentHealth==50-expected,"Real signature animation/projectile hits once: "+signature+" (HP "+enemy.CurrentHealth+")");enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);
                if(!config.EquippedWeapon.ranged){yield return new WaitForSeconds(1.3f);var far=Target(body.position+Vector3.right*3.4f);combat.Attack();yield return new WaitForSeconds(.5f);Check(far.CurrentHealth==50,"Signature blade cannot hit beyond visible reach: "+signature);far.gameObject.SetActive(false);Destroy(far.gameObject);}
                int index=CharacterSkills.Index(character.id);d.characterSkills[index]=3;
                if(character.id=="goblin")
                {var coin=new GameObject("Skill magnet test coin");coin.transform.position=body.position+Vector3.right*1.9f;coin.AddComponent<Coin>();float before=Vector3.Distance(coin.transform.position,player.transform.position);d.adventureUpgrades[0]=0;yield return new WaitForSeconds(.12f);Check(ability.MagnetRange==1.5f && Vector3.Distance(coin.transform.position,player.transform.position)<before-.1f,"Goblin skill attracts a real coin outside normal pickup range");Destroy(coin);}
                else if(character.id=="skeleton-knight")
                {int hearts=player.HeartUnits;player.TakeDamage(1);Check(player.HeartUnits==hearts && ability.Remaining>15,"Knight absorbs actual damage and starts cooldown");yield return new WaitForSeconds(.6f);player.TakeDamage(1);Check(player.HeartUnits==hearts-1,"Knight on cooldown takes ordinary half-heart damage");}
                else
                {var target=Target(body.position+Vector3.right*4);float speed=target.speed;EnemyWeaponGuard.Apply(target,1,signature);Check(character.id=="skeleton-scout" ? target.speed<speed && target.CurrentHealth==49 : target.CurrentHealth==48,"Class skill affects a real signature hit: "+character.id);Check(ability.Remaining>7,"Class skill starts cooldown: "+character.id);EnemyWeaponGuard.Apply(target,1,signature);Check(target.CurrentHealth==(character.id=="skeleton-scout" ? 48 : 47),"Cooldown prevents repeated rune bonus / focus trigger: "+character.id);yield return new WaitForSeconds(2.1f);Check(Mathf.Approximately(target.speed,speed),"Slow expires and restores enemy speed");target.gameObject.SetActive(false);Destroy(target.gameObject);}
                run.Pause();float remaining=ability.Remaining;yield return new WaitForSecondsRealtime(.15f);Check(Mathf.Approximately(ability.Remaining,remaining) && !ability.Absorb(),"Pause freezes and blocks class ability: "+character.id);run.Resume();
                foreach(var shot in FindObjectsByType<ProjectileShot>(FindObjectsSortMode.None))shot.owner.Release(shot);
                d.characterSkills[index]=0;
            }
            foreach(var kind in (WeaponGuardKind[])Enum.GetValues(typeof(WeaponGuardKind)))foreach(var weapon in config.weapons)
            {var enemy=Target(new Vector3(2000,2,0));enemy.gameObject.AddComponent<EnemyWeaponGuard>().kind=kind;bool allowed=kind==WeaponGuardKind.Magic ? ArsenalProgression.IsMagic(weapon.id) : kind==WeaponGuardKind.Spellproof ? !ArsenalProgression.IsMagic(weapon.id) : ArsenalProgression.BreaksArmor(weapon.id);Check(EnemyWeaponGuard.Apply(enemy,1,weapon.id)==allowed && enemy.CurrentHealth==(allowed ? 49 : 50),"Guard matrix: "+kind+" / "+weapon.id);enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
            d.characterId="goblin";d.equippedWeapon="spear";CharacterCatalog.Select("skeleton-knight");Check(d.equippedWeapon=="sword" && d.weapons.Contains("spear"),"Character switch safely equips sword while retaining bought signature");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;var customAbility=LevelRun.Instance.Player.GetComponent<CharacterAbility>();d.characterSkills[1]=3;Check(customAbility.Rank==0 && !customAbility.Absorb(),"Community/editor play ignores paid class skills");
            LevelRun.CustomWeapon="froststaff";Check(config.EquippedWeapon.id=="froststaff","Editor can test every signature independent of character and purchases");Check(BuilderPalette.Entries.Count(a=>a.role==BuilderRole.Weapon && a.id.StartsWith("weapon."))==8,"All eight weapons available in unified editor palette");
            LevelRun.Instance.Menu();yield return null;yield return null;yield return null;var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Shop");yield return null;
            Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("72 Leveln")),"Shop explains final campaign milestone");Capture("Shop",0);menu.Show("Charakter");yield return null;Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Knochenwache")),"Character menu exposes selected class skill and purchase");Capture("Charakter",1);
            string json=JsonUtility.ToJson(d);var restored=JsonUtility.FromJson<ProgressData>(json);Check(restored.characterSkills.SequenceEqual(d.characterSkills) && restored.weaponUpgrades.SequenceEqual(d.weaponUpgrades) && restored.weapons.SequenceEqual(d.weapons) && restored.coins==d.coins,"New abilities and weapons survive save round-trip without losing inventory");
        }
        Enemy Target(Vector3 p){var go=new GameObject("Arsenal target");go.SetActive(false);go.transform.position=p;var enemy=go.AddComponent<Enemy>();enemy.maxHealth=50;enemy.speed=2;enemy.deathDelay=10;go.GetComponent<BoxCollider>().size=new Vector3(.65f,1.3f,.7f);go.SetActive(true);go.GetComponent<EnemyPatrol>().enabled=false;return enemy;}
        void Capture(string name,float position)
        {
            foreach(var scroll in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))scroll.verticalNormalizedPosition=position;Canvas.ForceUpdateCanvases();
            var camera=FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera");var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(2340,1080,24);var previous=camera.targetTexture;camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}Canvas.ForceUpdateCanvases();foreach(var button in FindObjectsByType<MenuButtonMotion>(FindObjectsSortMode.None))button.SendMessage("LateUpdate");Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(2340,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2340,1080),0,0);image.Apply();File.WriteAllBytes("Logs/Arsenal/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=previous;for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
