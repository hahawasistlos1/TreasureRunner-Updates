using UnityEngine;
using UnityEngine.Rendering;
namespace TreasureRunner
{
    public enum EnemyAwarenessState { Patrolling, Pursuing, Returning, Watching }
    [RequireComponent(typeof(Enemy),typeof(EnemyPatrol))]
    public class EnemyAwareness : MonoBehaviour
    {
        [Min(1)] public float detectionRange=6;
        [Min(.1f)] public float memorySeconds=1;
        [Min(0)] public float chaseDistance=2.8f;
        [Min(1)] public float pursuitSpeed=1.2f;
        [Min(0)] public float reactionSeconds=.35f;
        public bool keepDistance;
        public EnemyAwarenessState State { get; private set; }
        public bool HasTarget => State==EnemyAwarenessState.Pursuing || State==EnemyAwarenessState.Watching;
        public int TargetFacing { get; private set; }=1;
        public float MinX => origin.x-Mathf.Max(chaseDistance,patrol.leftDistance);
        public float MaxX => origin.x+Mathf.Max(chaseDistance,patrol.rightDistance);
        public float SpeedMultiplier => State==EnemyAwarenessState.Pursuing ? pursuitSpeed : 1;
        private Enemy enemy;
        private EnemyPatrol patrol;
        private EnemyRangedAttack ranged;
        private Transform alert;
        private Vector3 origin,targetPoint;
        private float nextScan,lastSeen=-100,alertUntil,reactAt;
        private void Awake()
        {
            enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();ranged=GetComponent<EnemyRangedAttack>();origin=transform.position;
        }
        private void Start()
        {
            var root=new GameObject("Spotted player !");root.transform.SetParent(transform,false);
            root.transform.localPosition=Vector3.up*(GetComponent<BoxCollider>().size.y*.5f+.65f);
            alert=root.transform;
            Part(PrimitiveType.Cube,Vector3.up*.12f,new Vector3(.09f,.3f,.07f));
            Part(PrimitiveType.Sphere,Vector3.down*.14f,Vector3.one*.11f);
            root.SetActive(false);
        }
        private void Part(PrimitiveType type,Vector3 position,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(type);go.transform.SetParent(alert,false);go.transform.localPosition=position;go.transform.localScale=size;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
            var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=GameConfig.Current.coinMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(1,.22f,.06f));renderer.SetPropertyBlock(block);
        }
        private void Update()
        {
            var run=LevelRun.Instance;
            if(enemy.IsDefeated) { State=EnemyAwarenessState.Patrolling;if(alert!=null) alert.gameObject.SetActive(false);return; }
            if(run==null || !run.IsPlaying || run.Player==null || run.Player.health<=0) return;
            if(Time.time>=nextScan)
            {
                nextScan=Time.time+.12f;
                Vector3 point=run.Player.transform.position;
                Vector3 from=GetComponent<BoxCollider>().bounds.center;
                Vector3 delta=point-from;
                bool visible=delta.sqrMagnitude<=detectionRange*detectionRange && Mathf.Abs(delta.y)<=(ranged!=null ? 7f : 2.2f) && Mathf.Abs(delta.z)<1;
                if(visible && Physics.Raycast(from,delta.normalized,out var hit,delta.magnitude,patrol.environmentLayers,QueryTriggerInteraction.Ignore))
                    visible=hit.collider.GetComponentInParent<PlayerMovement>()!=null;
                if(visible)
                {
                    if(!HasTarget) { alertUntil=Time.time+1;reactAt=Time.time+reactionSeconds; }
                    targetPoint=point;lastSeen=Time.time;
                    if(Mathf.Abs(point.x-transform.position.x)>.05f) TargetFacing=point.x>transform.position.x ? 1 : -1;
                    State=ranged!=null ? EnemyAwarenessState.Watching : EnemyAwarenessState.Pursuing;
                }
                else if(HasTarget && Time.time-lastSeen>memorySeconds) State=EnemyAwarenessState.Returning;
                if(State==EnemyAwarenessState.Returning && Mathf.Abs(transform.position.x-origin.x)<.12f) State=EnemyAwarenessState.Patrolling;
            }
            if(alert!=null) alert.gameObject.SetActive(HasTarget && Time.time<alertUntil);
        }
        public int MoveDirection(int patrolDirection)
        {
            if(State==EnemyAwarenessState.Watching)
            {
                if(!keepDistance || Time.time<reactAt || Mathf.Abs(targetPoint.y-transform.position.y)>1.5f)return 0;
                return Mathf.Abs(targetPoint.x-transform.position.x)<2.8f ? -TargetFacing : 0;
            }
            if(State==EnemyAwarenessState.Pursuing)
            {
                if(Time.time<reactAt) return 0;
                float dx=targetPoint.x-transform.position.x;
                // Contact attacks still use Enemy's trigger and normal damage immunity.
                return Mathf.Abs(dx)<.65f ? 0 : dx>0 ? 1 : -1;
            }
            if(State==EnemyAwarenessState.Returning)
            {
                float dx=origin.x-transform.position.x;
                return Mathf.Abs(dx)<.1f ? 0 : dx>0 ? 1 : -1;
            }
            return patrolDirection;
        }
    }
}
