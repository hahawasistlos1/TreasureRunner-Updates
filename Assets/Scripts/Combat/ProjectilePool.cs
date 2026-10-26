using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool instance;
        private readonly Queue<ProjectileShot> inactive=new Queue<ProjectileShot>();
        private Material material;private int created;
        public static void Fire(Vector3 p,int facing,int damage,Color color)=>Fire(p,Vector3.right*facing,damage,color,"staff",null);
        public static void Fire(Vector3 p,Vector3 direction,int damage,Color color,string weapon,Enemy target,Vector3 offset=default)
        {
            if(instance==null) instance=new GameObject("ProjectilePool").AddComponent<ProjectilePool>();
            instance.Launch(p,direction,damage,color,weapon,target,offset);
        }
        private void Launch(Vector3 p,Vector3 direction,int damage,Color color,string weapon,Enemy target,Vector3 offset=default)
        {
            ProjectileShot shot;
            if(inactive.Count>0) shot=inactive.Dequeue();
            else
            {
                if(created>=64) return;created++;
                var go=new GameObject("Player projectile");go.transform.SetParent(transform);
                if(material==null) material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                shot=go.AddComponent<ProjectileShot>();shot.owner=this;shot.BuildVisual(material);
            }
            shot.transform.position=p;shot.gameObject.SetActive(true);shot.Fire(direction,damage,color,weapon,target,offset);
        }
        public void Release(ProjectileShot shot){shot.gameObject.SetActive(false);inactive.Enqueue(shot);}
        private void OnDestroy(){if(instance==this) instance=null;if(material!=null) Destroy(material);}
    }
}
