using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreasureRunner
{
    public enum RunState { Playing, Paused, Dead, Complete, Recovering }
    public class LevelRun : MonoBehaviour
    {
        public static LevelRun Instance { get; private set; }
        public static int SelectedLevel;
        public RunState State { get; private set; } = RunState.Playing;
        public bool IsPlaying => State == RunState.Playing;
        public PlayerHealth Player { get; set; }
        public Vector3 Spawn { get; set; }
        public Vector3 Checkpoint { get; private set; }
        public bool HasCheckpoint { get; private set; }
        public int Revives { get; private set; } = 2;
        public float Elapsed { get; private set; }
        public int CollectedCoins { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int CompletionBonus { get; private set; }
        public int FinalScore { get; private set; }
        public CampaignHUD HUD { get; set; }
        public bool BossAlive { get; set; }
        private int livingBosses;
        public void RegisterBoss(){livingBosses++;BossAlive=true;}
        public void DefeatBoss(){livingBosses=Mathf.Max(0,livingBosses-1);BossAlive=livingBosses>0;HUD?.Toast(BossAlive ? "Wächter besiegt – weitere Wächter warten." : "Alle Wächter besiegt! Das Portal ist offen.");}
        private float fallThreshold = -8;
        private void OnApplicationPause(bool paused)
        {
            if (paused && Application.isMobilePlatform && Player != null && IsPlaying) Pause();
        }
        public static CommunityLevel CustomLevel;
        public static string CustomWeapon="sword";
        public static bool IsCustom => CustomLevel!=null;
        public string DisplayName => IsCustom ? CustomLevel.title : GameConfig.Current.LevelName(SelectedLevel);
        private void Awake()
        {
            Instance = this; Revives=2+AdventureUpgrades.Level(3);
            Time.timeScale = 1;
        }
        private void Update()
        {
            if (!IsPlaying || Player == null) return;
            Elapsed += Time.deltaTime;
            if (Player.health <= 0) { State = RunState.Dead; Time.timeScale = 0; HUD?.ShowState(); return; }
            // Interpolated visual transforms can still show the old fall position just after a respawn.
            if (Player.GetComponent<Rigidbody>().position.y < fallThreshold)
            {
                Player.TakeFallDamage();
            }
        }
        public void RecoverFromHit(bool lostLife=false)
        {
            if(!IsPlaying || Player==null || Player.health<=0) return;
            StartCoroutine(Recover(lostLife));
        }
        private System.Collections.IEnumerator Recover(bool lostLife)
        {
            State=RunState.Recovering;
            var movement=Player.GetComponent<PlayerMovement>();movement.ClearInput();movement.ControlLocked=true;
            yield return new WaitForSeconds(.45f);
            var body=Player.GetComponent<Rigidbody>();body.position=HasCheckpoint ? Checkpoint : Spawn;body.linearVelocity=Vector3.zero;
            if(lostLife)Player.RestoreHearts();
            movement.ClearInput();movement.ControlLocked=false;Player.GrantInvulnerability(2+GameConfig.Current.Stat(4)*.25f+AdventureUpgrades.Value(4));
            State=RunState.Playing;HUD?.Toast((lostLife ? "Ein Leben verloren – " : "Gestürzt – ")+(HasCheckpoint ? "zurück zum Checkpoint" : "zurück zum Start"));
        }
        public void TrackCoins(int amount) { if (amount > 0) CollectedCoins += amount; }
        public void ReportKill()
        {
            DefeatedEnemies++;
            if(IsCustom)return;
            ProgressStore.Data.totalEnemies++;
            ProgressStore.Save();
            GameAudio.Instance?.Play("kill");
        }
        public void SetCheckpoint(Vector3 point)
        {
            if (!IsPlaying) return;
            Checkpoint = point; HasCheckpoint = true;
            HUD?.Toast("Checkpoint erreicht");
            GameAudio.Instance?.Play("coin");
        }
        public void Pause()
        {
            if (!IsPlaying) return;
            State = RunState.Paused; Time.timeScale = 0; HUD?.ShowState();
        }
        public void Resume()
        {
            if (State != RunState.Paused) return;
            State = RunState.Playing; Time.timeScale = 1; HUD?.ShowState();
        }
        public void ReviveAtCheckpoint()
        {
            if (State != RunState.Dead || !HasCheckpoint || Revives <= 0) return;
            Revives--;
            Player.RestoreFullHealth();
            Rigidbody body = Player.GetComponent<Rigidbody>();
            body.position = Checkpoint; body.linearVelocity = Vector3.zero;
            Player.GetComponent<PlayerMovement>().ClearInput();
            Player.GrantInvulnerability(2+GameConfig.Current.Stat(4)*.25f+AdventureUpgrades.Value(4));
            State = RunState.Playing; Time.timeScale = 1; HUD?.ShowState();
        }
        public void Complete()
        {
            if (!IsPlaying || Player == null || Player.health <= 0) return;
            if (BossAlive) { HUD?.Toast("Besiege zuerst den Wächter!"); return; }
            FinalScore = (ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0) +
                500 + Player.health * 50 + Mathf.Max(0,600 - Mathf.FloorToInt(Elapsed * 4));
            CompletionBonus = IsCustom ? 0 : ProgressStore.CompleteLevel(SelectedLevel,FinalScore,Elapsed,GameConfig.Current.TotalLevels,CollectedCoins,DefeatedEnemies);
            if(IsCustom)EditorTestSession.Complete(CustomLevel);
            State = RunState.Complete; Time.timeScale = 0;
            GameAudio.Instance?.Play("win");
            HUD?.ShowState();
        }
        public static void StartLevel(int id)
        {
            if (GameConfig.Current == null || id < 0 || id >= GameConfig.Current.TotalLevels ||
                !CampaignCatalog.Unlocked(id)) return;
            EditorTestSession.Clear();CustomLevel=null; SelectedLevel = id; Time.timeScale = 1;
            SceneManager.LoadScene("CampaignLevel");
        }
        public static void StartCustom(CommunityLevel level)
        {
            if(!LevelCodec.Validate(level,out _))return;
            CustomWeapon="sword";CustomLevel=LevelCodec.Decode(LevelCodec.Encode(level));SelectedLevel=0;Time.timeScale=1;SceneManager.LoadScene("CampaignLevel");
        }
        public void Retry()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("CampaignLevel");
        }
        public void Menu()
        {
            if(EditorTestSession.IsTest){EditorTestSession.ReturnToEditor=true;EditorTestSession.Clear();}
            else HomeScreen.ReturnToPlayerMenu=true;
            CustomLevel=null;
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }
        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1;
        }
    }
}
