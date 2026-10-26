using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace TreasureRunner
{
    // Build decorative shapes once, then batch each section by material. No gameplay colliders.
    public class ComicWorldArt:MonoBehaviour
    {
        int world,quality;System.Random rng;Transform group;
        readonly List<Material> materials=new List<Material>();readonly List<Mesh> meshes=new List<Mesh>();
        Material earth,leaf,leafLight,wood,cream,pink,stone,accent,far,near;
        Mesh cone,rock,cube,sphere;
        public void Initialize(int w,int seed)
        {
            world=w;quality=ProgressStore.Data.graphicsQuality;rng=new System.Random(seed+9841);
            earth=Mat("Earth",ComicPalette.Earth[w]);stone=Mat("Stone",Color.Lerp(ComicPalette.Earth[w],Color.white,.18f));
            leaf=Mat("Foliage",w==5 ? ComicPalette.Hex(0x3B9EAC) : ComicPalette.Hex(0x36B879));
            leafLight=Mat("Sunlit foliage",w==5 ? ComicPalette.Hex(0x91D09F) : ComicPalette.Hex(0xABE45B));
            wood=Mat("Wood",w==5 ? ComicPalette.Hex(0x62618F) : ComicPalette.Hex(0xAD714C));
            cream=Mat("Cream",ComicPalette.Hex(0xFFF3D1));pink=Mat("Petals",ComicPalette.Hex(0xFF83B8));accent=Mat("Accent",ComicPalette.Accent[w]);
            far=Mat("Far skyline",Color.Lerp(ComicPalette.Sky[w],ComicPalette.Earth[w],.32f));near=Mat("Near skyline",Color.Lerp(ComicPalette.Sky[w],ComicPalette.Earth[w],.53f));
            cone=Loft(new[]{new Vector2(0,1),new Vector2(1,0)},9);rock=Loft(new[]{new Vector2(-.5f,.65f),new Vector2(-.2f,1),new Vector2(.35f,.72f),new Vector2(.65f,.12f)},7);
            cube=PrimitiveMesh(PrimitiveType.Cube);sphere=PrimitiveMesh(PrimitiveType.Sphere);
        }
        Material Mat(string name,Color c){var m=ComicPalette.Material("Comic "+name,c);materials.Add(m);return m;}
        Mesh PrimitiveMesh(PrimitiveType kind)
        {var go=GameObject.CreatePrimitive(kind);var mesh=go.GetComponent<MeshFilter>().sharedMesh;go.SetActive(false);Destroy(go);return mesh;}
        public void Section(RouteSection s,int index)
        {
            group=new GameObject("Comic scenery "+index+" · "+WorldRoute.Title(s.feature)).transform;group.SetParent(transform);
            float x=s.Center,y=s.height,w=s.width;
            // Layered, faceted silhouettes with a distant horizon instead of stretched spheres.
            Shape("Distant ridge",rock,new Vector3(x,-3,30),new Vector3(w*1.8f,10+index%4*2,5),far);
            if(index%2==0)Shape("Near ridge",rock,new Vector3(x+3,-3,19),new Vector3(w*1.4f,6+index%3*2,4),near);
            if(index%4==0 && world!=4 && quality>0)Cloud(new Vector3(x+1,y+10,24),world==3 ? pink : cream);
            switch(world)
            {
                case 0:
                    Tree(new Vector3(x-w*.25f,y,4.6f),4.7f+index%3*.9f,false);
                    if(index%2==0)Tree(new Vector3(x+w*.35f,y,7.5f),6,false);
                    if(s.feature==RouteFeature.Canopy){Tree(new Vector3(x,y-5,1.3f),5.8f,false);Shape("Canopy branch",cube,new Vector3(x,y-.35f,0),new Vector3(w,.5f,1.4f),wood);}
                    if(s.feature==RouteFeature.RootBridge)Boardwalk(s);
                    if(s.feature==RouteFeature.Clearing && index%2==0){Shape("Fallen tree trunk",cube,new Vector3(x+w*.22f,y+.5f,4.3f),new Vector3(4,.9f,1.2f),wood,Quaternion.Euler(0,0,-8));Shape("Flowering shrub",rock,new Vector3(x+w*.22f,y+.9f,4.3f),new Vector3(1.5f,.7f,1),pink);}
                    break;
                case 1:
                    Crystal(new Vector3(x-w*.25f,y,4.8f),2.8f,stone);
                    if(s.feature==RouteFeature.Glacier)Crystal(new Vector3(x,y,6),4.6f,cream);
                    if(s.feature==RouteFeature.FrozenLake){Shape("Frozen lake inset",cube,new Vector3(x,y+.025f,.2f),new Vector3(w*.8f,.04f,2.1f),accent);for(int j=0;j<3;j++)Shape("Lake crack",cube,new Vector3(x+(j-1)*2,y+.05f,0),new Vector3(.04f,.02f,1.5f),stone,Quaternion.Euler(0,j*36,0));}
                    for(int j=0;j<3;j++)Shape("Hanging icicle",cone,new Vector3(s.x+w*(j+1)/4,y-1,-1.4f),new Vector3(.3f,1.4f+j*.2f,.35f),cream,Quaternion.Euler(0,0,180));
                    break;
                case 2:
                    if(s.feature==RouteFeature.Oasis){Palm(new Vector3(x-2,y,4.5f));Shape("Oasis pool",sphere,new Vector3(x+2,y+.03f,4.5f),new Vector3(5,.12f,2.8f),accent);}
                    if(s.feature==RouteFeature.Ruins){Arch(new Vector3(x,y,4.8f),3.4f,earth);Columns(s,earth);}
                    if(s.feature==RouteFeature.Canyon){Shape("Sandstone hoodoo",rock,new Vector3(x,y-2,5),new Vector3(2.8f,8,2.5f),earth);Shape("Hoodoo cap",rock,new Vector3(x,y+3,5),new Vector3(4,1.4f,3),cream);}
                    Shape("Sandstone foundation",rock,new Vector3(x,y-2,.6f),new Vector3(w*.5f,2.5f,1.8f),earth);
                    break;
                case 3:
                    Shape("Lava channel",cube,new Vector3(x,-3.3f,5),new Vector3(w+3,.2f,8),accent);
                    for(int j=0;j<3;j++)Shape("Basalt pillars",rock,new Vector3(s.x+w*(j+1)/4,y-3,4.7f),new Vector3(.9f,6+j,1),stone);
                    if(s.feature==RouteFeature.Forge){Arch(new Vector3(x,y,5),3,stone);Shape("Furnace glow",cube,new Vector3(x,y+1.3f,5.1f),new Vector3(2.8f,2.6f,.2f),accent);}
                    if(s.feature==RouteFeature.LavaCrossing)Boardwalk(s);
                    break;
                case 4:
                    Shape("Cavern backdrop",cube,new Vector3(x,y+2,13),new Vector3(w+3,15,1),far);
                    for(int j=0;j<3;j++)Shape("Stalactite",cone,new Vector3(s.x+w*(j+1)/4,y+7,5),new Vector3(.8f,2+j*.3f,.8f),stone,Quaternion.Euler(0,0,180));
                    Crystal(new Vector3(x-w*.3f,y,4),s.feature==RouteFeature.CrystalHall ? 3.4f : 1.8f,accent);
                    if(s.feature==RouteFeature.Mine){Boardwalk(s);Arch(new Vector3(x,y,4.4f),3,wood);}
                    if(s.feature==RouteFeature.Tunnel)Arch(new Vector3(x,y,4),3.2f,stone);
                    break;
                case 5:
                    Tree(new Vector3(x-2,y,5),7.5f,true);
                    Mushroom(new Vector3(x+2,y,4),s.feature==RouteFeature.MushroomGrove ? 3.8f : 1.8f);
                    if(s.feature==RouteFeature.HollowTree)Arch(new Vector3(x,y,5),3.4f,wood);
                    if(s.feature==RouteFeature.ThornPass)Boardwalk(s);
                    break;
                case 6:
                    Castle(s,index);
                    break;
                case 7:
                    Shape("Floating island",rock,new Vector3(x,y-1.6f,.1f),new Vector3(w*.45f,2,1.6f),stone,Quaternion.Euler(0,0,180));
                    Crystal(new Vector3(x-w*.25f,y,4),s.feature==RouteFeature.CrystalGarden ? 4 : 2.2f,accent);
                    if(s.feature==RouteFeature.Spiral)Arch(new Vector3(x,y,5),3.3f,accent);
                    if(s.feature==RouteFeature.Drift){Cloud(new Vector3(x,y-1.5f,3),pink);Shape("Floating rune",cone,new Vector3(x+2,y+4,5),new Vector3(.6f,.9f,.6f),cream);}
                    break;
            }
            // A contrasting rim and small front details give the walkable edge a clear silhouette.
            for(int j=0;j<Mathf.Min(5,Mathf.CeilToInt(w/2));j++)
            {
                float px=s.x+.6f+j*(w-1.2f)/Mathf.Max(1,Mathf.Min(5,Mathf.CeilToInt(w/2))-1);
                if(world==6)Shape("Masonry seams",cube,new Vector3(px,y-.5f,-1.51f),new Vector3(.045f,.75f,.025f),cream);
                else if(world==0 || world==5)Grass(new Vector3(px,y,-1.1f));
                else if(quality>0)Shape("Rock facets",rock,new Vector3(px,y-.7f,-1.35f),new Vector3(.4f,.35f,.1f),index%2==0 ? stone : cream);
                if(quality>0 && j%2==0 && (world==0 || world==5))Flower(new Vector3(px,y,-1.28f));
            }
            Batch(group);
        }
        void Tree(Vector3 p,float h,bool dark)
        {
            Shape("Tapered trunk",cone,p,new Vector3(.7f,h,.65f),wood);
            Shape("Branch",cube,p+new Vector3(.7f,h*.48f,0),new Vector3(1.8f,.3f,.35f),wood,Quaternion.Euler(0,0,28));
            for(int j=0;j<3;j++)Shape("Faceted crown",rock,p+new Vector3((j-1)*1.1f,h*.65f+(j==1 ? .9f : 0),0),new Vector3(1.8f,2.5f,1.7f),j==1 ? leafLight : leaf);
        }
        void Palm(Vector3 p)
        {
            Shape("Palm trunk",cone,p,new Vector3(.4f,4,.4f),wood,Quaternion.Euler(0,0,-8));
            for(int j=0;j<5;j++)Shape("Palm frond",cone,p+Vector3.up*3.6f,new Vector3(.5f,2.7f,.3f),leafLight,Quaternion.Euler(j*72,0,65));
        }
        void Mushroom(Vector3 p,float h)
        {
            Shape("Mushroom stem",cone,p,new Vector3(.4f,h,.4f),cream);
            Shape("Mushroom cap",rock,p+Vector3.up*h*.85f,new Vector3(h*.8f,h*.55f,h*.7f),pink);
            if(quality>0)for(int j=0;j<3;j++)Shape("Mushroom dots",sphere,p+new Vector3((j-1)*h*.3f,h*1.1f,-h*.25f),Vector3.one*.22f,cream);
        }
        void Crystal(Vector3 p,float h,Material m)
        {for(int j=0;j<3;j++)Shape("Cut crystal",rock,p+Vector3.right*(j-1)*.55f,new Vector3(.45f,h*(j==1 ? 1 : .7f),.55f),m,Quaternion.Euler(0,j*30,(j-1)*18));}
        void Arch(Vector3 p,float h,Material m)
        {
            if(world==7)
            {
                for(int j=0;j<4;j++){float a=(j*90+45)*Mathf.Deg2Rad;Shape("Floating diamond rune",cube,p+new Vector3(Mathf.Cos(a)*h*.6f,h+Mathf.Sin(a)*h*.6f,0),new Vector3(h*.95f,.16f,.3f),m,Quaternion.Euler(0,0,j*90-45));}
                return;
            }
            if(world==4 || world==5)
            {
                for(int j=0;j<2;j++)Shape(world==4 ? "Cavern pillar" : "Hollow trunk root",rock,p+new Vector3((j==0 ? -1 : 1)*h*.65f,h*.25f,0),new Vector3(.75f,h*1.3f,.85f),m);
                Shape(world==4 ? "Cavern ledge" : "Gnarled branch",rock,p+Vector3.up*h,new Vector3(h,world==4 ? 1.1f : .55f,1),m);return;
            }
            Shape("Arch left",cube,p+new Vector3(-h*.6f,h*.48f,0),new Vector3(.55f,h,.8f),m,Quaternion.Euler(0,0,-5));
            Shape("Arch right",cube,p+new Vector3(h*.6f,h*.48f,0),new Vector3(.55f,h,.8f),m,Quaternion.Euler(0,0,5));
            Shape("Arch lintel",rock,p+Vector3.up*h,new Vector3(h*.9f,.6f,.65f),m);
        }
        void Columns(RouteSection s,Material m)
        {for(int j=0;j<2;j++){var p=new Vector3(s.x+1+j*(s.width-2),s.height,4);Shape("Ruined column",cone,p,new Vector3(.5f,2.5f,.6f),m);Shape("Column capital",cube,p+Vector3.up*2.5f,new Vector3(1.3f,.3f,1.2f),cream);}}
        void Castle(RouteSection s,int index)
        {
            float x=s.Center,y=s.height;
            Shape("Castle curtain wall",cube,new Vector3(x,y+1.6f,6),new Vector3(s.width+1,3.2f,1),stone);
            for(int j=0;j<4;j++)Shape("Battlement",cube,new Vector3(s.x+(j+.5f)*s.width/4,y+3.5f,6),new Vector3(1,.65f,1.1f),cream);
            Arch(new Vector3(x,y,5.35f),2.8f,earth);
            if(s.feature==RouteFeature.Tower || index%3==0)
            {
                Shape("Tower",cube,new Vector3(x-2,y+2,8),new Vector3(2.8f,6,2.8f),stone);
                Shape("Colorful tower roof",cone,new Vector3(x-2,y+5,8),new Vector3(2.3f,2.3f,2.3f),accent);
                Shape("Tower window",cube,new Vector3(x-2,y+3,6.55f),new Vector3(.7f,1.2f,.08f),cream);
            }
            Shape("Banner",cube,new Vector3(x+s.width*.3f,y+2.6f,5.35f),new Vector3(.7f,1.5f,.08f),accent);
        }
        void Boardwalk(RouteSection s)
        {
            for(int j=0;j<Mathf.CeilToInt(s.width/.7f);j++)Shape("Walkway plank",cube,new Vector3(s.x+.35f+j*.7f,s.height+.035f,0),new Vector3(.62f,.07f,2.7f),j%3==0 ? cream : wood);
            for(int j=0;j<3;j++)Shape("Walkway post",cube,new Vector3(s.x+.3f+j*(s.width-.6f)/2,s.height+.45f,1.15f),new Vector3(.14f,.9f,.14f),wood);
        }
        void Grass(Vector3 p)
        {for(int j=0;j<2;j++)Shape("Grass tuft",cone,p+Vector3.right*j*.18f,new Vector3(.13f,.32f+j*.12f,.11f),leafLight,Quaternion.Euler(0,0,(j==0 ? -15 : 18)));}
        void Flower(Vector3 p)
        {Shape("Flower stalk",cube,p+Vector3.up*.18f,new Vector3(.05f,.36f,.05f),leaf);Shape("Flower petals",sphere,p+Vector3.up*.38f,new Vector3(.28f,.15f,.22f),pink);Shape("Flower center",sphere,p+new Vector3(0,.43f,-.04f),Vector3.one*.09f,cream);}
        void Cloud(Vector3 p,Material m)
        {for(int j=0;j<3;j++)Shape("Puffy cloud",sphere,p+new Vector3((j-1)*1.8f,j==1 ? .5f : 0,0),new Vector3(3,1.5f,1),m);}
        GameObject Shape(string name,Mesh mesh,Vector3 p,Vector3 scale,Material m,Quaternion? rotation=null)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(group);go.transform.position=p;go.transform.localScale=scale;go.transform.rotation=rotation??Quaternion.identity;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;return go;
        }
        void Batch(Transform root)
        {
            var byMaterial=new Dictionary<Material,List<CombineInstance>>();var originals=root.GetComponentsInChildren<MeshFilter>();
            foreach(var f in originals)
            {
                var r=f.GetComponent<MeshRenderer>();if(!byMaterial.TryGetValue(r.sharedMaterial,out var list)){list=new List<CombineInstance>();byMaterial[r.sharedMaterial]=list;}
                list.Add(new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix});r.enabled=false;
            }
            foreach(var pair in byMaterial)
            {
                var go=new GameObject(pair.Key.name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
                var mesh=new Mesh{name="Batched comic scenery",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());meshes.Add(mesh);go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=pair.Key;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
            }
            foreach(var f in originals)Destroy(f.gameObject);
        }
        Mesh Loft(Vector2[] rings,int sides)
        {
            var v=new List<Vector3>();var t=new List<int>();
            // Independent face vertices produce deliberate crisp, low-poly normals.
            for(int r=0;r<rings.Length-1;r++)for(int s=0;s<sides;s++)
            {
                int start=v.Count;for(int j=0;j<4;j++){int ri=r+(j>1 ? 1 : 0);int si=s+((j==1 || j==2) ? 1 : 0);float a=si*Mathf.PI*2/sides;v.Add(new Vector3(Mathf.Cos(a)*rings[ri].y,rings[ri].x,Mathf.Sin(a)*rings[ri].y));}
                t.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
            }
            var mesh=new Mesh{name="Comic faceted form"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        void OnDestroy(){foreach(var m in materials)if(m!=null)Destroy(m);foreach(var m in meshes)if(m!=null)Destroy(m);}
    }
}
