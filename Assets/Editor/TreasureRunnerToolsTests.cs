#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerToolsTests
{
    static TreasureRunnerToolsTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.ToolsTests",false)){SessionState.SetBool("TR.ToolsTests",false);new GameObject("Tools checks").AddComponent<TreasureRunner.ToolsChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.ToolsTests",true);EditorApplication.EnterPlaymode();}
    public static void PrepareAndRun(){TreasureRunnerOpenArt.Prepare();Run();}
}
#endif
