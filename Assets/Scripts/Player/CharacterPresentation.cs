using UnityEngine;

namespace TreasureRunner
{
    public class CharacterPresentation : MonoBehaviour
    {
        public Transform visual;
        private PlayerMovement movement;
        private Rigidbody body;
        private Enemy enemy;
        private EnemyPatrol patrol;
        private MeshFilter[] weaponMeshes;
        private Renderer[] weaponRenderers;
        private bool melee;
        private Transform leftLeg, rightLeg, leftArm, rightArm, rightForearm, rightHand, leftForearm, leftHand, weapon;
        private Quaternion ll,rl,la,ra;
        private float attackUntil;
        private float attackStarted=-10;
        private float phase;
        private float hitUntil;private Vector3 restPosition;private Renderer[] bodyRenderers;private bool[] rendererEnabled;
        public void HitPose(){hitUntil=Time.time+.45f;}
        public void RefreshWeapon()
        {
            if(visual==null) return;
            foreach(Transform t in visual.GetComponentsInChildren<Transform>()) if(t.name.StartsWith("Equipped ")){t.gameObject.SetActive(false);Destroy(t.gameObject);}
            VisualStyle.Weapon(visual);
            weapon=null;foreach(Transform t in visual.GetComponentsInChildren<Transform>()) if(t.gameObject.activeInHierarchy && t.name.StartsWith("Equipped ")){weapon=t;break;}
            if(weapon!=null){weaponMeshes=weapon.GetComponentsInChildren<MeshFilter>();weaponRenderers=weapon.GetComponentsInChildren<Renderer>();}
            melee=!GameConfig.Current.EquippedWeapon.ranged;
        }
        private void Start()
        {
            movement = GetComponent<PlayerMovement>(); body = GetComponent<Rigidbody>(); enemy = GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();
            if (visual == null) return;
            restPosition=visual.localPosition;bodyRenderers=visual.GetComponentsInChildren<Renderer>();rendererEnabled=System.Array.ConvertAll(bodyRenderers,r=>r.enabled);
            if (movement != null) VisualStyle.Apply(visual);
            var animator = visual.GetComponentInChildren<Animator>();
            if (!CharacterRig.Humanoid(visual) && movement==null) return;
            if(animator!=null)animator.applyRootMotion = false;
            if (movement != null) VisualStyle.Weapon(visual);
            leftLeg = CharacterRig.Bone(visual,HumanBodyBones.LeftUpperLeg);
            rightLeg = CharacterRig.Bone(visual,HumanBodyBones.RightUpperLeg);
            leftArm = CharacterRig.Bone(visual,HumanBodyBones.LeftUpperArm);
            rightArm = CharacterRig.Bone(visual,HumanBodyBones.RightUpperArm);
            rightForearm=CharacterRig.Bone(visual,HumanBodyBones.RightLowerArm);
            rightHand=CharacterRig.Bone(visual,HumanBodyBones.RightHand);
            leftForearm=CharacterRig.Bone(visual,HumanBodyBones.LeftLowerArm);leftHand=CharacterRig.Bone(visual,HumanBodyBones.LeftHand);
            if(movement!=null) foreach(Transform t in visual.GetComponentsInChildren<Transform>())
                if(t.name.StartsWith("Equipped ")) { weapon=t;break; }
            if(weapon!=null) { weaponMeshes=weapon.GetComponentsInChildren<MeshFilter>();weaponRenderers=weapon.GetComponentsInChildren<Renderer>(); }
            if(movement!=null) melee=!GameConfig.Current.EquippedWeapon.ranged;
            if (leftLeg != null) ll = leftLeg.localRotation;
            if (rightLeg != null) rl = rightLeg.localRotation;
            if (leftArm != null) la = leftArm.localRotation;
            if (rightArm != null) ra = rightArm.localRotation;
        }
        public void AttackPose() { attackStarted=Time.time;attackUntil=Time.time+.38f; }
        private void LateUpdate()
        {
            if (visual == null) return;
            if (movement != null)
            {
                float hit=Time.time<hitUntil ? Mathf.Sin((hitUntil-Time.time)*30)*.1f : 0;
                visual.localPosition=restPosition+Vector3.right*hit;
                visual.localRotation = Quaternion.Euler(0,movement.Facing > 0 ? 90 : -90,hit*100);
                for(int i=0;i<bodyRenderers.Length;i++) if(bodyRenderers[i]!=null) bodyRenderers[i].enabled=rendererEnabled[i] && (Time.time>=hitUntil || Mathf.FloorToInt(Time.time*20)%2==0);
            }
            float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0;
            if (enemy != null && !enemy.IsDefeated) speed = patrol.Suspended || !patrol.IsMoving ? 0 : enemy.speed;
            phase += Time.deltaTime * Mathf.Max(2,speed * 3);
            float swing = Mathf.Sin(phase) * Mathf.Min(28,speed * 7);
            if (movement != null && !movement.IsGrounded) swing = body.linearVelocity.y>0 ? -24 : 12;
            if (leftLeg != null) leftLeg.localRotation = ll * Quaternion.Euler(swing,0,0);
            if (rightLeg != null) rightLeg.localRotation = rl * Quaternion.Euler(-swing,0,0);
            if (leftArm != null) leftArm.localRotation = la * Quaternion.Euler(-swing,0,35);
            if (rightArm != null) rightArm.localRotation = ra * Quaternion.Euler(swing,0,-35);
            if(movement!=null && rightHand!=null)
            {
                Vector3 forward=Vector3.right*movement.Facing;
                float strike=Time.time<attackUntil ? Mathf.Sin(Mathf.Clamp01((Time.time-attackStarted)/.38f)*Mathf.PI) : 0;
                Vector3 target=transform.position+forward*Mathf.Lerp(.32f,.92f,strike)+Vector3.up*Mathf.Lerp(-.25f,-.32f,strike)+Vector3.back*.15f;
                ForwardArmPose.Reach(rightArm,rightForearm,rightHand,target);
                if(!melee && Time.time<attackUntil)
                {
                    var aim=GetComponent<PlayerCombat>().AimDirection;
                    ForwardArmPose.Reach(rightArm,rightForearm,rightHand,transform.position+aim*.8f+Vector3.back*.12f);
                    if(ArsenalProgression.IsArrow(GameConfig.Current.EquippedWeapon.id) && leftHand!=null)
                        ForwardArmPose.Reach(leftArm,leftForearm,leftHand,transform.position+aim*Mathf.Lerp(.65f,.2f,strike)+Vector3.back*.28f);
                    else if(leftArm!=null) leftArm.localRotation=la*Quaternion.Euler(-65,0,30);
                }
                var model=weapon!=null ? weapon.GetComponent<WeaponModel>() : null;
                if(model!=null)
                {
                    Vector3 direction=melee ? (Vector3.up*(1-strike)+forward*strike).normalized : Vector3.up;
                    model.Aim(rightHand.position,direction);
                    if(melee && model.bladeBase!=null)GetComponent<PlayerCombat>().SampleBlade(model.bladeBase.position,model.bladeTip.position);
                }
            }
        }
    }
}
