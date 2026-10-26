using TMPro;
using UnityEngine;

// Session score only; persistent highscores belong to the progression phase.
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }
    public TextMeshProUGUI scoreText;
    public int Score { get; private set; }
    private void Awake() { Instance = this; }
    private void Start() { Refresh(); }
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;
        Score += amount;
        Refresh();
    }
    private void Refresh()
    {
        if (scoreText != null) scoreText.text = "Punkte: " + Score;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
