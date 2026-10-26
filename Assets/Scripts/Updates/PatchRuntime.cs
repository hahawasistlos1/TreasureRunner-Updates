using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public static class PatchRuntime
    {
        static PatchStore store;public static event Action Changed;
        public static int Revision=>store?.Revision??0;public static int BlockedThrough=>store?.BlockedThrough??0;
        public static PatchManifest Manifest=>store?.Manifest;
        public static void Initialize()
        {
            if(store!=null)return;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if(ProgressStore.IsMemoryTest)return;
#endif
            store=new PatchStore(Path.Combine(Application.persistentDataPath,"data-patches-v1"),Application.version);store.Load();
        }
        public static bool Install(PatchPackage package,out string reason)
        {Initialize();if(LevelRun.Instance!=null){reason="Spielpatch wird erst im Hauptmenü aktiviert.";return false;}if(store==null){reason="Patchspeicher ist nicht verfügbar.";return false;}if(!store.Install(package,out reason))return false;Changed?.Invoke();return true;}
        public static int WeaponDamage(WeaponDefinition w,int baseline)=>LevelRun.IsCustom ? baseline : store?.Active?.weapons?.FirstOrDefault(p=>p.id==w.id)?.damage??baseline;
        public static float WeaponCooldown(WeaponDefinition w,float baseline)=>LevelRun.IsCustom ? baseline : store?.Active?.weapons?.FirstOrDefault(p=>p.id==w.id)?.cooldown??baseline;
        public static List<RouteSection> Route(List<RouteSection> route,int world,int stage)
        {var patch=store?.Active?.levels?.FirstOrDefault(l=>l.id==CampaignCatalog.Id(world,stage));if(patch!=null)PatchCodec.ApplySections(route,patch);return route;}
        public static void ApplyEnemy(GameObject go,int world,string kind)
        {
            var patch=store?.Active?.enemies?.FirstOrDefault(e=>e.world==world && e.kind==kind);if(patch==null)return;
            var enemy=go.GetComponent<Enemy>();enemy.maxHealth=patch.health;enemy.speed=patch.speed;go.GetComponent<EnemyAwareness>().detectionRange=patch.detection;
            var box=go.GetComponent<BoxCollider>();float oldHeight=box.size.y;box.size=new Vector3(box.size.x*patch.hitboxX,oldHeight*patch.hitboxY,box.size.z);box.center+=Vector3.up*(box.size.y-oldHeight)*.5f;
        }
#if UNITY_EDITOR
        public static void SetTestStore(PatchStore value){store=value;}
#endif
    }
}
