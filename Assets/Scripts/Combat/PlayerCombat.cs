using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
namespace TreasureRunner
{
    public class PlayerCombat : MonoBehaviour
    {
        private float nextAttack;
        private PlayerMovement movement;
        private readonly Collider[] hits=new Collider[64];
        private readonly RaycastHit[] shotCover=new RaycastHit[32];
        private readonly HashSet<Enemy> unique=new HashSet<Enemy>();
        public Vector3 AimDirection { get; private set; }=Vector3.right;
        public Vector3 AimPoint { get; private set; }
        public Enemy Target { get; private set; }
        private float strikeStarted=-10;
        private int strikeDamage;
        private string strikeWeapon;
        private Vector3 previousBase,previousTip;
        private bool previousSample;
        private void Awake()=>movement=GetComponent<PlayerMovement>();
        private void Update()
        {
            if(Keyboard.current==null) return;
            if(Keyboard.current.fKey.wasPressedThisFrame) Attack();
            if(Keyboard.current.qKey.wasPressedThisFrame) CycleWeapon();
        }
        public void CycleWeapon()
        {
            if(LevelRun.Instance==null || !LevelRun.Instance.IsPlaying) return;
            if(LevelRun.IsCustom)
            {
                var weapons=GameConfig.Current.weapons;int current=System.Array.FindIndex(weapons,w=>w.id==LevelRun.CustomWeapon);
                LevelRun.CustomWeapon=weapons[(current+1)%weapons.Length].id;GetComponent<CharacterPresentation>()?.RefreshWeapon();LevelRun.Instance.HUD?.Toast(GameConfig.Current.EquippedWeapon.title);return;
            }
            var list=ProgressStore.Data.weapons.FindAll(id=>ArsenalProgression.CanUse(id) && System.Array.Exists(GameConfig.Current.weapons,w=>w.id==id)); if(list.Count<2) return;
            int index=list.IndexOf(GameConfig.Current.EquippedWeapon.id);
            ShopService.Equip(list[(index+1)%list.Count]);
            GetComponent<CharacterPresentation>()?.RefreshWeapon();
            LevelRun.Instance.HUD?.Toast(GameConfig.Current.EquippedWeapon.title);
        }
        public Enemy FindTarget(Vector3 muzzle)
        {
            Enemy best=null;float score=float.MaxValue;
            int count=Physics.OverlapSphereNonAlloc(transform.position,11,hits,~0,QueryTriggerInteraction.Collide);
            for(int i=0;i<count;i++)
            {
                var e=hits[i].GetComponentInParent<Enemy>();if(e==null || e.IsDefeated) continue;
                Vector3 point=e.GetComponent<BoxCollider>().bounds.center;
                Vector3 d=point-muzzle;
                if(Mathf.Abs(d.z)>1 || Mathf.Abs(d.y)>7 || d.x*movement.Facing<-.4f) continue;
                if(!ClearShot(muzzle,point))
                {
                    point=e.GetComponent<BoxCollider>().bounds.center+Vector3.up*e.GetComponent<BoxCollider>().bounds.extents.y*.8f;d=point-muzzle;
                    if(!ClearShot(muzzle,point))continue;
                }
                float rank=d.sqrMagnitude+Mathf.Abs(d.y)*.3f;
                if(rank<score) {score=rank;best=e;AimPoint=point;}
            }
            return best;
        }
        private bool ClearShot(Vector3 origin,Vector3 point)
        {
            Vector3 direction=point-origin;
            int count=Physics.SphereCastNonAlloc(origin,.11f,direction.normalized,shotCover,direction.magnitude,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)if(shotCover[i].collider.GetComponentInParent<PlayerMovement>()==null)return false;
            return true;
        }
        public void Attack()
        {
            var run=LevelRun.Instance;if(run==null || !run.IsPlaying || Time.time<nextAttack) return;
            var config=GameConfig.Current;var weapon=config.EquippedWeapon;
            nextAttack=Time.time+WeaponUpgrades.Cooldown(weapon);
            int damage=WeaponUpgrades.Damage(weapon);
            Vector3 muzzle=transform.position+Vector3.down*.35f+Vector3.right*movement.Facing*.45f;
            Target=weapon.ranged ? FindTarget(muzzle) : null;
            AimDirection=Target!=null ? (AimPoint-muzzle).normalized : Vector3.right*movement.Facing;
            GetComponent<CharacterPresentation>()?.AttackPose();
            if(weapon.ranged)
            {
                Color element=ElementColor(LevelRun.IsCustom ? LevelRun.CustomLevel.world : CampaignCatalog.World(LevelRun.SelectedLevel));
                ProjectilePool.Fire(muzzle,AimDirection,damage,ArsenalProgression.IsArrow(weapon.id) || weapon.id=="froststaff" ? weapon.color : element,weapon.id,Target,Target!=null ? AimPoint-Target.GetComponent<BoxCollider>().bounds.center : Vector3.zero);
                return;
            }
            strikeStarted=Time.time;strikeDamage=damage;strikeWeapon=weapon.id;unique.Clear();previousSample=false;
        }
        // Called after the visible weapon has been positioned in LateUpdate.
        public void SampleBlade(Vector3 bladeBase,Vector3 bladeTip)
        {
            float phase=(Time.time-strikeStarted)/.38f;
            if(phase<.18f || phase>.72f || LevelRun.Instance==null || !LevelRun.Instance.IsPlaying){previousSample=false;return;}
            int steps=previousSample ? Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(previousTip,bladeTip)/.07f),1,24) : 1;
            for(int s=1;s<=steps;s++)
            {
                Vector3 a=previousSample ? Vector3.Lerp(previousBase,bladeBase,s/(float)steps) : bladeBase;
                Vector3 b=previousSample ? Vector3.Lerp(previousTip,bladeTip,s/(float)steps) : bladeTip;
                int count=Physics.OverlapCapsuleNonAlloc(a,b,.09f,hits,~0,QueryTriggerInteraction.Collide);
                for(int i=0;i<count;i++)
                {
                    var enemy=hits[i].GetComponentInParent<Enemy>();
                    if(enemy==null || enemy.IsDefeated || unique.Contains(enemy))continue;
                    Vector3 contact=hits[i].ClosestPoint((a+b)*.5f);
                    if((contact.x-transform.position.x)*movement.Facing<0 || !ClearShot(transform.position+Vector3.down*.3f,contact))continue;
                    unique.Add(enemy);EnemyWeaponGuard.Apply(enemy,strikeDamage,strikeWeapon);
                }
            }
            previousBase=bladeBase;previousTip=bladeTip;previousSample=true;
        }
        public static Color ElementColor(int world)=>world==1 ? new Color(.25f,.85f,1) : world==4 || world==7 ? new Color(.7f,.25f,1) : world==5 ? new Color(.25f,1,.4f) : new Color(1,.28f,.025f);
    }
}
