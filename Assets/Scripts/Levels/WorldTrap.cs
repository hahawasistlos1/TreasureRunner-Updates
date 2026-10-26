using UnityEngine;
namespace TreasureRunner
{
    public enum TrapKind { FallingRock, Cannon, Flame, CrumblingFloor }
    public class WorldTrap : MonoBehaviour
    {
        public TrapKind kind;public float floorY;public int facing=-1;
        public bool IsWarning { get; private set; }
        public bool IsActive { get; private set; }
        public int Activations { get; private set; }
        private float next,phaseEnd,fallSpeed,nextFx;private Vector3 rest;private Collider floorCollider;
        private Vector3 Muzzle=>new Vector3(rest.x+facing*1.17f,floorY+.68f,0);
        private Transform effect,warning;private Material material;private Renderer[] renderers;private readonly Collider[] occupants=new Collider[32];
        private void Start()
        {
            rest=transform.position;renderers=GetComponentsInChildren<Renderer>();floorCollider=GetComponent<Collider>();
            material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));material.color=new Color(1,.3f,.035f);
            var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="Trap warning area";warning=marker.transform;
            warning.position=new Vector3(rest.x,floorY+.04f,0);warning.rotation=Quaternion.identity;warning.localScale=new Vector3(kind==TrapKind.Flame ? 3.2f : 1.8f,.06f,1.6f);Decorate(marker);marker.SetActive(false);
            var fx=GameObject.CreatePrimitive(kind==TrapKind.Flame ? PrimitiveType.Capsule : PrimitiveType.Sphere);fx.name=kind+" effect";effect=fx.transform;Decorate(fx);fx.SetActive(false);next=Time.time+1.5f;
            var source=OpenWorldArt.Asset(kind==TrapKind.Cannon ? "Platformer_bomb" : "rock_largeA");
            if(source!=null && (kind==TrapKind.Cannon || kind==TrapKind.FallingRock)){fx.GetComponent<Renderer>().enabled=false;AssetBlockSurface.Fit(source,fx.transform,Vector3.zero,Vector3.one);}
        }
        private void Decorate(GameObject go){go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;}
        public void Trigger()
        {
            if(IsWarning || IsActive || warning==null) return;IsWarning=true;phaseEnd=Time.time+(kind==TrapKind.CrumblingFloor ? .85f : 1.1f);warning.gameObject.SetActive(true);
            if(kind==TrapKind.CrumblingFloor && floorCollider!=null){var b=floorCollider.bounds;warning.position=new Vector3(b.center.x,b.max.y+.04f,b.center.z);warning.localScale=new Vector3(b.size.x,.05f,b.size.z);}
            if(kind==TrapKind.Cannon || kind==TrapKind.Flame)warning.position=new Vector3(Muzzle.x+facing*1.2f,floorY+.04f,0);
        }
        private void Update()
        {
            var run=LevelRun.Instance;if(run==null || !run.IsPlaying || run.Player==null) return;
            var p=run.Player.transform.position;float dx=Mathf.Abs(p.x-rest.x);
            if(!IsWarning && !IsActive && Time.time>=next)
            {
                Bounds pb=run.Player.GetComponent<Collider>().bounds;
                bool near=kind==TrapKind.CrumblingFloor ? floorCollider!=null && pb.max.x>floorCollider.bounds.min.x && pb.min.x<floorCollider.bounds.max.x && Mathf.Abs(pb.min.y-floorCollider.bounds.max.y)<.4f : dx<(kind==TrapKind.Cannon ? 10 : 5);
                if(near) Trigger();
            }
            if(IsWarning && Time.time>=phaseEnd)
            {
                IsWarning=false;IsActive=true;Activations++;warning.gameObject.SetActive(false);effect.gameObject.SetActive(kind!=TrapKind.CrumblingFloor);
                phaseEnd=Time.time+(kind==TrapKind.Flame ? 1.5f : kind==TrapKind.CrumblingFloor ? 3.5f : 2.5f);
                fallSpeed=0;effect.position=kind==TrapKind.FallingRock ? new Vector3(rest.x,Mathf.Max(floorY+.6f,rest.y-.6f),0) : Muzzle;
                if(kind==TrapKind.CrumblingFloor)
                {
                    var bounds=floorCollider.bounds;
                    int count=Physics.OverlapBoxNonAlloc(bounds.center+Vector3.up, new Vector3(bounds.extents.x,1.5f,bounds.extents.z),occupants,Quaternion.identity,~0,QueryTriggerInteraction.Collide);
                    for(int i=0;i<count;i++){var e=occupants[i].GetComponentInParent<Enemy>();if(e!=null && !e.IsDefeated && e.transform.position.x>bounds.min.x && e.transform.position.x<bounds.max.x)e.TakeDamage(e.CurrentHealth);}
                    floorCollider.enabled=false;foreach(var v in renderers)v.enabled=false;
                }
                if(kind==TrapKind.Cannon){effect.localScale=Vector3.one*.55f;GameEffects.Impact(Muzzle,new Color(.7f,.7f,.75f),"smoke_04");}
            }
            if(!IsActive) return;
            if(kind==TrapKind.FallingRock || kind==TrapKind.Cannon)
            {
                fallSpeed+=JumpProfile.Gravity*Time.deltaTime;
                Vector3 delta=(kind==TrapKind.Cannon ? Vector3.right*facing*7 : Vector3.down*fallSpeed)*Time.deltaTime;
                float radius=kind==TrapKind.Cannon ? .275f : .4f;effect.localScale=Vector3.one*radius*2;
                if(delta.sqrMagnitude>0 && Physics.SphereCast(effect.position,radius,delta.normalized,out var hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                {if(hit.collider.GetComponentInParent<PlayerHealth>()==run.Player)run.Player.TakeDamage(1);phaseEnd=Time.time;}
                effect.position+=delta;
            }
            if(kind==TrapKind.Flame)
            {
                Vector3 direction=Vector3.right*facing;float length=2.8f;
                if(Physics.Raycast(Muzzle,direction,out var wall,length,~0,QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<PlayerHealth>()==null)length=Mathf.Max(.05f,wall.distance);
                effect.position=Muzzle+direction*length*.5f;effect.localScale=new Vector3(.45f,length*.5f,.45f);effect.rotation=Quaternion.Euler(0,0,90);
                if(Time.time>=nextFx){nextFx=Time.time+.18f;GameEffects.Impact(effect.position,new Color(1,.45f,.08f),"fire_01");}
                int count=Physics.OverlapCapsuleNonAlloc(Muzzle+direction*Mathf.Min(.225f,length*.5f),Muzzle+direction*Mathf.Max(length-.225f,length*.5f),.225f,occupants,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(occupants[i].GetComponentInParent<PlayerHealth>()==run.Player)run.Player.TakeDamage(1);
            }
            if(Time.time>=phaseEnd || (kind==TrapKind.FallingRock && effect.position.y<floorY-.5f))
            {
                IsActive=false;effect.gameObject.SetActive(false);next=Time.time+3.5f;
                if(kind==TrapKind.CrumblingFloor){floorCollider.enabled=true;foreach(var v in renderers)v.enabled=true;}
            }
        }
        private void OnDestroy(){if(warning!=null)Destroy(warning.gameObject);if(effect!=null)Destroy(effect.gameObject);if(material!=null)Destroy(material);}
    }
}
