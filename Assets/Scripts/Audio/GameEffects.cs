using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class GameEffects : MonoBehaviour
    {
        private static GameEffects instance;
        private readonly Queue<ParticleSystem> pool = new Queue<ParticleSystem>();
        private int created;
        private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        public static void Spark(Vector3 position)
        {
            if (GameConfig.Current == null || ProgressStore.Data.graphicsQuality == 0) return;
            if (instance == null) instance = new GameObject("Effect Pool").AddComponent<GameEffects>();
            instance.Emit(position,"star_04",GameUI.Gold);
        }
        public static void Impact(Vector3 position,Color color,string texture="spark_05")
        {
            if(GameConfig.Current==null || ProgressStore.Data.graphicsQuality==0)return;
            if(instance==null)instance=new GameObject("Effect Pool").AddComponent<GameEffects>();
            instance.Emit(position,texture,color);
        }
        private void Emit(Vector3 position,string texture,Color color)
        {
            ParticleSystem particles;
            if (pool.Count > 0) particles = pool.Dequeue();
            else
            {
                if (created >= 12) return;
                created++;
                var go = new GameObject("Treasure spark");
                go.transform.SetParent(transform);
                particles = go.AddComponent<ParticleSystem>();
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false; main.loop = false; main.duration = .4f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(.22f,.48f); main.startSpeed = new ParticleSystem.MinMaxCurve(1.1f,2.5f); main.startSize = new ParticleSystem.MinMaxCurve(.12f,.27f);
                main.startColor = new ParticleSystem.MinMaxGradient(GameUI.Gold,new Color(1,.96f,.64f)); main.maxParticles = 12; main.useUnscaledTime = true;main.gravityModifier=.7f;
                var emission = particles.emission; emission.rateOverTime = 0;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0,12) });
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .1f;
                var fade=particles.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(GameUI.Gold,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});fade.color=gradient;
            }
            if(!materials.TryGetValue(texture,out var material))
            {
                var template=Resources.Load<Material>("OpenArt/Materials/Particle");if(template==null){pool.Enqueue(particles);return;}
                material=new Material(template);material.SetTexture("_BaseMap",Resources.Load<Texture2D>("OpenArt/Particles/"+texture));materials[texture]=material;
            }
            particles.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;var settings=particles.main;settings.startColor=color;
            particles.transform.position = position; particles.gameObject.SetActive(true);
            particles.Play();
            StartCoroutine(Return(particles));
        }
        private IEnumerator Return(ParticleSystem particles)
        {
            yield return new WaitForSecondsRealtime(.6f);
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.gameObject.SetActive(false); pool.Enqueue(particles);
        }
        private void OnDestroy() { foreach(var m in materials.Values)Destroy(m);if (instance == this) instance = null; }
    }
}
