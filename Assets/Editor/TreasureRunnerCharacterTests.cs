#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerCharacterTests
{
    static TreasureRunnerCharacterTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.CharacterTests",false)){SessionState.SetBool("TR.CharacterTests",false);new GameObject("Character checks").AddComponent<TreasureRunner.CharacterChoiceChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.CharacterTests",true);EditorApplication.EnterPlaymode();}
}
#endif
