using UnityEngine;
using UnityEngine.SceneManagement;

// Small recovery screen until the full menu system is implemented.
[RequireComponent(typeof(PlayerHealth))]
public class GameOverScreen : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private bool restarting;
    private GUIStyle titleStyle;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    public bool IsVisible => playerHealth != null && playerHealth.health <= 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetTimeForNewGame() => Time.timeScale = 1f;

    private void Awake() => playerHealth = GetComponent<PlayerHealth>();

    private void OnGUI()
    {
        if (!IsVisible || TreasureRunner.LevelRun.Instance != null) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
        }
        float scale = Mathf.Min(Screen.width / 560f, Screen.height / 360f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Screen.width / 2f, Screen.height / 2f),
            Quaternion.identity, Vector3.one * scale);
        GUI.depth = -100;
        GUI.Box(new Rect(-250, -140, 500, 280), GUIContent.none);
        GUI.Label(new Rect(-230, -120, 460, 55), "Game Over", titleStyle);
        GUI.Label(new Rect(-225, -55, 450, 65), "Du hast keine Leben mehr.\nVersuche es noch einmal!", textStyle);
        GUI.enabled = !restarting;
        if (GUI.Button(new Rect(-190, 40, 380, 70), "Erneut versuchen", buttonStyle)) RestartLevel();
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }

    public void RestartLevel()
    {
        if (!IsVisible || restarting) return;
        restarting = true;
        // Scene reload alone does not reset Time.timeScale.
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
