using UnityEngine;
namespace TreasureRunner
{
    // Preview projectiles belong to one tile and never join the gameplay pool or cause damage.
    public sealed class EditorPreviewProjectile:MonoBehaviour
    {
        Vector3 velocity;float lifetime=3;
        public static void Fire(Transform owner,Vector3 position,Vector3 direction,bool arrow)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Vorschau-Geschoss";go.transform.SetParent(owner,true);go.transform.position=position;
            go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());
            var material=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));material.color=new Color(1,.35f,.05f);go.GetComponent<Renderer>().sharedMaterial=material;
            if(arrow){EnemyArrowArt.Build(go.transform);go.GetComponent<Renderer>().enabled=false;go.transform.rotation=Quaternion.FromToRotation(Vector3.up,direction);}else go.transform.localScale=Vector3.one*.22f;
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=owner.gameObject.layer;
            go.AddComponent<EditorPreviewProjectile>().velocity=direction.normalized*5;
        }
        void FixedUpdate()
        {
            Vector3 step=velocity*Time.fixedDeltaTime;
            if(Physics.SphereCast(transform.position,.1f,step.normalized,out _,step.magnitude,1<<gameObject.layer,QueryTriggerInteraction.Ignore)){Destroy(gameObject);return;}
            transform.position+=step;lifetime-=Time.fixedDeltaTime;if(lifetime<=0)Destroy(gameObject);
        }
        void OnDestroy(){var r=GetComponent<Renderer>();if(r!=null && r.sharedMaterial!=null)Destroy(r.sharedMaterial);}
    }
}
