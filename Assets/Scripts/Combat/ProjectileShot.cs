using UnityEngine;
namespace TreasureRunner
{
    public class ProjectileShot : MonoBehaviour
    {
        public ProjectilePool owner;
        public Vector3 Direction { get; private set; }
        public string Weapon { get; private set; }
        private int damage;private float remaining;private Enemy target;private Vector3 targetOffset;
        private Transform arrow,orb;private Renderer orbRenderer;private TrailRenderer trail;
        private readonly RaycastHit[] hits=new RaycastHit[32];
        private readonly Collider[] overlaps=new Collider[24];
        public void BuildVisual(Material material)
        {
            arrow=new GameObject("Visible arrow").transform;arrow.SetParent(transform,false);
            Part(arrow,PrimitiveType.Cube,new Vector3(-.14f,0,0),new Vector3(.75f,.055f,.055f),material,new Color(.55f,.3f,.12f));
            Part(arrow,PrimitiveType.Cube,new Vector3(.27f,0,0),new Vector3(.16f,.13f,.08f),material,new Color(.9f,.92f,.95f)).localRotation=Quaternion.Euler(0,0,45);
            Part(arrow,PrimitiveType.Cube,new Vector3(-.43f,0,0),new Vector3(.16f,.2f,.035f),material,Color.white);
            orb=Part(transform,PrimitiveType.Sphere,Vector3.zero,Vector3.one*.3f,material,Color.white);orb.name="Element fireball";orbRenderer=orb.GetComponent<Renderer>();
            trail=gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=material;trail.time=.18f;trail.startWidth=.16f;trail.endWidth=0;trail.minVertexDistance=.1f;
        }
        private Transform Part(Transform parent,PrimitiveType type,Vector3 p,Vector3 scale,Material material,Color color)
        {
            var go=GameObject.CreatePrimitive(type);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color);go.GetComponent<Renderer>().SetPropertyBlock(block);return go.transform;
        }
        public void Fire(Vector3 direction,int power,Color color,string weapon,Enemy aim,Vector3 offset)
        {
            Direction=direction.normalized;damage=power;remaining=2;target=aim;targetOffset=offset;Weapon=weapon;
            arrow.gameObject.SetActive(ArsenalProgression.IsArrow(weapon));orb.gameObject.SetActive(!ArsenalProgression.IsArrow(weapon));arrow.localScale=weapon=="crossbow" ? new Vector3(.7f,.85f,.85f) : Vector3.one;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",color*1.4f);orbRenderer.SetPropertyBlock(block);
            trail.Clear();trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0);trail.emitting=!ArsenalProgression.IsArrow(weapon);Orient();
        }
        private void Orient()=>transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(Direction.y,Direction.x)*Mathf.Rad2Deg);
        private bool Hit(Collider c)
        {
            if(c.GetComponentInParent<PlayerMovement>()!=null) return false;
            var enemy=c.GetComponentInParent<Enemy>();
            if(c.isTrigger && enemy==null) return false;
            if(enemy!=null && !enemy.IsDefeated) EnemyWeaponGuard.Apply(enemy,damage,Weapon);
            if(ArsenalProgression.IsArrow(Weapon))GameEffects.Impact(transform.position,GameUI.Gold);else GameEffects.Impact(transform.position,trail.startColor,"circle_05");owner.Release(this);return true;
        }
        private void FixedUpdate()
        {
            if(target!=null && !target.IsDefeated)
            {
                Vector3 aim=target.GetComponent<BoxCollider>().bounds.center+targetOffset-transform.position;
                if(Vector3.Dot(aim,Direction)>0) Direction=Vector3.RotateTowards(Direction,aim.normalized,2.2f*Time.fixedDeltaTime,0).normalized;
            }
            Orient();float step=(Weapon=="crossbow" ? 16 : 12)*Time.fixedDeltaTime;
            int n=Physics.OverlapSphereNonAlloc(transform.position,.1f,overlaps,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<n;i++) if(Hit(overlaps[i])) return;
            int count=Physics.SphereCastNonAlloc(transform.position,.09f,Direction,hits,step,~0,QueryTriggerInteraction.Collide);
            Collider nearest=null;float closest=float.MaxValue;
            for(int i=0;i<count;i++)
            {
                var c=hits[i].collider;if(c.GetComponentInParent<PlayerMovement>()!=null || (c.isTrigger && c.GetComponentInParent<Enemy>()==null)) continue;
                if(hits[i].distance<closest){closest=hits[i].distance;nearest=c;}
            }
            if(nearest!=null && Hit(nearest)) return;
            transform.position+=Direction*step;remaining-=Time.fixedDeltaTime;if(remaining<=0) owner.Release(this);
        }
    }
}
