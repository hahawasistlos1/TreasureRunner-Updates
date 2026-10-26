using UnityEngine;
namespace TreasureRunner
{
    public class HealthPickup : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var health=other.attachedRigidbody!=null ? other.attachedRigidbody.GetComponent<PlayerHealth>() : null;
            if(health==null || health.health<=0 || (LevelRun.Instance!=null && !LevelRun.Instance.IsPlaying) || (LevelRun.Instance!=null ? health.HeartUnits>=PlayerHealth.MaxHeartUnits : health.health>=health.MaxHealth)) return;
            health.Heal(1);LevelRun.Instance?.HUD?.Toast("+1 Herz");GameAudio.Instance?.Play("coin");Destroy(gameObject);
        }
    }
}
