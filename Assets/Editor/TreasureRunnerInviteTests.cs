#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerInviteTests
{
    static TreasureRunnerInviteTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.InviteTests",false)){SessionState.SetBool("TR.InviteTests",false);if(SessionState.GetBool("TR.InviteRegression",false)){SessionState.SetBool("TR.InviteRegression",false);TreasureRunner.ToolsChecks.ReportPath="Logs/InviteRegression.txt";TreasureRunner.ToolsChecks.PreviewDirectory="Builds/InviteRegression";new GameObject("Invitation regression").AddComponent<TreasureRunner.ToolsChecks>();}else new GameObject("Invitation checks").AddComponent<TreasureRunner.InviteChecks>();}};}
    public static void Regression(){SessionState.SetBool("TR.InviteRegression",true);Run();}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.InviteTests",true);EditorApplication.EnterPlaymode();}
}
#endif
