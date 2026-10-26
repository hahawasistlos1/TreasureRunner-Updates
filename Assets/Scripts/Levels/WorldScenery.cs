using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace TreasureRunner
{
    // Shared materials and meshes keep decoration inexpensive on mobile.
    public class WorldScenery : MonoBehaviour
    {
        private int world;
        private Material stone, vegetation, highlight, snow;
        private readonly List<Material> owned = new List<Material>();
        public static Color Sky(int w) => new[] { new Color(.40f,.68f,.79f),new Color(.55f,.77f,.9f),new Color(.88f,.65f,.38f),new Color(.18f,.08f,.1f),new Color(.025f,.035f,.07f),new Color(.025f,.09f,.1f),new Color(.3f,.37f,.48f),new Color(.13f,.055f,.25f) }[w];
        public void Initialize(int value)
        {
            world=value;
            stone=Make("Biome rock",world==1 ? new Color(.45f,.78f,.92f) : world==2 ? new Color(.65f,.37f,.18f) : new Color(.19f,.21f,.26f));
            vegetation=Make("Biome vegetation",world==2 ? new Color(.22f,.45f,.16f) : new Color(.09f,.23f,.17f));
            highlight=Make("Biome glow",world==3 ? new Color(1,.17f,.025f) : world==4 ? new Color(.15f,.75f,.95f) : new Color(.65f,.3f,1));
            snow=Make("Snow",new Color(.9f,.97f,1));
        }
        public void Segment(float x,float y,int index)
        {
            float z=3.8f;
            switch(world)
            {
                case 0:
                    if(index%3==0) Model("tree_pineRoundA",new Vector3(x+6,y,z+2),5,vegetation);
                    break;
                case 1:
                    Model("rock_tallA",new Vector3(x+3,y,z),3.7f,stone);
                    Shape("Snow cap",PrimitiveType.Sphere,new Vector3(x+3,y+3.1f,z),new Vector3(2.6f,.6f,2),snow);
                    Shape("Ice spire",PrimitiveType.Capsule,new Vector3(x+7,y+1.8f,z+1),new Vector3(.65f,2.4f,.7f),stone);
                    Shape("Snow ridge",PrimitiveType.Sphere,new Vector3(x+5,-1,13),new Vector3(16,7+index%3,7),snow); break;
                case 2:
                    Model(index%2==0 ? "cactus_tall" : "cactus_short",new Vector3(x+2,y,z),2.6f,vegetation);
                    Shape("Sand dune",PrimitiveType.Sphere,new Vector3(x+5,-1,12),new Vector3(17,4+index%3,8),MakeSharedSand());
                    if(index%3==0) Model("statue_columnDamaged",new Vector3(x+7,y,z+1),3.2f,stone); break;
                case 3:
                    Model("rock_tallA",new Vector3(x+2,y,z),4.5f,stone);
                    Shape("Lava river",PrimitiveType.Cube,new Vector3(x+4,-1.8f,3),new Vector3(11,.25f,8),highlight);
                    Shape("Volcanic peak",PrimitiveType.Capsule,new Vector3(x+5,0,14),new Vector3(7,7+index%3,6),stone); break;
                case 4:
                    Model("rock_tallA",new Vector3(x+2,y,z),4.2f,stone);
                    Shape("Cavern ceiling",PrimitiveType.Cube,new Vector3(x+5,7,3),new Vector3(11,1.2f,5),stone);
                    Crystal(new Vector3(x+7,y+.8f,z),highlight);
                    Shape("Cavern wall",PrimitiveType.Cube,new Vector3(x+5,2,10),new Vector3(11,10,2),stone); break;
                case 5:
                    Model("tree_default_dark",new Vector3(x+2,y,z),6.5f,vegetation);
                    Model("tree_default_dark",new Vector3(x+8,y,z+3),8,vegetation);
                    Shape("Glowing mushroom cap",PrimitiveType.Sphere,new Vector3(x+6,y+.8f,2),new Vector3(1.1f,.35f,1.1f),highlight);
                    Shape("Mushroom stem",PrimitiveType.Cylinder,new Vector3(x+6,y+.4f,2),new Vector3(.18f,.4f,.18f),snow); break;
                case 6:
                    Shape("Castle wall",PrimitiveType.Cube,new Vector3(x+5,2,5),new Vector3(11,4,1),stone);
                    for(int j=0;j<4;j++) Shape("Battlement",PrimitiveType.Cube,new Vector3(x+1+j*2.8f,4.4f,5),new Vector3(1.2f,.8f,1.2f),stone);
                    if(index%3==0) { Shape("Castle tower",PrimitiveType.Cylinder,new Vector3(x+3,3,7),new Vector3(3,3,3),stone); Model("statue_columnDamaged",new Vector3(x+7,y,2.8f),2.7f,snow); } break;
                case 7:
                    Crystal(new Vector3(x+2,y+1.8f,z),highlight);
                    Crystal(new Vector3(x+7,y+1,z+1),stone);
                    Shape("Floating island",PrimitiveType.Sphere,new Vector3(x+5,3+index%3,14),new Vector3(6,1.5f,3),stone);
                    Shape("Magic ring",PrimitiveType.Cylinder,new Vector3(x+6,5,z+2),new Vector3(1.1f,.08f,1.1f),highlight); break;
            }
        }
        private Material sand;
        private Material MakeSharedSand() => sand!=null ? sand : sand=Make("Dune sand",new Color(.82f,.57f,.28f));
        private void Crystal(Vector3 p,Material m)
        {
            var a=Shape("Crystal cluster",PrimitiveType.Cube,p,new Vector3(.7f,2.8f,.7f),m);a.transform.rotation=Quaternion.Euler(10,30,-18);
            Shape("Crystal shard",PrimitiveType.Cube,p+Vector3.right*.7f,new Vector3(.35f,1.4f,.4f),m).transform.rotation=Quaternion.Euler(5,15,25);
        }
        private void Model(string name,Vector3 p,float height,Material m)
        {
            var source=Resources.Load<GameObject>("KenneyNature/"+name);
            if(source==null) return;
            var root=new GameObject("Biome "+name);root.transform.SetParent(transform);root.transform.position=p;
            var visual=LevelFactory.AddVisual(source,root.transform,height,0);
            foreach(var r in visual.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;var slots=r.sharedMaterials;for(int i=0;i<slots.Length;i++) slots[i]=m;r.sharedMaterials=slots; }
        }
        private GameObject Shape(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material m)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(transform);go.transform.position=p;go.transform.localScale=scale;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=m;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;go.GetComponent<Renderer>().receiveShadows=false;return go;
        }
        private Material Make(string name,Color c) { var m=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));m.name=name;m.color=c;owned.Add(m);return m; }
        private void OnDestroy() { foreach(var m in owned) Destroy(m); }
    }
}
