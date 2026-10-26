#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerGridTests
{
    static TreasureRunnerGridTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.GridTests",false)){SessionState.SetBool("TR.GridTests",false);new GameObject("Filled editor grid checks").AddComponent<TreasureRunner.EditorGridChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.GridTests",true);EditorApplication.EnterPlaymode();}
}
#endif
