using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider), typeof(EnemyPatrol))]
public class Enemy : MonoBehaviour
{
    [Min(0)] public float speed = 2f;
    [Min(1)] public int maxHealth = 1;
    [Min(1)] public int contactDamage = 1;
    public bool canBeStomped = true;
    [Min(0)] public int coinReward = 1;
    [Min(0)] public int scoreReward = 100;
    [Min(0)] public float bounceSpeed = 5f;
    [Min(0)] public float deathDelay;
    public UnityEvent onDamaged = new UnityEvent();
    public UnityEvent onDefeated = new UnityEvent();
    public int CurrentHealth { get; private set; }
    public bool IsDefeated { get; private set; }
    public bool EditorPreview {get;set;}
    private BoxCollider hitbox;

    private void Awake()
    {
        CurrentHealth = Mathf.Max(1, maxHealth);
        hitbox = GetComponent<BoxCollider>();
        // Triggers cause damage without pushing the player through solver impulses.
        hitbox.isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
    }

    private void OnTriggerEnter(Collider other) => TouchPlayer(other);
    private void OnTriggerStay(Collider other) => TouchPlayer(other);

    private void TouchPlayer(Collider other)
    {
        if (EditorPreview || IsDefeated || Time.timeScale == 0f) return;
        Rigidbody player = other.attachedRigidbody;
        if (player == null || !player.CompareTag("Player")) return;
        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health == null || health.health <= 0) return;
        float allowance = Mathf.Max(0.12f, -player.linearVelocity.y * Time.fixedDeltaTime + 0.06f);
        bool fromAbove = player.linearVelocity.y < -0.1f &&
            other.bounds.min.y >= hitbox.bounds.max.y - allowance &&
            other.bounds.center.y > hitbox.bounds.max.y;
        if (canBeStomped && fromAbove)
        {
            health.GrantInvulnerability(0.15f);
            TakeDamage(1);
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null) movement.Bounce(bounceSpeed);
            else
            {
                Vector3 velocity = player.linearVelocity;
                velocity.y = bounceSpeed;
                player.linearVelocity = velocity;
            }
        }
        else health.TakeDamage(contactDamage);
    }

    // Shared damage entry point for future weapons.
    public void TakeDamage(int amount)
    {
        if (EditorPreview || IsDefeated || amount <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        if (CurrentHealth > 0) { onDamaged.Invoke(); return; }
        IsDefeated = true;
        hitbox.enabled = false;
        GetComponent<EnemyPatrol>().enabled = false;
        if (CoinManager.Instance != null) CoinManager.Instance.AddCoins(coinReward);
        if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(scoreReward);
        TreasureRunner.LevelRun.Instance?.ReportKill();
        onDefeated.Invoke();
        Destroy(gameObject,deathDelay);
    }
}
