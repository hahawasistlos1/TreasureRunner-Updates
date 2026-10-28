using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public static class EnemyArrowArt
    {
        public static Transform Build(Transform parent)
        {
            var entry=AssetLibrary.Data.entries.FirstOrDefault(e=>e.pack=="kay-adventurers-v2" && e.title.StartsWith("arrow bow ·"));var source=AssetLibrary.Model(entry);if(source==null)return null;
            var arrow=Object.Instantiate(source);arrow.name="Asset-Pfeil";arrow.transform.position=Vector3.zero;arrow.transform.rotation=Quaternion.identity;
            var renderers=arrow.GetComponentsInChildren<Renderer>();var bounds=Bounds(renderers);
            Vector3 axis=bounds.size.x>bounds.size.y && bounds.size.x>bounds.size.z ? Vector3.right : bounds.size.z>bounds.size.y ? Vector3.forward : Vector3.up;
            arrow.transform.rotation=Quaternion.FromToRotation(axis,Vector3.up);bounds=Bounds(renderers);arrow.transform.localScale*=.65f/Mathf.Max(.01f,bounds.size.y);bounds=Bounds(renderers);
            arrow.transform.position-=new Vector3(bounds.center.x,bounds.max.y,bounds.center.z);arrow.transform.SetParent(parent,false);return arrow.transform;
        }
        static Bounds Bounds(Renderer[] renderers){var b=AssetBlockSurface.GeometryBounds(renderers[0]);foreach(var r in renderers.Skip(1))b.Encapsulate(AssetBlockSurface.GeometryBounds(r));return b;}
    }
}
