using System;
using UnityEngine;
namespace TreasureRunner
{
    public class SkeletonMotion:MonoBehaviour
    {
        Animation animationPlayer;Enemy enemy;EnemyPatrol patrol;string idle,walk,hit,death,attack;float actionUntil;
        void Start()
        {
            animationPlayer=GetComponentInChildren<Animation>();enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();
            if(animationPlayer==null)return;animationPlayer.enabled=true;
            foreach(AnimationState state in animationPlayer)
            {
                string n=state.name;if(n.Contains("Idle"))idle=n;if(n.Contains("Walking"))walk=n;
                if(n.Contains("Hit"))hit=n;if(n.Contains("Death"))death=n;if(n.Contains("Attack")||n.Contains("Shoot"))attack=n;
                state.wrapMode=n.Contains("Idle")||n.Contains("Walking") ? WrapMode.Loop : WrapMode.Once;
            }
            enemy.onDamaged.AddListener(()=>Act(hit,.35f));enemy.onDefeated.AddListener(()=>Act(death,2));
            foreach(var bone in GetComponentsInChildren<Transform>())
                if(bone.name=="handslot.r")
                {
                    var source=OpenWorldArt.Asset(GetComponent<EnemyRangedAttack>()!=null ? "Skeleton_Staff" : "Skeleton_Blade");
                    if(source!=null){var weapon=Instantiate(source,bone);weapon.name="KayKit-Waffe";weapon.transform.localPosition=Vector3.zero;}
                    break;
                }
            Play(idle);
        }
        void Act(string clip,float duration){actionUntil=Time.time+duration;Play(clip);}
        void Play(string clip){if(animationPlayer!=null && clip!=null && !animationPlayer.IsPlaying(clip))animationPlayer.CrossFade(clip,.12f);}
        public void Attack(){Act(attack,.55f);}
        void Update(){if(enemy!=null && !enemy.IsDefeated && Time.time>=actionUntil)Play(patrol.IsMoving ? walk??idle : idle);}
    }
}
