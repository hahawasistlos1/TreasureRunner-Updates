using UnityEngine;
namespace TreasureRunner
{
    public static class WorldEnemyVisual
    {
        public static string Name(int world,bool heavy)=>new[]{heavy ? "Waldwächter" : "Waldräuber",heavy ? "Eisgolem" : "Frostling",heavy ? "Dünenwächter" : "Sandskorpion",heavy ? "Lavakoloss" : "Glutling",heavy ? "Kristallgolem" : "Höhlenspinne",heavy ? "Dornenwächter" : "Schattenpilz",heavy ? "Burgritter" : "Burgwache",heavy ? "Arkanwächter" : "Magiewisp"}[world];
        public static Transform Build(Transform parent,int world,float height,bool heavy)
        {
            var root=new GameObject("Visual "+Name(world,heavy)).transform;root.SetParent(parent,false);
            var color=world==0 ? new Color(.28f,.53f,.13f) : world==1 ? new Color(.3f,.8f,1) : world==2 ? new Color(.7f,.35f,.12f) : world==3 ? new Color(.65f,.1f,.04f) : world==4 ? new Color(.3f,.55f,.65f) : world==5 ? new Color(.35f,.1f,.5f) : world==6 ? new Color(.45f,.5f,.6f) : new Color(.65f,.25f,1);
            var material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));material.color=color;
            var owner=root.gameObject.AddComponent<OwnedEnemyMaterial>();owner.material=material;
            void Part(string name,PrimitiveType type,Vector3 p,Vector3 size,Color? tint=null)
            {
                var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=p*height;go.transform.localScale=size*height;
                go.GetComponent<Collider>().enabled=false;Object.Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
                if(tint.HasValue){var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",tint.Value);go.GetComponent<Renderer>().SetPropertyBlock(block);}
            }
            Part("Body",world==6 ? PrimitiveType.Cube : world==7 ? PrimitiveType.Sphere : PrimitiveType.Capsule,Vector3.zero,new Vector3(.65f,.4f,.5f));
            Part("Head",PrimitiveType.Sphere,new Vector3(0,.32f,0),Vector3.one*.42f);
            for(int eye=0;eye<2;eye++)
            {
                Part("Eye white",PrimitiveType.Sphere,new Vector3((eye==0 ? -1 : 1)*.11f,.36f,-.19f),new Vector3(.13f,.14f,.06f),Color.white);
                Part("Pupil",PrimitiveType.Sphere,new Vector3((eye==0 ? -1 : 1)*.11f,.35f,-.225f),new Vector3(.055f,.07f,.025f),new Color(.025f,.04f,.08f));
            }
            if(world==0)
            {
                Part("Acorn shell",PrimitiveType.Sphere,new Vector3(0,.07f,.12f),new Vector3(.75f,.7f,.62f),new Color(.65f,.32f,.12f));
                Part("Leaf helmet",PrimitiveType.Cube,new Vector3(0,.58f,0),new Vector3(.28f,.16f,.22f),new Color(.6f,.85f,.15f));
            }
            if(heavy)Part("Armored belly",PrimitiveType.Cube,new Vector3(0,-.02f,-.28f),new Vector3(.47f,.48f,.10f),new Color(.9f,.65f,.23f));
            if(world==2 || world==4)
                for(int leg=0;leg<6;leg++) Part("Claw leg",PrimitiveType.Cube,new Vector3((leg%2==0 ? -1 : 1)*.4f,-.22f,(leg/2-1)*.22f),new Vector3(.5f,.1f,.1f));
            else if(world!=7)
                for(int leg=0;leg<2;leg++) Part("Foot",PrimitiveType.Cube,new Vector3((leg==0 ? -1 : 1)*.22f,-.38f,0),new Vector3(.22f,.25f,.32f));
            if(world==1 || world==3 || world==4 || world==7) for(int i=0;i<3;i++) Part("Crystal horn",PrimitiveType.Cube,new Vector3((i-1)*.22f,.5f,0),new Vector3(.1f,.32f,.13f));
            if(world==5) Part("Mushroom crown",PrimitiveType.Sphere,new Vector3(0,.45f,0),new Vector3(1,.25f,.65f));
            if(world==6) Part("Shield",PrimitiveType.Cube,new Vector3(0,0,-.35f),new Vector3(.55f,.65f,.12f));
            if(world==2) Part("Scorpion tail",PrimitiveType.Capsule,new Vector3(0,.38f,.35f),new Vector3(.18f,.4f,.18f));
            return root;
        }
    }
}
