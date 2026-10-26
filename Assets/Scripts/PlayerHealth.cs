using TMPro;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 3;
    public TextMeshProUGUI healthText;
    [Min(0)] public float invulnerabilitySeconds = 1f;
    private float nextDamageTime;
    public int MaxHealth { get; private set; }
    // health/MaxHealth remain the life counter for existing saves and game-over code.
    public const int MaxHeartUnits = 6;
    public int HeartUnits { get; private set; } = MaxHeartUnits;
    private TreasureRunner.LevelRun CampaignRun => TreasureRunner.LevelRun.Instance != null && TreasureRunner.LevelRun.Instance.Player == this ? TreasureRunner.LevelRun.Instance : null;

    private void OnEnable()
    {
        if (GetComponent<GameOverScreen>() == null)
            gameObject.AddComponent<GameOverScreen>();
    }

    public void GrantInvulnerability(float seconds)
    {
        nextDamageTime = Mathf.Max(nextDamageTime, Time.time + Mathf.Max(0, seconds));
    }

    void Start()
    {
        MaxHealth = Mathf.Max(1,health);
        HeartUnits = MaxHeartUnits;
        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        if(amount<=0 || health<=0) return;
        if(CampaignRun!=null) HeartUnits=Mathf.Min(MaxHeartUnits,HeartUnits+(int)System.Math.Min(MaxHeartUnits,(long)amount*2));
        else health=Mathf.Min(MaxHealth,health+amount);
        UpdateHealthUI();
    }
    public void RestoreHearts() { HeartUnits=MaxHeartUnits;UpdateHealthUI(); }
    public void RestoreFullHealth() { health = Mathf.Max(1,MaxHealth); RestoreHearts();nextDamageTime = 0; UpdateHealthUI(); }
    public void Kill()
    {
        if (health <= 0) return;
        health = 0;HeartUnits=0; UpdateHealthUI(); Time.timeScale = 0;
    }

    public void TakeFallDamage()=>ApplyDamage(1,true);

    public void TakeDamage(int damage)
    {
        ApplyDamage(damage,false);
    }

    private void ApplyDamage(int damage,bool fall)
    {
        var run=CampaignRun;
        if (damage <= 0 || health <= 0 || Time.timeScale<=0 || (run!=null && !run.IsPlaying) || (!fall && Time.time < nextDamageTime)) return;
        if(!fall && GetComponent<TreasureRunner.PlayerWard>()?.Absorb()==true){GrantInvulnerability(.5f);return;}
        if(!fall && GetComponent<TreasureRunner.CharacterAbility>()?.Absorb()==true)return;
        float protection=run!=null && !TreasureRunner.LevelRun.IsCustom ? TreasureRunner.GameConfig.Current.Stat(4)*.25f+TreasureRunner.AdventureUpgrades.Value(4) : 0;
        nextDamageTime = Time.time + invulnerabilitySeconds+protection;
        bool lostLife=false;
        if(run!=null)
        {
            // Every accepted enemy/trap hit takes exactly half of one of three hearts.
            HeartUnits=Mathf.Max(0,HeartUnits-1);
            if(HeartUnits==0){health--;lostLife=true;}
        }
        else health -= damage;
        GetComponent<TreasureRunner.CharacterPresentation>()?.HitPose();
        TreasureRunner.GameEffects.Impact(transform.position,new Color(1,.4f,.4f));
        TreasureRunner.GameAudio.Instance?.Play("hit");

        if (health < 0)
            health = 0;

        UpdateHealthUI();

        if(health>0 && (lostLife || fall)) run?.RecoverFromHit(lostLife);
        if (health <= 0)
        {
            Debug.Log("Game Over");
            Time.timeScale = 0f;
        }
    }

    void UpdateHealthUI()
    {
        if (healthText != null) healthText.text = CampaignRun!=null ? "Herzen: " + (HeartUnits*.5f).ToString("0.#") + "/3 · Leben: " + health : "Health: " + health;
    }
}
