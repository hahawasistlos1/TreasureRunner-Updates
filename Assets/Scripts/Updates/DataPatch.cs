using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
namespace TreasureRunner
{
    [Serializable] public class PatchManifest
    {public int schema=1,revision,payloadBytes;public string appVersion,title,changes,payloadFile,sha256;}
    [Serializable] public class DataPatch
    {public int schema=1,revision;public string appVersion;public PatchWeapon[] weapons=new PatchWeapon[0];public PatchEnemy[] enemies=new PatchEnemy[0];public PatchLevel[] levels=new PatchLevel[0];}
    [Serializable] public class PatchWeapon {public string id;public int damage;public float cooldown;}
    [Serializable] public class PatchEnemy {public int world;public string kind;public int health;public float speed,detection,hitboxX=1,hitboxY=1;}
    [Serializable] public class PatchLevel {public int id;public PatchSection[] sections;}
    [Serializable] public class PatchSection {public int index,room;public float width,height,gap;}
    [Serializable] public class PatchPackage {public PatchManifest manifest;public string payload;}
    public static class PatchCodec
    {
        public const int MaxManifestBytes=16384,MaxPayloadBytes=262144;
        public static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static bool Range(float v,float min,float max)=>!float.IsNaN(v) && !float.IsInfinity(v) && v>=min && v<=max;
        public static bool Manifest(PatchManifest m,string version,out string reason)
        {
            reason="Patch-Ankündigung ungültig.";
            if(m==null || m.schema!=1 || m.appVersion!=version || m.revision<1 || m.revision>1000000 || m.payloadBytes<2 || m.payloadBytes>MaxPayloadBytes || string.IsNullOrWhiteSpace(m.title) || m.title.Length>80 || m.changes==null || m.changes.Length>2000 || m.payloadFile==null || !Regex.IsMatch(m.payloadFile,@"\Apatch-[0-9]{1,8}\.json\z") || m.sha256==null || !Regex.IsMatch(m.sha256,@"\A[0-9a-f]{64}\z"))return false;
            reason="";return true;
        }
        public static bool Package(PatchPackage package,string version,out DataPatch patch,out string reason)
        {
            patch=null;reason="Unvollständiger Spielpatch.";if(package==null || package.payload==null)return false;if(!Manifest(package.manifest,version,out reason))return false;
            byte[] bytes=Encoding.UTF8.GetBytes(package.payload);if(bytes.Length!=package.manifest.payloadBytes || Hash(bytes)!=package.manifest.sha256){reason="Patch-Prüfsumme oder Dateigröße stimmt nicht.";return false;}
            try{patch=JsonUtility.FromJson<DataPatch>(package.payload);}catch(Exception){reason="Patch-Daten sind nicht lesbar.";return false;}
            if(patch==null || patch.revision!=package.manifest.revision){reason="Patchnummer stimmt nicht überein.";return false;}
            return Validate(patch,version,out reason);
        }
        public static bool Validate(DataPatch patch,string version,out string reason)
        {
            reason="Patch-Daten sind nicht mit dieser Spielversion kompatibel.";
            if(patch==null || patch.schema!=1 || patch.appVersion!=version || patch.revision<1 || patch.revision>1000000)return false;
            var weapons=patch.weapons??new PatchWeapon[0];var enemies=patch.enemies??new PatchEnemy[0];var levels=patch.levels??new PatchLevel[0];
            reason="Patch enthält ungültige oder doppelte Spielwerte.";
            if(weapons.Length>ArsenalProgression.WeaponCount || enemies.Length>32 || levels.Length>80 || weapons.Length+enemies.Length+levels.Length==0)return false;
            var ids=new HashSet<string>();
            foreach(var w in weapons)if(w==null || ArsenalProgression.WeaponIndex(w.id)<0 || !ids.Add(w.id) || w.damage<1 || w.damage>20 || !Range(w.cooldown,ArsenalProgression.IsArrow(w.id) || ArsenalProgression.IsMagic(w.id) ? .62f : .35f,5))return false;
            ids.Clear();foreach(var e in enemies)if(e==null || e.world<0 || e.world>7 || !new[]{"normal","monster","shooter","boss"}.Contains(e.kind) || !ids.Add(e.world+":"+e.kind) || e.health<1 || e.health>200 || !Range(e.speed,0,5) || !Range(e.detection,1,16) || !Range(e.hitboxX,.7f,1.25f) || !Range(e.hitboxY,.7f,1.25f))return false;
            var levelIds=new HashSet<int>();
            foreach(var l in levels)
            {
                if(l==null || l.id<0 || l.id>=CampaignCatalog.Count || !levelIds.Add(l.id) || l.sections==null || l.sections.Length<1 || l.sections.Length>12)return false;
                int world=CampaignCatalog.World(l.id),stage=CampaignCatalog.Stage(l.id);var route=WorldRoute.CreateBase(world,stage,WorldRoute.SectionCount(world,stage));var sections=new HashSet<int>();
                foreach(var s in l.sections)
                {
                    if(s==null || s.index<0 || s.index>=route.Count || !sections.Add(s.index) || s.room<0 || s.room>9 || !Range(s.width,3.5f,30) || !Range(s.height,-4,12) || !Range(s.gap,0,3.5f))return false;
                    if(s.index==0 && (s.height!=0 || s.width<6) || s.index==route.Count-1 && (s.width<14 || s.room!=(int)EncounterRoom.Rest || s.gap!=0))return false;
                }
                ApplySections(route,l);
                for(int i=0;i<route.Count-1;i++)if(!JumpProfile.CanCross(route[i].gap,route[i+1].height-route[i].height)){reason="Patch würde eine nicht erreichbare Sprunglücke erzeugen.";return false;}
            }
            reason="";return true;
        }
        public static void ApplySections(List<RouteSection> route,PatchLevel patch)
        {
            foreach(var s in patch.sections){var r=route[s.index];r.width=s.width;r.height=s.height;r.gap=s.gap;r.room=(EncounterRoom)s.room;route[s.index]=r;}
            float x=0;for(int i=0;i<route.Count;i++){var r=route[i];r.x=x;route[i]=r;x+=r.width+r.gap;}
        }
    }
}
