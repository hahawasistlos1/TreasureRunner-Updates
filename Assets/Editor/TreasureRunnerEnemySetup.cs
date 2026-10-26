using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TreasureRunnerEnemySetup
{
    [MenuItem("TreasureRunner/Gegner in SampleScene einrichten")]
    public static void Setup()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Directory.CreateDirectory("Assets/Prefabs/Enemies");
        AssetDatabase.Refresh();
        GameObject goblin = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Standout7/LOWPO_Goblin_Pack/Prefabs/PFB_Basic_Goblin.prefab");
        if (goblin == null) throw new InvalidOperationException("Goblin prefab missing");
        Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)
            .OrderBy(e => e.transform.position.x).ToArray();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            bool stone = i == enemies.Length - 1;
            enemy.name = stone ? "Steinmonster_Platzhalter" : "Goblin_" + (i + 1);
            enemy.speed = stone ? 0.7f : 1.6f;
            enemy.maxHealth = stone ? 3 : 1;
            enemy.contactDamage = stone ? 2 : 1;
            enemy.canBeStomped = true;
            enemy.coinReward = stone ? 5 : 1;
            enemy.scoreReward = stone ? 300 : 100;
            var patrol = enemy.GetComponent<EnemyPatrol>();
            if (patrol == null) patrol = enemy.gameObject.AddComponent<EnemyPatrol>();
            patrol.leftDistance = stone ? 1.1f : 0.8f;
            patrol.rightDistance = stone ? 1.4f : 0.8f;
            patrol.startMovingRight = i % 2 == 0;
            enemy.transform.localScale = Vector3.one;
            var box = enemy.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = stone ? new Vector3(0.9f, 1.2f, 0.65f) : new Vector3(0.65f, 0.9f, 0.55f);
            box.center = Vector3.zero;
            Vector3 position = enemy.transform.position;
            position.y = 0.85f + box.size.y * 0.5f;
            position.z = 0.47f;
            enemy.transform.position = position;
            Rigidbody body = enemy.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            if (!stone)
            {
                enemy.GetComponent<MeshRenderer>().enabled = false;
                Transform visual = enemy.transform.Find("Visual");
                if (visual == null)
                {
                    GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(goblin, enemy.transform);
                    instance.name = "Visual";
                    visual = instance.transform;
                    foreach (Collider c in instance.GetComponentsInChildren<Collider>()) c.enabled = false;
                    foreach (Rigidbody b in instance.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(b);
                    foreach (Animator a in instance.GetComponentsInChildren<Animator>()) a.applyRootMotion = false;
                    Bounds bounds = new Bounds();
                    bool first = true;
                    foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    { if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds); }
                    float scale = 0.9f / Mathf.Max(0.01f, bounds.size.y);
                    visual.localScale *= scale;
                    first = true;
                    foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
                    { if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds); }
                    visual.position += new Vector3(position.x - bounds.center.x,
                        0.85f - bounds.min.y, position.z - bounds.center.z);
                }
                patrol.visual = visual;
            }
            if (stone || i == 0)
                PrefabUtility.SaveAsPrefabAsset(enemy.gameObject,
                    "Assets/Prefabs/Enemies/" + (stone ? "StoneEnemy_Placeholder" : "GoblinEnemy") + ".prefab");
            EditorUtility.SetDirty(enemy);
            Debug.Log($"ENEMY_SETUP {enemy.name}: x={position.x}, HP={enemy.maxHealth}, speed={enemy.speed}");
        }
        CoinManager coins = UnityEngine.Object.FindFirstObjectByType<CoinManager>();
        if (coins != null)
        {
            ScoreManager score = coins.GetComponent<ScoreManager>();
            if (score == null) score = coins.gameObject.AddComponent<ScoreManager>();
            if (score.scoreText == null && coins.coinText != null)
            {
                score.scoreText = UnityEngine.Object.Instantiate(coins.coinText, coins.coinText.transform.parent);
                score.scoreText.name = "ScoreText";
                score.scoreText.text = "Punkte: 0";
                score.scoreText.rectTransform.anchoredPosition += Vector2.down * 100f;
            }
        }
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("ENEMY_SETUP_OK");
    }
}
