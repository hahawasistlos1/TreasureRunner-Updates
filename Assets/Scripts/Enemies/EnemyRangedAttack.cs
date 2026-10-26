using UnityEngine;
namespace TreasureRunner
{
    [RequireComponent(typeof(Enemy))]
    public class EnemyRangedAttack : MonoBehaviour
    {
        [Min(1)] public float range=8;
        [Min(.25f)] public float windup=.9f;
        [Min(1)] public float cooldown=2.8f;
        public int damage=1;
        public bool IsPreparing { get; private set; }
        public int ShotsFired { get; private set; }
        private Enemy enemy;
        private EnemyPatrol patrol;
        private Transform marker;
        private Material material;
        private Vector3 aim;
        private float fireAt,nextAttack;
        private void Start()
        {
            enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();
            var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);orb.name="Shot warning";orb.transform.SetParent(transform,false);
            orb.transform.localPosition=Vector3.up*.85f;orb.GetComponent<Collider>().enabled=false;Destroy(orb.GetComponent<Collider>());
            material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));material.color=new Color(1,.28f,.04f);
            orb.GetComponent<Renderer>().sharedMaterial=material;marker=orb.transform;orb.SetActive(false);
            var bowRoot=new GameObject("Archer bow");bowRoot.transform.SetParent(transform,false);bowRoot.transform.localPosition=new Vector3(0,.15f,-.2f);
            if(GetComponent<SkeletonMotion>()==null)LevelFactory.AddVisual(GameConfig.Current.bowVisual,bowRoot.transform,.6f,-.2f);
            nextAttack=Time.time+1;
        }
        private void Update()
        {
            var run=LevelRun.Instance;
            if(enemy.IsDefeated || run==null || !run.IsPlaying) { if(enemy.IsDefeated) Cancel();return; }
            var player=run.Player;
            if(player==null || player.health<=0) { Cancel();return; }
            Vector3 muzzle=transform.position+Vector3.up*.2f;
            Vector3 point=player.transform.position-Vector3.up*.35f;
            float distance=Vector3.Distance(muzzle,point);
            if(distance>range || Mathf.Abs(point.z-muzzle.z)>1) { Cancel();return; }
            if(IsPreparing)
            {
                marker.localScale=Vector3.one*Mathf.Lerp(.12f,.4f,1-(fireAt-Time.time)/windup);
                if(Time.time>=fireAt)
                {
                    EnemyBolt.Fire(muzzle,aim,damage);ShotsFired++;
                    GetComponent<CharacterPresentation>()?.AttackPose();
                    GetComponent<SkeletonMotion>()?.Attack();
                    Cancel();nextAttack=Time.time+cooldown;
                }
                return;
            }
            if(Time.time<nextAttack || distance<1.4f) return;
            if(Physics.Raycast(muzzle,(point-muzzle).normalized,out var hit,distance,~0,QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PlayerMovement>()==null) return;
            aim=(point-muzzle).normalized;fireAt=Time.time+windup;IsPreparing=true;patrol.Suspended=true;
            if(patrol.visual!=null) patrol.visual.localRotation=Quaternion.Euler(0,aim.x>0 ? 90 : -90,0);
            marker.gameObject.SetActive(true);
        }
        private void Cancel() { IsPreparing=false;if(patrol!=null) patrol.Suspended=false;if(marker!=null) marker.gameObject.SetActive(false); }
        private void OnDisable() => Cancel();
        private void OnDestroy() { if(material!=null) Destroy(material); }
    }
}
