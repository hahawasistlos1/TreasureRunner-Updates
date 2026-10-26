using UnityEngine;
namespace TreasureRunner
{
    public class TreasureBlock:MonoBehaviour
    {
        public bool Claimed{get;private set;}
        void OnCollisionEnter(Collision c)
        {
            var player=c.collider.GetComponent<PlayerMovement>();
            if(Claimed || player==null || player.transform.position.y>transform.position.y || c.relativeVelocity.sqrMagnitude<.25f || c.GetContact(0).point.y>transform.position.y-.25f)return;
            Claimed=true;CoinManager.Instance?.AddCoins(5);ScoreManager.Instance?.AddPoints(50);GameEffects.Spark(transform.position+Vector3.up);GameAudio.Instance?.Play("coin");
            var block=new MaterialPropertyBlock();block.SetColor("_BaseColor",new Color(.5f,.36f,.2f));GetComponent<Renderer>().SetPropertyBlock(block);
            LevelRun.Instance?.HUD?.Toast("Versteckter Schatz! +5 Münzen");
        }
    }
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody),typeof(BoxCollider))]
    public class MovingRide : MonoBehaviour
    {
        public Vector3 travel=new Vector3(0,2.6f,0); public float period=4;
        Vector3 origin;Rigidbody body;float elapsed;
        public Vector3 Velocity {get;private set;}
        void Awake(){body=GetComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;GetComponent<BoxCollider>().isTrigger=false;}
        void Start(){origin=body.position;}
        void FixedUpdate()
        {
            var run=LevelRun.Instance;Velocity=Vector3.zero;if(run==null || !run.IsPlaying)return;
            elapsed+=Time.fixedDeltaTime;
            Vector3 next=origin+travel*((1-Mathf.Cos(elapsed*Mathf.PI*2/Mathf.Max(.5f,period)))*.5f);
            Velocity=(next-body.position)/Time.fixedDeltaTime;body.MovePosition(next);
        }
        void OnDisable(){Velocity=Vector3.zero;}
    }
    public class OneWayLedge:MonoBehaviour
    {
        Collider ledge,player;
        void Start(){ledge=GetComponent<Collider>();}
        void FixedUpdate()
        {
            var run=LevelRun.Instance;if(run==null || run.Player==null || ledge==null)return;
            player=run.Player.GetComponent<Collider>();
            if(GetComponent<MovingRide>()!=null){Physics.IgnoreCollision(ledge,player,false);return;}
            bool below=player.bounds.min.y<ledge.bounds.max.y-.08f || run.Player.GetComponent<Rigidbody>().linearVelocity.y>.3f;
            Physics.IgnoreCollision(ledge,player,below);
        }
        void OnDisable(){if(ledge!=null && player!=null)Physics.IgnoreCollision(ledge,player,false);}
    }
    public class PressureGate : MonoBehaviour
    {
        public GameObject gate; public bool Opened{get;private set;}
        void OnTriggerEnter(Collider c)
        {
            if(Opened || c.GetComponent<PlayerMovement>()==null)return;Opened=true;
            if(gate!=null){gate.GetComponent<Collider>().enabled=false;gate.transform.position+=Vector3.up*4;}
            LevelRun.Instance?.HUD?.Toast("Schalter aktiviert – Tor offen!");GameAudio.Instance?.Play("coin");
        }
    }
    public class SpringPad : MonoBehaviour
    {
        float next;
        void OnTriggerStay(Collider c)
        {
            var p=c.GetComponent<PlayerMovement>();if(p==null || Time.time<next || LevelRun.Instance==null || !LevelRun.Instance.IsPlaying)return;
            next=Time.time+.6f;p.Bounce(9);GameAudio.Instance?.Play("coin");
        }
    }
    public enum EnemyTactic { Hopper, Charger }
    public class EnemyTactics : MonoBehaviour
    {
        public EnemyTactic tactic; public int Actions{get;private set;}public bool WindingUp{get;private set;}
        Enemy enemy;EnemyPatrol patrol;float speed,next,phase;Vector3 rest;bool dashing;
        void Start(){enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();speed=enemy.speed;rest=transform.position;next=Time.time+1;}
        void Update()
        {
            var run=LevelRun.Instance;if(run==null || !run.IsPlaying || enemy.IsDefeated || run.Player==null)return;
            float distance=Vector3.Distance(run.Player.transform.position,transform.position);
            if(tactic==EnemyTactic.Hopper)
            {
                if(distance<7 && Time.time>=next){phase=Time.time;next=Time.time+2.1f;Actions++;}
                if(Time.time-phase<.8f && Actions>0){var p=transform.position;p.y=rest.y+Mathf.Sin((Time.time-phase)/.8f*Mathf.PI)*1.15f;transform.position=p;}
                else{var p=transform.position;p.y=rest.y;transform.position=p;}
                return;
            }
            if(WindingUp && Time.time>=phase){WindingUp=false;dashing=true;patrol.Suspended=false;enemy.speed=speed*3.8f;phase=Time.time+.65f;Actions++;}
            if(dashing && Time.time>=phase){dashing=false;enemy.speed=speed;next=Time.time+2.4f;}
            if(!WindingUp && !dashing && distance<6 && Mathf.Abs(run.Player.transform.position.y-transform.position.y)<1.5f && Time.time>=next)
            {WindingUp=true;patrol.Suspended=true;phase=Time.time+.8f;GameEffects.Spark(transform.position+Vector3.up*.7f);}
        }
    }
}
