#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerRelease10Tests
{
    static TreasureRunnerRelease10Tests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.Release10",false)){SessionState.SetBool("TR.Release10",false);new GameObject("Release10 checks").AddComponent<TreasureRunner.Release10Checks>();}};}
    public static void Run(){TreasureRunnerRelease10.Configure();ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.Release10",true);EditorApplication.EnterPlaymode();}
}
#endif
