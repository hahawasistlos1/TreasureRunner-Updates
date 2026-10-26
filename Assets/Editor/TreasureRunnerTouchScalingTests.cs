#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerTouchScalingTests
{
    static TreasureRunnerTouchScalingTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.TouchScaling",false)){SessionState.SetBool("TR.TouchScaling",false);new GameObject("Touch scaling checks").AddComponent<TreasureRunner.TouchScalingChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.TouchScaling",true);EditorApplication.EnterPlaymode();}
}
#endif
