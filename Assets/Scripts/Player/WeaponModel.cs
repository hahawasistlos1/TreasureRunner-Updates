using UnityEngine;
namespace TreasureRunner
{
    // Original models with explicit blade markers: rendering and damage share these endpoints.
    public class WeaponModel:MonoBehaviour
    {
        public Transform bladeBase,bladeTip;
        static Material material;
        public static WeaponModel Build(Transform hand,string id)
        {
            if(material==null)material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit")){color=Color.white};
            var root=new GameObject("Equipped "+id).transform;root.SetParent(hand,false);
            root.localScale=new Vector3(1/hand.lossyScale.x,1/hand.lossyScale.y,1/hand.lossyScale.z);
            var model=root.gameObject.AddComponent<WeaponModel>();
            Color wood=new Color(.42f,.20f,.09f),steel=new Color(.72f,.93f,1),gold=new Color(1,.68f,.15f);
            Part(root,PrimitiveType.Cube,new Vector3(0,.06f,0),new Vector3(.10f,.26f,.10f),wood);
            if(id=="spear")
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,.55f,0),new Vector3(.07f,1.15f,.07f),wood);
                Part(root,PrimitiveType.Cube,new Vector3(0,1.16f,0),new Vector3(.14f,.32f,.07f),steel);
                Part(root,PrimitiveType.Sphere,new Vector3(0,.95f,0),Vector3.one*.13f,new Color(.3f,1,.5f));
                model.bladeBase=Marker(root,new Vector3(0,1f,0));model.bladeTip=Marker(root,new Vector3(0,1.32f,0));
            }
            else if(id=="hammer")
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,.38f,0),new Vector3(.09f,.7f,.09f),wood);
                Part(root,PrimitiveType.Cube,new Vector3(0,.78f,0),new Vector3(.48f,.23f,.22f),new Color(.9f,.87f,.7f));
                Part(root,PrimitiveType.Cube,new Vector3(0,.78f,-.12f),new Vector3(.10f,.18f,.04f),gold);
                model.bladeBase=Marker(root,new Vector3(-.24f,.78f,0));model.bladeTip=Marker(root,new Vector3(.24f,.78f,0));
            }
            else if(id=="sword" || id=="axe")
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,.22f,0),new Vector3(.32f,.08f,.13f),gold);
                Part(root,PrimitiveType.Cube,new Vector3(0,.60f,0),new Vector3(id=="axe" ? .07f : .12f,.70f,.07f),id=="axe" ? wood : steel);
                if(id=="axe")Part(root,PrimitiveType.Cube,new Vector3(.06f,.79f,0),new Vector3(.40f,.28f,.09f),steel);
                model.bladeBase=Marker(root,new Vector3(0,id=="axe" ? .65f : .25f,0));model.bladeTip=Marker(root,new Vector3(0,.95f,0));
            }
            else if(id=="staff" || id=="froststaff")
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,.40f,0),new Vector3(.075f,.9f,.075f),wood);
                Part(root,id=="froststaff" ? PrimitiveType.Cube : PrimitiveType.Sphere,new Vector3(0,.92f,0),Vector3.one*.23f,new Color(.25f,.85f,1)).localRotation=Quaternion.Euler(0,0,id=="froststaff" ? 45 : 0);
                for(int i=0;i<3;i++)Part(root,PrimitiveType.Cube,new Vector3((i-1)*.14f,.90f,0),new Vector3(.045f,.34f,.07f),gold);
                if(id=="froststaff")for(int i=-1;i<=1;i+=2)Part(root,PrimitiveType.Cube,new Vector3(i*.24f,.85f,0),new Vector3(.10f,.35f,.07f),steel).localRotation=Quaternion.Euler(0,0,-i*35);
            }
            else if(id=="crossbow")
            {
                Part(root,PrimitiveType.Cube,new Vector3(0,.36f,0),new Vector3(.12f,.68f,.14f),wood);
                Part(root,PrimitiveType.Cube,new Vector3(0,.54f,0),new Vector3(.68f,.09f,.10f),steel);
                Part(root,PrimitiveType.Cube,new Vector3(0,.33f,-.085f),new Vector3(.018f,.75f,.018f),gold);
                for(int i=-1;i<=1;i+=2)Part(root,PrimitiveType.Cube,new Vector3(i*.19f,.40f,0),new Vector3(.38f,.025f,.025f),Color.white).localRotation=Quaternion.Euler(0,0,-i*45);
            }
            else
            {
                for(int i=0;i<7;i++)
                {
                    float a=(i-3)*.34f;var part=Part(root,PrimitiveType.Cube,new Vector3(Mathf.Cos(a)*.24f-.18f,Mathf.Sin(a)*.55f+.23f,0),new Vector3(.07f,.20f,.08f),wood);
                    part.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);
                }
                Part(root,PrimitiveType.Cube,new Vector3(-.06f,.23f,0),new Vector3(.018f,.97f,.018f),new Color(.9f,.95f,1));
            }
            return model;
        }
        static Transform Marker(Transform p,Vector3 position){var t=new GameObject("Blade contact marker").transform;t.SetParent(p,false);t.localPosition=position;return t;}
        static Transform Part(Transform parent,PrimitiveType type,Vector3 p,Vector3 scale,Color color)
        {
            var o=GameObject.CreatePrimitive(type);o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=scale;
            o.GetComponent<Collider>().enabled=false;Destroy(o.GetComponent<Collider>());o.GetComponent<Renderer>().sharedMaterial=material;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);o.GetComponent<Renderer>().SetPropertyBlock(block);return o.transform;
        }
        public void Aim(Vector3 hand,Vector3 direction){transform.position=hand;transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);}
    }
}
