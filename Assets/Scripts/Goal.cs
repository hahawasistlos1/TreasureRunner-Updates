using UnityEngine;

public class Goal : MonoBehaviour
{
    private bool completed;
    private void OnTriggerEnter(Collider other)
    {
        if (!completed && other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player"))
        {
            if (TreasureRunner.LevelRun.Instance != null)
            {
                TreasureRunner.LevelRun.Instance.Complete();
                completed = TreasureRunner.LevelRun.Instance.State == TreasureRunner.RunState.Complete;
                return;
            }
            completed = true;
            if (!PlayerPrefs.HasKey("TreasureRunner.Prototype.ClearBonus"))
            {
                CoinManager.Instance.AddCoins(100);
                PlayerPrefs.SetInt("TreasureRunner.Prototype.ClearBonus",1);
                PlayerPrefs.Save();
            }

            Debug.Log("LEVEL GESCHAFFT!");

            Time.timeScale = 0f;
        }
    }
}
