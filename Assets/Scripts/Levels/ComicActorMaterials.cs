using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class ComicActorMaterials:MonoBehaviour
    {
        readonly Dictionary<Material,Material> copies=new Dictionary<Material,Material>();
        public void Apply(Transform root)
        {
            foreach(var r in root.GetComponentsInChildren<Renderer>())
            {
                if(!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))continue;
                var slots=r.sharedMaterials;
                for(int i=0;i<slots.Length;i++)
                {
                    var original=slots[i];if(original==null || original.shader==ComicPalette.Shader)continue;
                    if(!copies.TryGetValue(original,out var copy))
                    {
                        Color color=original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") : original.color;
                        Texture texture=original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.mainTexture;
                        copy=ComicPalette.Material("Comic actor · "+original.name,color);
                        if(texture!=null){copy.SetTexture("_BaseMap",texture);copy.SetTextureScale("_BaseMap",original.mainTextureScale);copy.SetTextureOffset("_BaseMap",original.mainTextureOffset);}
                        copies[original]=copy;
                    }
                    slots[i]=copy;
                }
                r.sharedMaterials=slots;
            }
        }
        void OnDestroy(){foreach(var m in copies.Values)if(m!=null)Destroy(m);}
    }
}
