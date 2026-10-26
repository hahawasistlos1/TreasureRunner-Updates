#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerNextDevTests
{
    static TreasureRunnerNextDevTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.NextDev",false)){SessionState.SetBool("TR.NextDev",false);if(SessionState.GetBool("TR.NextDev.Regression",false)){SessionState.SetBool("TR.NextDev.Regression",false);TreasureRunner.ToolsChecks.ReportPath="Logs/NextDevRegression.txt";TreasureRunner.ToolsChecks.PreviewDirectory="Builds/NextDevRegression";new GameObject("Next development regression").AddComponent<TreasureRunner.ToolsChecks>();}
        else new GameObject("Next development checks").AddComponent<TreasureRunner.NextDevChecks>();}};}
    public static void Regression(){SessionState.SetBool("TR.NextDev.Regression",true);Run();}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.NextDev",true);EditorApplication.EnterPlaymode();}
}
#endif
