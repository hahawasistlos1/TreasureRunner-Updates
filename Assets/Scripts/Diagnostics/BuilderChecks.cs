#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public static class BuilderChecks
    {
        public static IEnumerable<string> Data()
        {
            var ids=new HashSet<string>();
            foreach(var a in BuilderPalette.Entries)
            {
                if(!ids.Add(a.id))throw new Exception("Duplicate builder id "+a.id);
                if(!BuilderPalette.Categories.Contains(a.category))throw new Exception("Hidden category "+a.id);
                var l=LevelCodec.Starter();l.format=a.format;l.tiles.Add(new LevelBlock{x=4,y=4,kind=a.kind,asset=a.id});
                if(a.kind==LevelTile.Goal)l.tiles.RemoveAll(t=>t.kind==LevelTile.Goal && t.x!=4);
                // Goal at height 4 is intentionally disconnected; use an existing floor cell instead.
                if(a.kind==LevelTile.Goal){l.tiles.RemoveAll(t=>t.x==4 && t.y==4);l.tiles.RemoveAll(t=>t.x==4 && t.y==0);l.tiles.Add(new LevelBlock{x=4,y=0,kind=a.kind,asset=a.id});}
                if(!LevelCodec.Validate(l,out string why))throw new Exception(a.id+": "+why);
                var copy=LevelCodec.Decode(LevelCodec.Encode(l));
                if(!copy.tiles.Any(t=>t.asset==a.id))throw new Exception("Variant lost "+a.id);
                yield return "Catalog round trip: "+a.id;
            }
            foreach(var model in Resources.LoadAll<GameObject>("OpenArt/Prefabs"))
                if(!BuilderPalette.Entries.Any(a=>a.Model==model))throw new Exception("Missing editor model "+model.name);
            yield return "Every prepared asset exposed without a world filter";
            var level=LevelCodec.Starter();string hash=EditorTestSession.Fingerprint(level);
            string json=LevelModeration.Merge(LevelModeration.Empty,level,hash);
            if(LevelModeration.ReadCatalog(LevelModeration.Withdraw(json,level.id,hash)).levels.Length!=0)throw new Exception("Withdrawal failed");
            bool rejected=false;try{LevelModeration.Withdraw(json,level.id,"stale");}catch(InvalidOperationException){rejected=true;}
            if(!rejected)throw new Exception("Stale withdrawal allowed");
            yield return "Withdrawal removes exact approved revision and rejects stale selection without network writes";
            var second=LevelCodec.Starter();second.format=2;
            if(LevelModeration.PathFor(level)!=LevelModeration.CurrentPath || LevelModeration.PathFor(second)!=LevelModeration.CurrentPath || LevelModeration.CatalogPaths.Contains(LevelModeration.LegacyPath))throw new Exception("New client would receive old tool catalog");
            yield return "All current level formats use exact-version catalog isolated from old moderation tools";
            if(LevelModeration.WriteBody("create",LevelModeration.Empty,null).Contains("\"sha\""))throw new Exception("Creation incorrectly includes a SHA");
            if(!LevelModeration.WriteBody("update",LevelModeration.Empty,"existing").Contains("\"sha\":\"existing\""))throw new Exception("Update lost its concurrency guard");
            yield return "New catalogs omit SHA; existing catalogs require exact SHA";

            var newer=LevelCodec.Decode(LevelCodec.Encode(level));newer.format=2;newer.revision++;
            var combined=LevelModeration.Combine(new CommunityCatalog{moderationProtocol=LevelModeration.Protocol,gameVersion=ReleaseInfo.Version,levels=new[]{level}},new CommunityCatalog{moderationProtocol=LevelModeration.Protocol,gameVersion=ReleaseInfo.Version,levels=new[]{newer}});
            if(combined.levels.Length!=1 || combined.levels[0].revision!=2)throw new Exception("Duplicate catalog listing");
            yield return "Combined selection uses newest revision";
            level.format=2;level.tiles.Add(new LevelBlock{x=4,y=4,kind=LevelTile.Coin,asset="../arbitrary"});
            if(LevelCodec.Validate(level,out _))throw new Exception("Unknown asset accepted");
            yield return "Unrecognized asset paths rejected";
        }
        public static IEnumerable<object> Gameplay(Action<bool,string> check)
        {
            var l=LevelCodec.Starter(5);l.format=2;
            l.tiles.RemoveAll(t=>t.x==6 && t.y==0);l.tiles.Add(new LevelBlock{x=6,y=0,kind=LevelTile.Ground,asset="art.Platformer_block-snow"});
            foreach(var pair in new[]{(20,"art.Platformer_trap-spikes"),(30,"art.Platformer_saw"),(41,"enemy.boss.0"),(47,"enemy.boss.7")})
            {l.tiles.RemoveAll(t=>t.x==pair.Item1 && t.y==0);var a=BuilderPalette.Get(pair.Item2);l.tiles.Add(new LevelBlock{x=pair.Item1,y=0,kind=a.kind,asset=a.id});}
            l.tiles.Add(new LevelBlock{x=10,y=2,kind=LevelTile.Coin,asset="weapon.bow"});
            int saved=ProgressStore.Data.coins;string weapon=ProgressStore.Data.equippedWeapon;
            LevelRun.StartCustom(l);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();player.GetComponent<PlayerMovement>().ControlLocked=true;body.useGravity=false;
            foreach(var boss in UnityEngine.Object.FindObjectsByType<WorldBoss>(FindObjectsSortMode.None))boss.enabled=false;
            var bosses=UnityEngine.Object.FindObjectsByType<WorldBoss>(FindObjectsSortMode.None);check(bosses.Length==2 && run.BossAlive,"World-independent bosses can coexist in one custom level");
            bosses[0].GetComponent<Enemy>().TakeDamage(999);check(run.BossAlive,"First defeated boss cannot unlock portal while another remains");
            bosses[1].GetComponent<Enemy>().TakeDamage(999);check(!run.BossAlive,"Last defeated boss unlocks custom portal");
            var snow=UnityEngine.Object.FindObjectsByType<OwnedBlockMesh>(FindObjectsSortMode.None).First(m=>m.mesh.name.Contains("Platformer_block-snow"));
            check(Mathf.Abs(snow.GetComponent<Renderer>().bounds.max.y-snow.GetComponent<Collider>().bounds.max.y)<.02f,"Chosen snow block keeps correct standing surface in dark forest");
            var coin=UnityEngine.Object.FindObjectsByType<Coin>(FindObjectsSortMode.None).First();check(coin.GetComponent<Renderer>()==null && coin.GetComponentInChildren<MeshRenderer>()!=null,"Gameplay coins use imported asset mesh instead of primitive cylinder");
            int before=run.CollectedCoins;body.position=coin.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.12f);check(run.CollectedCoins>before,"Imported coin still collects through real physics trigger");
            var pickup=UnityEngine.Object.FindFirstObjectByType<BuilderWeaponPickup>();body.position=pickup.transform.position;Physics.SyncTransforms();yield return new WaitForSeconds(.12f);
            check(LevelRun.CustomWeapon=="bow" && player.GetComponentInChildren<WeaponModel>().name=="Equipped bow","Editor weapon pickup equips and visibly updates weapon through real trigger");
            var hazard=UnityEngine.Object.FindObjectsByType<AssetHazard>(FindObjectsSortMode.None).First(h=>!h.saw);
            float timeout=Time.time+5;while(!hazard.Dangerous && Time.time<timeout)yield return null;
            player.GrantInvulnerability(0);int hearts=player.HeartUnits;body.position=hazard.transform.position+Vector3.up*1.05f;Physics.SyncTransforms();yield return new WaitForSeconds(.1f);
            check(player.HeartUnits==hearts-1 && player.health==3,"Imported spikes remove half a heart without consuming a life");
            body.position=new Vector3(2,1.05f,0);Physics.SyncTransforms();
            check(ProgressStore.Data.coins==saved && ProgressStore.Data.equippedWeapon==weapon,"Mixed asset test preserves campaign money, weapons and boss rewards");
        }
        public static void Joystick(VirtualJoystick stick)
        {
            stick.SetDeflection(new Vector2(.1f,0));if(stick.Axis!=0)throw new Exception("Joystick center drift");
            stick.SetDeflection(Vector2.right);if(stick.Axis<.999f)throw new Exception("No full forward motion");
            stick.SetDeflection(Vector2.left);if(stick.Axis>-.999f)throw new Exception("No reverse motion");
            stick.SetDeflection(new Vector2(.5f,0));if(stick.Axis<=0 || stick.Axis>=.6f)throw new Exception("Missing analog motion");
            stick.SetDeflection(Vector2.up);if(stick.Axis!=0)throw new Exception("Vertical drift");
            stick.gameObject.SetActive(false);if(stick.Axis!=0 || stick.knob.anchoredPosition!=Vector2.zero)throw new Exception("Joystick stuck after pause");stick.gameObject.SetActive(true);
            var first=new PointerEventData(EventSystem.current){pointerId=41,position=RectTransformUtility.WorldToScreenPoint(null,stick.transform.position)};
            stick.OnPointerDown(first);stick.SetDeflection(Vector2.right);
            stick.OnPointerUp(new PointerEventData(EventSystem.current){pointerId=42});if(stick.Axis<.9f)throw new Exception("Second finger released joystick");
            stick.OnPointerUp(first);if(stick.Axis!=0)throw new Exception("Owner release stuck");
        }
    }
}
#endif
