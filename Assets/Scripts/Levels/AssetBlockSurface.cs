using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class OwnedBlockMesh:MonoBehaviour
    {
        public Mesh mesh,collision;
        public void Replace(Mesh value){if(mesh!=null)Destroy(mesh);mesh=value;}
        public void ReplaceCollision(Mesh value){if(collision!=null)Destroy(collision);collision=value;}
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(collision!=null)Destroy(collision);}
    }
    public static class AssetBlockSurface
    {
        class Shape {public Mesh mesh;public Material[] materials;}
        static readonly Dictionary<GameObject,Shape> normalized=new Dictionary<GameObject,Shape>();
        static readonly string[] defaults={"Platformer_block-grass","Platformer_block-snow","floor_dirt_small_A","floor_tile_small_broken_A","floor_tile_small","floor_wood_small_dark","floor_foundation_allsides","floor_tile_small_decorated"};
        public static Bounds GeometryBounds(Renderer renderer)
        {
            var filter=renderer.GetComponent<MeshFilter>();var skin=renderer as SkinnedMeshRenderer;
            Bounds local;
            if(skin!=null){var baked=new Mesh();skin.BakeMesh(baked,true);local=baked.bounds;if(Application.isPlaying)Object.Destroy(baked);else Object.DestroyImmediate(baked);}
            else if(filter!=null && filter.sharedMesh!=null)
            {
                var mesh=filter.sharedMesh;local=mesh.bounds;
                // Some imported static meshes (notably the Adventurers bow) have a
                // zero-sized cached bound despite containing visible vertices.
                // Repair it before fitting; dividing by the empty bound makes a giant weapon.
                if(mesh.isReadable && mesh.vertexCount>0 && (local.size.sqrMagnitude<.00000001f || !Finite(local)))
                {mesh.RecalculateBounds();local=mesh.bounds;}
            }
            else local=new Bounds(Vector3.zero,Vector3.one);
            var bounds=new Bounds(renderer.transform.TransformPoint(local.center),Vector3.zero);
            for(int n=0;n<8;n++)bounds.Encapsulate(renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((n&1)==0 ? -1 : 1,(n&2)==0 ? -1 : 1,(n&4)==0 ? -1 : 1))));
            return bounds;
        }
        static bool Finite(Bounds b)=>Finite(b.center.x) && Finite(b.center.y) && Finite(b.center.z) && Finite(b.size.x) && Finite(b.size.y) && Finite(b.size.z);
        static bool Finite(float n)=>!float.IsNaN(n) && !float.IsInfinity(n);
        public static GameObject Default(int world)=>LibraryWorldArt.Floor(world)??OpenWorldArt.Asset(defaults[world]);
        public static Transform Fit(GameObject source,Transform parent,Vector3 center,Vector3 size)
        {
            if(source==null)return null;var data=Normalized(source);var go=new GameObject("Asset · "+source.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=center;go.transform.localScale=size;
            go.GetComponent<MeshFilter>().sharedMesh=data.mesh;go.GetComponent<MeshRenderer>().sharedMaterials=data.materials;return go.transform;
        }
        static Shape Normalized(GameObject source)
        {
            if(normalized.TryGetValue(source,out var data))return data;
            var copy=Object.Instantiate(source);copy.SetActive(false);copy.transform.position=Vector3.zero;
            var renderers=copy.GetComponentsInChildren<MeshRenderer>(true);Bounds bounds=new Bounds();bool found=false;
            foreach(var r in renderers){if(!found){bounds=GeometryBounds(r);found=true;}else bounds.Encapsulate(GeometryBounds(r));}
            var mats=new List<Material>();var meshes=new List<Mesh>();
            var normalization=Matrix4x4.Scale(new Vector3(1/Mathf.Max(.001f,bounds.size.x),1/Mathf.Max(.001f,bounds.size.y),1/Mathf.Max(.001f,bounds.size.z)))*Matrix4x4.Translate(-bounds.center);
            foreach(var r in renderers)
            {
                var filter=r.GetComponent<MeshFilter>();if(filter==null || filter.sharedMesh==null)continue;
                for(int n=0;n<filter.sharedMesh.subMeshCount;n++)
                {
                    var mesh=new Mesh();mesh.CombineMeshes(new[]{new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=n,transform=normalization*filter.transform.localToWorldMatrix}},true,true);meshes.Add(mesh);mats.Add(r.sharedMaterials[Mathf.Min(n,r.sharedMaterials.Length-1)]);
                }
            }
            var result=new Mesh{name="Normalized asset "+source.name};var instances=new List<CombineInstance>();foreach(var mesh in meshes)instances.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.identity});result.CombineMeshes(instances.ToArray(),false,true);
            foreach(var mesh in meshes)Object.Destroy(mesh);Object.Destroy(copy);data=new Shape{mesh=result,materials=mats.ToArray()};normalized[source]=data;return data;
        }
        public static bool Attach(GameObject platform,GameObject source,Material side,bool matchCollision=false)
        {
            if(source==null)return false;var data=Normalized(source);Vector3 scale=platform.transform.localScale;float width=Mathf.Abs(scale.x),depth=Mathf.Abs(scale.y),cap=Mathf.Min(.65f,depth);
            var all=new List<CombineInstance>();var materials=new List<Material>();
            for(int sub=0;sub<data.mesh.subMeshCount;sub++)
            {
                var tiles=new List<CombineInstance>();for(float x=0;x<width-.001f;x+=1)
                {
                    float slice=Mathf.Min(1,width-x);var matrix=Matrix4x4.TRS(new Vector3((x+slice*.5f)/width-.5f,.5f-cap/(depth*2),0),Quaternion.identity,new Vector3(slice/width,cap/depth,1));
                    tiles.Add(new CombineInstance{mesh=data.mesh,subMeshIndex=sub,transform=matrix});
                }
                var combined=new Mesh();combined.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;combined.CombineMeshes(tiles.ToArray(),true,true);all.Add(new CombineInstance{mesh=combined,transform=Matrix4x4.identity});materials.Add(data.materials[sub]);
            }
            if(depth>cap+.01f)
            {
                var baseMesh=Box(.5f-cap/depth);all.Add(new CombineInstance{mesh=baseMesh,transform=Matrix4x4.identity});materials.Add(side??platform.GetComponent<Renderer>().sharedMaterial);
            }
            var output=new Mesh{name="Asset building blocks · "+source.name};output.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;output.CombineMeshes(all.ToArray(),false,true);
            foreach(var piece in all)Object.Destroy(piece.mesh);
            platform.GetComponent<MeshFilter>().sharedMesh=output;platform.GetComponent<Renderer>().sharedMaterials=materials.ToArray();
            var owner=platform.GetComponent<OwnedBlockMesh>()??platform.AddComponent<OwnedBlockMesh>();owner.Replace(output);
            if(matchCollision)
            {
                var solid=source.GetComponent<LibraryTileCollision>();Mesh collision=output;
                if(solid!=null && solid.shape!=null){collision=Object.Instantiate(solid.shape);owner.ReplaceCollision(collision);}
                foreach(var collider in platform.GetComponents<Collider>()){collider.enabled=false;Object.Destroy(collider);}
                var precise=platform.AddComponent<MeshCollider>();precise.sharedMesh=collision;
            }
            return true;
        }
        static Mesh Box(float top)
        {
            var v=new List<Vector3>();var t=new List<int>();
            Q(new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,top,-.5f),new Vector3(-.5f,top,-.5f));
            Q(new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,top,.5f),new Vector3(.5f,top,.5f));
            Q(new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,top,-.5f),new Vector3(-.5f,top,.5f));
            Q(new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,top,.5f),new Vector3(.5f,top,-.5f));
            Q(new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,-.5f));
            var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();return mesh;
            void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d){int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        }
    }
}
