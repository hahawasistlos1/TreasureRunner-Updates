#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerControlChoiceTests
{
    static TreasureRunnerControlChoiceTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.ControlChoice",false)){SessionState.SetBool("TR.ControlChoice",false);new GameObject("Control choice checks").AddComponent<TreasureRunner.ControlChoiceChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.ControlChoice",true);EditorApplication.EnterPlaymode();}
}
#endif
