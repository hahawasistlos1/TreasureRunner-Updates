#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerPlatformCollisionTests
{
    static TreasureRunnerPlatformCollisionTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.PlatformCollision",false)){SessionState.SetBool("TR.PlatformCollision",false);new GameObject("Platform collision checks").AddComponent<TreasureRunner.PlatformCollisionChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.PlatformCollision",true);EditorApplication.EnterPlaymode();}
}
#endif
