using UnityEngine;
namespace TreasureRunner
{
    public class AssetHazard:MonoBehaviour
    {
        public bool saw;public Transform visual;
        public bool Dangerous=>saw || Mathf.Repeat(Time.time,4.4f)>=1.4f;
        readonly Collider[] hits=new Collider[12];
        void Update()
        {
            if(LevelRun.Instance==null || !LevelRun.Instance.IsPlaying)return;
            if(visual!=null)
            {
                if(saw)visual.Rotate(0,0,-220*Time.deltaTime,Space.Self);
                else visual.localPosition=new Vector3(0,Dangerous ? .2f : -.14f,0);
            }
            if(!Dangerous)return;
            int n=Physics.OverlapBoxNonAlloc(transform.position+Vector3.up*(saw ? .4f : .18f),saw ? new Vector3(.31f,.31f,.22f) : new Vector3(.4f,.16f,.45f),hits,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)hits[i].GetComponentInParent<PlayerHealth>()?.TakeDamage(1);
        }
    }
    public class BuilderWeaponPickup:MonoBehaviour
    {
        public string weapon;
        void OnTriggerEnter(Collider other)
        {
            if(!LevelRun.IsCustom || other.GetComponentInParent<PlayerHealth>()==null)return;
            LevelRun.CustomWeapon=weapon;other.GetComponentInParent<CharacterPresentation>()?.RefreshWeapon();LevelRun.Instance?.HUD?.Toast("Waffenfund: "+GameConfig.Current.EquippedWeapon.title);Destroy(gameObject);
        }
    }
}
