using UnityEngine;

public class Coin : MonoBehaviour
{
    [Min(1)] public int value = 1;
    private bool collected;
    private void Update()
    {
        var run=TreasureRunner.LevelRun.Instance;
        if(collected || run==null || !run.IsPlaying || run.Player==null)return;
        float skill=run.Player.GetComponent<TreasureRunner.CharacterAbility>()?.MagnetRange??0;
        float range=.6f+TreasureRunner.AdventureUpgrades.Value(0)+skill;
        if((TreasureRunner.AdventureUpgrades.Level(0)>0 || skill>0) && Vector3.Distance(transform.position,run.Player.transform.position)<range)
            transform.position=Vector3.MoveTowards(transform.position,run.Player.transform.position,7*Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!collected && other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            collected = true;
            if (CoinManager.Instance != null) CoinManager.Instance.AddCoins(value);
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddPoints(value * 10);
            TreasureRunner.GameAudio.Instance?.Play("coin");
            TreasureRunner.GameEffects.Spark(transform.position);
            Destroy(gameObject);
        }
    }
}
