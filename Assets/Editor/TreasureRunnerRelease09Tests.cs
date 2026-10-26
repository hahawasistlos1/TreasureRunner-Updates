#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerRelease09Tests
{
    static TreasureRunnerRelease09Tests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.Release09",false)){SessionState.SetBool("TR.Release09",false);new GameObject("Release09 checks").AddComponent<TreasureRunner.Release09Checks>();}};}
    public static void Run(){TreasureRunnerRelease09.Configure();ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.Release09",true);EditorApplication.EnterPlaymode();}
}
#endif
