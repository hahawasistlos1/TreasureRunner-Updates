#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerPatchTests
{
    static TreasureRunnerPatchTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.DataPatchTests",false)){SessionState.SetBool("TR.DataPatchTests",false);new GameObject("Data patch tests").AddComponent<TreasureRunner.DataPatchChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.DataPatchTests",true);EditorApplication.EnterPlaymode();}
}
#endif
