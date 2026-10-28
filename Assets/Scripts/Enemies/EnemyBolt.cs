using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    // One bounded pool per scene; all bolts are released when changing levels.
    public class EnemyBolt : MonoBehaviour
    {
        private static readonly Queue<EnemyBolt> pool=new Queue<EnemyBolt>();
        private static GameObject root;
        private static Material shared;
        private Vector3 velocity;
        private int damage;
        private float life;
        Transform arrow;public bool IsArrow {get;private set;}
        private readonly RaycastHit[] hits=new RaycastHit[24];
        public static void Fire(Vector3 position,Vector3 direction,int power,bool arrow=false)
        {
            if(root==null)
            {
                pool.Clear();root=new GameObject("Enemy shot pool");root.AddComponent<EnemyShotPoolLifetime>();
                shared=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));shared.color=new Color(1,.32f,.06f);
            }
            EnemyBolt shot=null;
            while(pool.Count>0 && shot==null) shot=pool.Dequeue();
            if(shot==null)
            {
                if(root.transform.childCount>=48) return;
                var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Enemy projectile";go.transform.SetParent(root.transform);
                go.transform.localScale=Vector3.one*.22f;go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial=shared;shot=go.AddComponent<EnemyBolt>();
            }
            shot.transform.position=position;shot.transform.rotation=Quaternion.identity;shot.transform.localScale=Vector3.one;
            if(arrow && shot.arrow==null)shot.arrow=EnemyArrowArt.Build(shot.transform);
            shot.IsArrow=arrow && shot.arrow!=null;shot.GetComponent<Renderer>().enabled=!shot.IsArrow;
            if(shot.arrow!=null)shot.arrow.gameObject.SetActive(shot.IsArrow);
            shot.transform.localScale=Vector3.one*(shot.IsArrow ? 1 : .22f);if(shot.IsArrow)shot.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);
            shot.velocity=direction.normalized*5;shot.damage=power;shot.life=3;shot.gameObject.SetActive(true);
        }
        private void FixedUpdate()
        {
            float step=velocity.magnitude*Time.fixedDeltaTime;
            int count=Physics.SphereCastNonAlloc(transform.position,.1f,velocity.normalized,hits,step,~0,QueryTriggerInteraction.Collide);
            Collider nearest=null;float distance=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var c=hits[i].collider;
                if(c.GetComponentInParent<Enemy>()!=null) continue;
                if(c.isTrigger && c.GetComponentInParent<PlayerHealth>()==null) continue;
                if(hits[i].distance<distance) { nearest=c;distance=hits[i].distance; }
            }
            if(nearest!=null)
            {
                var health=nearest.GetComponentInParent<PlayerHealth>();
                if(health!=null && LevelRun.Instance!=null && LevelRun.Instance.IsPlaying) health.TakeDamage(damage);
                Release();return;
            }
            transform.position+=velocity*Time.fixedDeltaTime;life-=Time.fixedDeltaTime;if(life<=0) Release();
        }
        private void Release() { gameObject.SetActive(false);pool.Enqueue(this); }
        public static void ClearPool() { if(shared!=null) Destroy(shared);shared=null;root=null;pool.Clear(); }
    }
    public class EnemyShotPoolLifetime : MonoBehaviour { private void OnDestroy() => EnemyBolt.ClearPool(); }
}
