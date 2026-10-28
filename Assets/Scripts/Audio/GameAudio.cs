using System.Collections.Generic;
using UnityEngine;

namespace TreasureRunner
{
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }
        private AudioSource music;
        private AudioSource effects;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        private readonly HashSet<AudioClip> owned=new HashSet<AudioClip>();
        public static void Ensure()
        {
            if (Instance == null) new GameObject("GameAudio").AddComponent<GameAudio>();
        }
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            music = gameObject.AddComponent<AudioSource>();
            effects = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            clips["coin"] = Tone(880, .12f);
            clips["jump"] = Tone(440, .14f);
            clips["hit"] = Tone(130, .2f);
            clips["kill"] = Tone(260, .18f);
            clips["win"] = Tone(660, .5f);
            clips["buy"] = Tone(1046, .2f);
            foreach(string key in new[]{"coin","jump","hit","kill","win","buy","click","step"})
            {var imported=LibraryAudioBank.Effect(key);if(imported!=null)clips[key]=imported;}
            music.clip = ThemeClip(-1);
            ApplyVolumes();
            music.Play();
        }
        private int theme=-1;
        public void PlayTheme(int id)
        {
            if(theme==id)return;theme=id;music.Stop();var old=music.clip;
            music.clip=ThemeClip(id);music.Play();if(old!=null && owned.Remove(old))Destroy(old);
        }
        AudioClip ThemeClip(int id){var clip=LibraryAudioBank.Theme(id);if(clip!=null)return clip;clip=AdventureMusic.Compose(id);owned.Add(clip);return clip;}
        private AudioClip Tone(float frequency, float seconds)
        {
            float[] samples = new float[Mathf.CeilToInt(22050 * seconds)];
            for (int i = 0; i < samples.Length; i++)
            {
                float fade = 1 - i / (float)samples.Length;
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 22050f) * fade * fade * .2f;
            }
            AudioClip clip = AudioClip.Create("Tone",samples.Length,1,22050,false);
            clip.SetData(samples,0);
            owned.Add(clip);
            return clip;
        }
        public void Play(string id)
        {
            if (effects != null && clips.TryGetValue(id,out AudioClip clip)) effects.PlayOneShot(clip);
        }
        public void ApplyVolumes()
        {
            music.volume = ProgressStore.Data.muted ? 0 : Mathf.Clamp01(ProgressStore.Data.musicVolume);
            effects.volume = ProgressStore.Data.muted ? 0 : Mathf.Clamp01(ProgressStore.Data.effectsVolume);
        }
        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            foreach (var clip in owned)if(clip!=null)Destroy(clip);
        }
    }
}
