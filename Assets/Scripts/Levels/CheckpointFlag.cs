using UnityEngine;
namespace TreasureRunner
{
    public class CheckpointFlag : MonoBehaviour
    {
        private bool active;
        public Renderer flagRenderer;
        public bool useExplicitRespawn;
        public Vector3 respawnPosition;
        public bool healOnActivate;
        private void OnTriggerEnter(Collider other)
        {
            if (active || other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player")) return;
            active = true;
            LevelRun.Instance?.SetCheckpoint(useExplicitRespawn ? respawnPosition : transform.position + Vector3.up);
            if(healOnActivate) other.attachedRigidbody.GetComponent<PlayerHealth>()?.Heal(1);
            var renderer = flagRenderer!=null ? flagRenderer : GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor",Color.green);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
