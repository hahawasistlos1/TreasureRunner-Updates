using UnityEngine;
namespace TreasureRunner
{
    public static class TrapModel
    {
        public static GameObject Build(Transform parent,TrapKind kind,Vector3 position,float floor,Material metal,Material trim)
        {
            var root=new GameObject(kind.ToString());root.transform.SetParent(parent,false);root.transform.position=position;
            if(kind==TrapKind.Cannon || kind==TrapKind.Flame)
            {
                var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.4f,0);box.size=new Vector3(1.2f,.8f,.95f);
                var baseModel=OpenWorldArt.Asset("Platformer_crate");
                if(baseModel!=null)AssetBlockSurface.Fit(baseModel,root.transform,box.center,box.size);
                else Part(root.transform,PrimitiveType.Cube,box.center,box.size,metal,false);
                var barrel=OpenWorldArt.Asset("Platformer_pipe");
                if(barrel!=null){var tube=AssetBlockSurface.Fit(barrel,root.transform,new Vector3(-.4f,.68f,0),new Vector3(.4f,.95f,.5f));tube.localRotation=Quaternion.Euler(0,0,90);}
                else Part(root.transform,PrimitiveType.Cube,new Vector3(-.4f,.68f,0),new Vector3(.95f,.4f,.5f),metal,false);
                var barrelBox=root.AddComponent<BoxCollider>();barrelBox.center=new Vector3(-.4f,.68f,0);barrelBox.size=new Vector3(.95f,.4f,.5f);
                Part(root.transform,PrimitiveType.Cube,new Vector3(-.88f,.68f,0),new Vector3(.06f,.25f,.32f),trim,false);
            }
            else if(OpenWorldArt.Asset("rock_largeA")!=null)AssetBlockSurface.Fit(OpenWorldArt.Asset("rock_largeA"),root.transform,Vector3.zero,new Vector3(.9f,.4f,.7f));
            else Part(root.transform,PrimitiveType.Cube,Vector3.zero,new Vector3(.9f,.3f,.7f),metal,false);
            var trap=root.AddComponent<WorldTrap>();trap.kind=kind;trap.floorY=floor;return root;
        }
        static Transform Part(Transform parent,PrimitiveType type,Vector3 p,Vector3 scale,Material material,bool solid)
        {
            var go=GameObject.CreatePrimitive(type);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;if(!solid){go.GetComponent<Collider>().enabled=false;Object.Destroy(go.GetComponent<Collider>());}return go.transform;
        }
    }
}
