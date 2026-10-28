using System;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public class LibraryWeaponSource:MonoBehaviour {public string assetId;}
    public static class LibraryWeaponFit
    {
        public static bool Build(WeaponModel target,string weapon)
        {
            string key=weapon=="froststaff" ? "staff" : weapon;
            var options=AssetLibrary.Data.entries.Where(e=>e.role==(int)BuilderRole.Weapon && e.weapon==key && !e.title.ToLowerInvariant().Contains("shield"))
                .OrderBy(e=>e.pack=="kay-weapons" ? 0 : e.pack=="kay-adventurers-v2" ? 1 : e.pack=="kay-adventurers" ? 2 : 3).ThenBy(e=>e.id,StringComparer.Ordinal).ToArray();
            if(options.Length==0)return false;
            var entry=options[Mathf.Min(WeaponUpgrades.Level(weapon),options.Length-1)];var source=AssetLibrary.Model(entry);if(source==null)return false;
            var view=UnityEngine.Object.Instantiate(source);view.transform.position=Vector3.zero;view.transform.rotation=Quaternion.identity;view.name="Asset-Waffe · "+entry.title;
            foreach(var a in view.GetComponentsInChildren<Animation>())a.enabled=false;
            foreach(var a in view.GetComponentsInChildren<Animator>())a.enabled=false;
            var renderers=view.GetComponentsInChildren<Renderer>();if(renderers.Length==0){UnityEngine.Object.Destroy(view);return false;}
            Bounds bounds=BoundsOf(renderers);
            if(!Usable(bounds)){UnityEngine.Object.Destroy(view);return false;}
            Vector3 axis=bounds.size.x>bounds.size.y && bounds.size.x>bounds.size.z ? Vector3.right : bounds.size.z>bounds.size.y ? Vector3.forward : Vector3.up;
            view.transform.rotation=Quaternion.FromToRotation(axis,Vector3.up);bounds=BoundsOf(renderers);
            float length=weapon=="spear" ? 1.32f : weapon=="staff" || weapon=="froststaff" ? 1.12f : weapon=="hammer" ? .92f : .95f;
            view.transform.localScale*=length/Mathf.Max(.01f,bounds.size.y);bounds=BoundsOf(renderers);
            view.transform.position+=new Vector3(-bounds.center.x,weapon=="bow" ? -bounds.center.y : -bounds.min.y-.12f,-bounds.center.z);bounds=BoundsOf(renderers);
            view.transform.SetParent(target.transform,false);view.AddComponent<LibraryWeaponSource>().assetId=entry.id;
            if(weapon=="sword" || weapon=="spear")
            {target.bladeBase=Marker(target.transform,new Vector3(0,bounds.min.y+length*.28f,0));target.bladeTip=Marker(target.transform,new Vector3(0,bounds.max.y,0));}
            if(weapon=="axe" || weapon=="hammer")
            {float y=bounds.max.y-length*.15f;target.bladeBase=Marker(target.transform,new Vector3(bounds.min.x,y,0));target.bladeTip=Marker(target.transform,new Vector3(bounds.max.x,y,0));}
            if(weapon=="froststaff")foreach(var r in renderers){var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.4f,.9f,1.3f));r.SetPropertyBlock(tint);}
            return true;
        }
        static Transform Marker(Transform parent,Vector3 point){var t=new GameObject("Blade contact marker").transform;t.SetParent(parent,false);t.localPosition=point;return t;}
        static bool Usable(Bounds b)=>b.size.sqrMagnitude>.00000001f && !float.IsNaN(b.size.sqrMagnitude) && !float.IsInfinity(b.size.sqrMagnitude);
        static Bounds BoundsOf(Renderer[] renderers){var b=AssetBlockSurface.GeometryBounds(renderers[0]);foreach(var r in renderers.Skip(1))b.Encapsulate(AssetBlockSurface.GeometryBounds(r));return b;}
    }
}
