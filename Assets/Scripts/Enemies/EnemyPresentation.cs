using UnityEngine;
namespace TreasureRunner
{
    public class EnemyPresentation : MonoBehaviour
    {
        private Enemy enemy;
        private Renderer[] renderers;
        private float flashUntil;
        private bool dead;
        private bool flashing;
        private float deathTime;
        private Quaternion restRotation;
        private MaterialPropertyBlock block;
        private MaterialPropertyBlock[][] originalBlocks;
        private void Awake()
        {
            block = new MaterialPropertyBlock();
            enemy = GetComponent<Enemy>();
            renderers = GetComponentsInChildren<Renderer>();
            originalBlocks=new MaterialPropertyBlock[renderers.Length][];for(int i=0;i<renderers.Length;i++){originalBlocks[i]=new MaterialPropertyBlock[renderers[i].sharedMaterials.Length];for(int slot=0;slot<originalBlocks[i].Length;slot++){originalBlocks[i][slot]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(originalBlocks[i][slot],slot);}}
            restRotation = transform.rotation;
            enemy.onDamaged.AddListener(() => { flashUntil = Time.time + .12f; GameAudio.Instance?.Play("hit"); });
            enemy.onDefeated.AddListener(() => { dead = true; deathTime = Time.time; GameEffects.Spark(transform.position); });
        }
        private void Update()
        {
            if (dead)
            {
                foreach(var r in renderers) if(r!=null) r.enabled=false;
                return;
            }
            bool shouldFlash = Time.time < flashUntil;
            if (shouldFlash == flashing) return;
            flashing = shouldFlash;
            for(int i=0;i<renderers.Length;i++)
            {
                var renderer=renderers[i];if(renderer==null)continue;
                for(int slot=0;slot<originalBlocks[i].Length;slot++)if (flashing)
                {
                    block.Clear();block.SetColor("_BaseColor",Color.white * 2); renderer.SetPropertyBlock(block,slot);
                }
                else renderer.SetPropertyBlock(originalBlocks[i][slot],slot);
            }
        }
    }
}
