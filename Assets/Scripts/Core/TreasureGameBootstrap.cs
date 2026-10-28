using UnityEngine;
namespace TreasureRunner
{
    public class TreasureGameBootstrap : MonoBehaviour
    {
        public bool mainMenu;
        private void Awake()
        {
            if (GameConfig.Current == null) { Debug.LogError("TreasureRunnerConfig fehlt. Projektaufbau im Editor ausführen."); return; }
            Time.timeScale = 1;
            InviteReceiver.Ensure();
            PatchRuntime.Initialize();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameGraphics.Apply(); GameAudio.Ensure();
            if (mainMenu)
            {
                if(PatchClient.Instance==null)new GameObject("Small Data Patches").AddComponent<PatchClient>();
                var camera = new GameObject("Menu Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
                camera.tag="MainCamera";
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = GameUI.Ink;
                camera.cullingMask &= ~(1 << 30);
                GameAudio.Instance.PlayTheme(-1);
                if(DesktopTools.Active){HomeScreen.ReturnToPlayerMenu=false;bool returning=EditorTestSession.ReturnToEditor;EditorTestSession.ReturnToEditor=false;if(returning && !DesktopTools.Moderator)LevelEditorScreen.OpenDocument(LevelEditorScreen.Working);else DesktopTools.Build(transform);}
                else if(HomeScreen.ReturnToPlayerMenu || EditorTestSession.ReturnToEditor)
                {
                    HomeScreen.ReturnToPlayerMenu=false;gameObject.AddComponent<MainMenuController>().Build();
                    if(EditorTestSession.ReturnToEditor){EditorTestSession.ReturnToEditor=false;LevelEditorScreen.OpenDocument(LevelEditorScreen.Working);}
                }
                else gameObject.AddComponent<HomeScreen>().Build();
            }
            else
            {
                GameAudio.Instance.PlayTheme(LevelRun.IsCustom ? AdventureMusic.Seed(LevelRun.CustomLevel.id)%80 : LevelRun.SelectedLevel);
                var run = gameObject.AddComponent<LevelRun>();
                gameObject.AddComponent<LevelFactory>().Build(run);
                gameObject.AddComponent<CampaignHUD>().Build(run);
            }
        }
    }
}
