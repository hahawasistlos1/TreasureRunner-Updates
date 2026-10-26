using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class WorldTerrainSurface:MonoBehaviour
    {
        private int world;private Mesh mesh;private Material side;
        public void Initialize(int value)
        {
            world=value;side=ComicPalette.Material("Biome platform sides",ComicPalette.Earth[world]);if(world==6)return;
            float[][] edges={new[]{-.4f,-.6f,-.9f,-.65f,-.4f},new[]{-.35f,-1.5f,-.5f,-1.9f,-.4f},new[]{-.3f,-.6f,-.9f,-.55f,-.3f},new[]{-.7f,-1.5f,-2f,-1.3f,-.9f},new[]{-.5f,-1f,-.7f,-1.4f,-.6f},new[]{-.4f,-.85f,-.5f,-.9f,-.4f},new[]{-.5f,-.5f,-.5f,-.5f,-.5f},new[]{-.2f,-.8f,-1.8f,-.8f,-.2f}};
            var vertices=new List<Vector3>{new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f)};
            var walls=new List<int>();
            for(int i=0;i<4;i++)
            {
                float l=-.5f+i*.25f,r=l+.25f,yl=edges[world][i],yr=edges[world][i+1];
                // Separate strips triangulate the concave outline without overlapping faces.
                Quad(vertices,walls,new Vector3(l,.5f,-.5f),new Vector3(r,.5f,-.5f),new Vector3(r,yr,-.5f),new Vector3(l,yl,-.5f));
                Quad(vertices,walls,new Vector3(r,.5f,.5f),new Vector3(l,.5f,.5f),new Vector3(l,yl,.5f),new Vector3(r,yr,.5f));
                Quad(vertices,walls,new Vector3(l,yl,-.5f),new Vector3(r,yr,-.5f),new Vector3(r,yr,.5f),new Vector3(l,yl,.5f));
            }
            Quad(vertices,walls,new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,edges[world][0],-.5f),new Vector3(-.5f,edges[world][0],.5f));
            Quad(vertices,walls,new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,edges[world][4],.5f),new Vector3(.5f,edges[world][4],-.5f));
            mesh=new Mesh{name="Biome terrain silhouette "+world};mesh.SetVertices(vertices);mesh.subMeshCount=2;mesh.SetTriangles(new[]{0,3,2,0,2,1},0);mesh.SetTriangles(walls,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        }
        private static void Quad(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
        {int n=v.Count;v.AddRange(new[]{a,b,c,d});t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
        public void Apply(GameObject platform)
        {
            if(AssetBlockSurface.Attach(platform,AssetBlockSurface.Default(world),side))return;
            if(mesh==null)return;
            var renderer=platform.GetComponent<Renderer>();var top=renderer.sharedMaterial;
            platform.GetComponent<MeshFilter>().sharedMesh=mesh;renderer.sharedMaterials=new[]{top,side};
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(side!=null)Destroy(side);}
    }
}
