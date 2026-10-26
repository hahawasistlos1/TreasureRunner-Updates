#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs only in an isolated, unsaved test scene; excluded from Android builds.
public class EnemySmokeTests : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        TreasureRunner.ProgressStore.BeginMemoryTest();
        IEnumerator tests = Scenarios();
        bool failed = false;
        while (true)
        {
            object next = null;
            bool more;
            try { more = tests.MoveNext(); if (more) next = tests.Current; }
            catch (Exception ex) { results.Add("FAIL: " + ex); failed = true; break; }
            if (!more) break;
            yield return next;
        }
        TreasureRunner.ProgressStore.EndMemoryTest();
        Time.timeScale = 1;
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/EnemyTests.txt", results);
        foreach (string result in results) Debug.Log(result);
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
        else EditorApplication.ExitPlaymode();
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        results.Add("PASS: " + description);
    }

    private Enemy MakeEnemy(Vector3 position, int hp = 1, float speed = 0)
    {
        GameObject go = new GameObject("TestEnemy");
        go.SetActive(false);
        go.transform.position = position;
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.maxHealth = hp;
        enemy.speed = speed;
        go.SetActive(true);
        return enemy;
    }

    private IEnumerator Scenarios()
    {
        var manager = new GameObject("Coins").AddComponent<CoinManager>();
        var score = new GameObject("Score").AddComponent<ScoreManager>();
        yield return null;
        int initialCoins = manager.coins;
        Enemy side = MakeEnemy(new Vector3(0, 1, 0));
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.tag = "Player";
        player.transform.position = new Vector3(0.7f, 1, 0);
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.useGravity = false;
        var health = player.AddComponent<PlayerHealth>();
        health.health = 10;
        var movement = player.AddComponent<PlayerMovement>();
        for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        Check(health.health == 9, "Side contact damages once during invulnerability");
        Check(Mathf.Abs(body.position.x - 0.7f) < 0.01f && Mathf.Abs(body.position.z) < 0.01f,
            "Enemy overlap does not displace player sideways or in Z");
        Check(side.CurrentHealth == 1, "Side contact does not count as stomp");
        Destroy(side.gameObject);

        Enemy stomp = MakeEnemy(new Vector3(4, 0.5f, 0));
        body.position = new Vector3(4, 2.2f, 0);
        body.linearVelocity = Vector3.down * 3;
        Physics.SyncTransforms();
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Check(stomp == null, "Descending top contact defeats one-HP enemy");
        Check(body.linearVelocity.y > 0, "Stomp bounces player upward");
        Check(manager.coins == initialCoins + 1 && score.Score == 100, "Enemy awards coins and separate score");

        Enemy stone = MakeEnemy(new Vector3(8, 0.5f, 0), 3);
        body.position = new Vector3(8, 2.2f, 0);
        body.linearVelocity = Vector3.down * 3;
        Physics.SyncTransforms();
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Check(stone.CurrentHealth == 2, "Three-HP enemy survives first stomp and loses one HP");
        Check(health.health == 9, "Successful stomps cause no contact damage while bouncing");
        stone.TakeDamage(2);
        stone.TakeDamage(10);
        Check(manager.coins == initialCoins + 2 && score.Score == 200, "Repeated lethal damage awards rewards only once");

        Enemy armored = MakeEnemy(new Vector3(20, 0.5f, 0), 3);
        armored.canBeStomped = false;
        body.position = new Vector3(20, 2.2f, 0);
        body.linearVelocity = Vector3.down * 3;
        Physics.SyncTransforms();
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Check(armored.CurrentHealth == 3, "Stomp immunity is configurable");
        Destroy(armored.gameObject);
        body.position = new Vector3(100, 5, 0);
        body.linearVelocity = Vector3.zero;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "PlatformWithAnyName";
        floor.transform.position = new Vector3(12, -0.5f, 0);
        floor.transform.localScale = new Vector3(3, 1, 3);
        Enemy patrol = MakeEnemy(new Vector3(12, 0.5f, 0), 1, 2);
        EnemyPatrol path = patrol.GetComponent<EnemyPatrol>();
        path.leftDistance = 10;
        path.rightDistance = 10;
        Physics.SyncTransforms();
        float min = 12, max = 12;
        for (int i = 0; i < 160; i++)
        {
            yield return new WaitForFixedUpdate();
            float x = patrol.GetComponent<Rigidbody>().position.x;
            min = Mathf.Min(min, x); max = Mathf.Max(max, x);
        }
        Check(min < 11.8f && max > 12.2f && min >= 11 && max <= 13,
            "Patrol reverses at both platform ledges");
        path.leftDistance = 0.3f;
        path.rightDistance = 0.3f;
        patrol.GetComponent<Rigidbody>().position = new Vector3(12, 0.5f, 0);
        for (int i = 0; i < 80; i++)
        {
            yield return new WaitForFixedUpdate();
            float x = patrol.GetComponent<Rigidbody>().position.x;
            if (x < 11.69f || x > 12.31f) throw new Exception("Patrol exceeded individual boundaries");
        }
        Check(true, "Individual patrol boundaries stay relative to spawn");
        Destroy(patrol.gameObject);

        body.position = new Vector3(12, 2, 0);
        body.useGravity = true;
        body.linearVelocity = Vector3.zero;
        for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
        Check(movement.IsGrounded, "Grounding works on a platform not named Cube");
        FieldInfo jump = typeof(PlayerMovement).GetField("jumpRequested", BindingFlags.Instance | BindingFlags.NonPublic);
        jump.SetValue(movement, true);
        // Consume the injected request in the physics step before a keyboard-less Update clears it.
        movement.SendMessage("FixedUpdate");
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Check(body.linearVelocity.y > 0 && !movement.IsGrounded, "Ground jump leaves floor");
        float previous = body.linearVelocity.y;
        jump.SetValue(movement, true);
        movement.SendMessage("FixedUpdate");
        for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
        Check(body.linearVelocity.y < previous, "Second jump request in the air adds no upward impulse");
        yield return SceneManager.LoadSceneAsync("SampleScene");
        for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
        Enemy[] sceneEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        Check(sceneEnemies.Length == 4, "Configured SampleScene loads with all four enemies");
        foreach (Enemy e in sceneEnemies)
        {
            Check(e.GetComponent<EnemyPatrol>() != null && e.GetComponent<BoxCollider>().isTrigger &&
                e.GetComponent<Rigidbody>().isKinematic, "Scene enemy has safe physics: " + e.name);
            if (!e.name.StartsWith("Goblin")) continue;
            Transform visual = e.GetComponent<EnemyPatrol>().visual;
            Check(visual != null && visual.GetComponentsInChildren<Renderer>().Length > 0,
                "Scene Goblin uses imported rendered model: " + e.name);
        }
        Check(ScoreManager.Instance != null && ScoreManager.Instance.scoreText != null,
            "SampleScene has a separate score display");
        PlayerHealth sceneHealth = FindFirstObjectByType<PlayerHealth>();
        sceneHealth.invulnerabilitySeconds = 0;
        sceneHealth.TakeDamage(sceneHealth.health);
        GameOverScreen gameOver = sceneHealth.GetComponent<GameOverScreen>();
        Check(Time.timeScale == 0 && gameOver != null && gameOver.IsVisible,
            "Lethal hit pauses gameplay and shows a restart screen");
        gameOver.RestartLevel();
        yield return null;
        yield return null;
        Check(Time.timeScale == 1 && FindFirstObjectByType<PlayerHealth>().health == 3,
            "Restart reloads level with full health and running time");
        Check(!FindFirstObjectByType<GameOverScreen>().IsVisible,
            "Game-over screen closes after restart");
        results.Add("ALL ENEMY TESTS PASSED");
    }
}
#endif
