using UnityEngine;
namespace TreasureRunner
{
    [DefaultExecutionOrder(150)] public class LibraryEnemyEquipment:MonoBehaviour
    {
        public string skinId,weaponId;public int world;public Transform visual;
        Transform upper,lower,hand;WeaponModel weapon;Enemy enemy;EnemyPatrol patrol;EnemyRangedAttack ranged;
        public Vector3 ShotOrigin=>hand!=null ? hand.position+Aim*.2f : transform.position+Vector3.up*.2f;
        Vector3 Aim {get{if(ranged!=null && ranged.EditorPreview)return Vector3.left;var player=LevelRun.Instance?.Player;return player!=null ? (player.transform.position-Vector3.up*.25f-ShotAnchor).normalized : transform.right;}}
        Vector3 ShotAnchor=>hand!=null ? hand.position : transform.position;
        void Start()
        {
            enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();ranged=GetComponent<EnemyRangedAttack>();
            upper=CharacterRig.Bone(visual,HumanBodyBones.RightUpperArm);lower=CharacterRig.Bone(visual,HumanBodyBones.RightLowerArm);hand=CharacterRig.Bone(visual,HumanBodyBones.RightHand);
            if(hand!=null)weapon=WeaponModel.Build(hand,weaponId);
        }
        void LateUpdate()
        {
            if(weapon==null)return;if(enemy.IsDefeated){weapon.gameObject.SetActive(false);return;}
            float facing=visual.forward.x>=0 ? 1 : -1;Vector3 forward=Vector3.right*facing;
            bool aiming=ranged!=null && ranged.IsPreparing;
            var player=LevelRun.Instance?.Player;float distance=player!=null ? Vector3.Distance(player.transform.position,transform.position) : 99;
            float strike=ranged==null && distance<1.6f ? Mathf.Max(0,Mathf.Sin(Time.time*7)) : 0;
            ForwardArmPose.Reach(upper,lower,hand,transform.position+(aiming ? Aim*.65f : forward*Mathf.Lerp(.3f,.68f,strike))+Vector3.down*.12f+Vector3.back*.13f);
            weapon.Aim(hand.position,aiming && weaponId!="bow" ? Aim : ranged==null ? Vector3.Lerp(Vector3.up,forward,strike).normalized : Vector3.up);
        }
    }
}
