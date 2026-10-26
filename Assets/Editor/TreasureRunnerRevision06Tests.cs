#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerRevision06Tests
{
    const string Pending="TR.Revision06.Tests";
    static TreasureRunnerRevision06Tests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Revision 0.6 tests").AddComponent<TreasureRunner.Revision06Checks>();}};}
    public static void Run(){Begin(false);}
    public static void RunFocused(){Begin(true);}
    static void Begin(bool focused){SessionState.SetBool("TR.Revision06.Focused",focused);TreasureRunnerBranding.Configure();ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
}
#endif
