using System;
using UnityEngine;
namespace TreasureRunner
{
    public class LibraryMotion:MonoBehaviour
    {
        Animation player;Enemy enemy;EnemyPatrol patrol;string idle,walk,hit,death,attack;float until;
        void Start()
        {
            player=GetComponentInChildren<Animation>();enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();if(player==null)return;player.enabled=true;
            foreach(AnimationState state in player)
            {
                string n=state.name.ToLowerInvariant();if(n.Contains("idle"))idle=state.name;if(n.Contains("walk") || n.Contains("run"))walk=state.name;
                if(n.Contains("hit") || n.Contains("damage"))hit=state.name;if(n.Contains("death") || n.Contains("die"))death=state.name;if(n.Contains("attack") || n.Contains("shoot"))attack=state.name;
                state.wrapMode=n.Contains("idle") || n.Contains("walk") || n.Contains("run") ? WrapMode.Loop : WrapMode.Once;
            }
            if(enemy!=null){enemy.onDamaged.AddListener(()=>Act(hit,.3f));enemy.onDefeated.AddListener(()=>Act(death,1));}Play(idle??walk);
        }
        void Play(string clip){if(player!=null && clip!=null && !player.IsPlaying(clip))player.CrossFade(clip,.1f);}
        void Act(string clip,float duration){until=Time.time+duration;Play(clip);}
        public void Attack(){Act(attack,.5f);}
        void Update(){if(enemy!=null && !enemy.IsDefeated && Time.time>=until)Play(patrol!=null && patrol.IsMoving ? walk??idle : idle??walk);}
    }
}
