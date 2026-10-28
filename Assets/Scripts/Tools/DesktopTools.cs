using System;
using UnityEngine;
namespace TreasureRunner
{
    public static class DesktopTools
    {
        public static string Mode { get; private set; }
        public static bool Active=>!string.IsNullOrEmpty(Mode);
        public static bool Moderator {
            get {
#if TREASURERUNNER_ADMIN || UNITY_EDITOR
                return Mode=="admin";
#else
                return false;
#endif
            }
        }
        public static bool AdminUnavailable { get; private set; }
        public static bool Smoke=>Array.IndexOf(Environment.GetCommandLineArgs(),"--tools-smoke")>=0;
        public static string DataPath=>Smoke ? System.IO.Path.Combine(Environment.CurrentDirectory,"Pruefung","TempData",Mode??"game") : Application.persistentDataPath;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            var args=Environment.GetCommandLineArgs();
#if TREASURERUNNER_ADMIN
            Mode=Array.IndexOf(args,"--level-editor")>=0 ? "editor" : "admin";
#else
            Mode="editor";AdminUnavailable=Array.IndexOf(args,"--level-admin")>=0;
#endif
            if(Smoke)ProgressStore.BeginMemoryTest();
            Screen.SetResolution(1440,900,FullScreenMode.Windowed);Application.runInBackground=true;
#endif
        }
        public static void Build(Transform host)
        {
            if(AdminUnavailable){var canvas=GameUI.Canvas("Nur Welteneditor");GameUI.Text("Diese Ausgabe enthält ausschließlich den Welteneditor. Die Verwaltung wird nicht öffentlich verteilt.",canvas,28);GameUI.Button("Beenden",canvas,()=>Application.Quit());return;}
#if TREASURERUNNER_ADMIN || UNITY_EDITOR
            if(Moderator){new GameObject("Levelverwaltung").AddComponent<LevelModerationScreen>().Build();return;}
#endif
            LevelEditorScreen.Open();
        }
        public static void OpenFolder()
        {
            string path=System.IO.Path.Combine(DataPath,"my-levels");
            System.IO.Directory.CreateDirectory(path);Application.OpenURL(new Uri(path+System.IO.Path.DirectorySeparatorChar).AbsoluteUri);
        }
    }
}
