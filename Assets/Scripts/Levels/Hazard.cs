using UnityEngine;
namespace TreasureRunner
{
    public class Hazard : MonoBehaviour
    {
        public int damage = 1;
        private void OnTriggerEnter(Collider other) => Hit(other);
        private void OnTriggerStay(Collider other) => Hit(other);
        private void Hit(Collider other)
        {
            if (Time.timeScale <= 0 || other.attachedRigidbody == null) return;
            var health = other.attachedRigidbody.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);
        }
    }
}
