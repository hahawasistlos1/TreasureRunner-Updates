using System;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public static class LibraryAudioBank
    {
        public static LibraryAudio[] Music=>AssetLibrary.Data.audio.Where(a=>a.use=="music").OrderBy(a=>a.resource,StringComparer.Ordinal).ToArray();
        public static AudioClip Theme(int level)
        {
            var music=Music;if(music.Length==0)return null;
            int i=level<0 ? music.Length-1 : (level&int.MaxValue)%music.Length;
            return Resources.Load<AudioClip>(music[i].resource);
        }
        public static AudioClip Effect(string id)
        {
            string[] words=id=="coin" ? new[]{"handleCoins","coin","confirmation"} : id=="jump" ? new[]{"cloth1","jump"} : id=="hit" ? new[]{"impactPunch_medium","impactSoft_medium","chop"} : id=="kill" ? new[]{"impactPunch_heavy","impactMetal_medium"} : id=="buy" || id=="win" ? new[]{"confirmation","select_001"} : id=="step" ? new[]{"footstep_grass_000","footstep_wood_000"} : id=="click" ? new[]{"click_001","click"} : Array.Empty<string>();
            foreach(var word in words)
            {
                var entry=AssetLibrary.Data.audio.FirstOrDefault(a=>a.use=="effect" && a.title.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);
                if(entry!=null){var clip=Resources.Load<AudioClip>(entry.resource);if(clip!=null)return clip;}
            }
            return null;
        }
    }
    public class LibraryFootsteps:MonoBehaviour
    {
        PlayerMovement movement;Rigidbody body;float next;
        void Start(){movement=GetComponent<PlayerMovement>();body=GetComponent<Rigidbody>();}
        void Update()
        {
            if(movement==null || body==null || Time.timeScale==0 || !movement.IsGrounded || Mathf.Abs(body.linearVelocity.x)<.6f || Time.time<next)return;
            next=Time.time+.30f;GameAudio.Instance?.Play("step");
        }
    }
}
