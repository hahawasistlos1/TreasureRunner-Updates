#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerHeartTests
{
    const string Pending="TR.HeartTests.Pending";
    static TreasureRunnerHeartTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);if(SessionState.GetBool("TR.HeartTests.NextDev",false)){SessionState.SetBool("TR.HeartTests.NextDev",false);TreasureRunner.HeartChecks.ReportPath="Logs/NextDevHeartTests.txt";TreasureRunner.HeartChecks.PreviewDirectory="Builds/NextDevHeartPreview";}new GameObject("Heart tests").AddComponent<TreasureRunner.HeartChecks>();}};}
    public static void NextDevelopment(){SessionState.SetBool("TR.HeartTests.NextDev",true);Run();}
    [MenuItem("TreasureRunner/Herzen und Leben testen")]
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
}
#endif
