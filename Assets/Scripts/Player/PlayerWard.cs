using UnityEngine;
namespace TreasureRunner
{
    public class PlayerWard:MonoBehaviour
    {
        public const float Duration=18;
        float expires;int charges;PlayerHealth health;LineRenderer ring;Material material;
        public float Remaining=>Mathf.Max(0,expires-Time.time);
        public bool Active=>charges>0 && Remaining>0 && health!=null && health.health>0;
        void Awake()
        {
            health=GetComponent<PlayerHealth>();var root=new GameObject("Schutzschild-Kontur");root.transform.SetParent(transform,false);
            ring=root.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=48;ring.startWidth=ring.endWidth=.035f;
            var template=Resources.Load<Material>("ShieldRing");material=template!=null ? new Material(template) : new Material(ComicPalette.Shader);material.SetColor("_BaseColor",new Color(.3f,.95f,1));ring.sharedMaterial=material;
            for(int i=0;i<48;i++){float a=i*Mathf.PI*2/48;ring.SetPosition(i,new Vector3(Mathf.Cos(a)*.66f,Mathf.Sin(a)*1.08f,-.45f));}ring.enabled=false;
        }
        public bool Activate()
        {
            if(Active || health==null || health.health<=0 || LevelRun.Instance==null || !LevelRun.Instance.IsPlaying)return false;
            charges=1;expires=Time.time+Duration;ring.enabled=true;LevelRun.Instance.HUD?.Toast("Schutzschild aktiv: fängt einen Treffer ab (18 s)");return true;
        }
        public bool Absorb()
        {
            if(!Active)return false;charges=0;ring.enabled=false;GameEffects.Impact(transform.position,new Color(.3f,.95f,1));LevelRun.Instance?.HUD?.Toast("Schutzschild hat den Treffer abgefangen");return true;
        }
        void Update(){ring.enabled=Active;}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
    public class WardPickup:MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            var health=other.GetComponentInParent<PlayerHealth>();if(health==null)return;
            var ward=health.GetComponent<PlayerWard>();if(ward!=null && ward.Activate()){GameAudio.Instance?.Play("coin");Destroy(gameObject);}
        }
    }
}
