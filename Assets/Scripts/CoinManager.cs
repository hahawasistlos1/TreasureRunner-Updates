using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance;

    public int coins = 0;
    public TextMeshProUGUI coinText;

    private void Awake()
    {
        Instance = this;
        coins = TreasureRunner.ProgressStore.Data.coins;
    }
    private void Start()
    {
        coins = TreasureRunner.ProgressStore.Data.coins;

        if (coinText != null) coinText.text = "Coins: " + coins;
    }
    public void AddCoin()
    {
        AddCoins(1);
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        if(!TreasureRunner.LevelRun.IsCustom)
        {
            int tier=TreasureRunner.AdventureUpgrades.Level(2);int[] interval={0,10,5,3,2};
            int before=TreasureRunner.LevelRun.Instance!=null ? TreasureRunner.LevelRun.Instance.CollectedCoins : 0;
            int bonus=tier>0 ? (before+amount)/interval[tier]-before/interval[tier] : 0;
            TreasureRunner.ProgressStore.AddCoins(amount+bonus);
        }
        TreasureRunner.LevelRun.Instance?.TrackCoins(amount);
        coins = TreasureRunner.ProgressStore.Data.coins;

        if (coinText != null) coinText.text = "Coins: " + coins;

    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
