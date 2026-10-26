#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerIdentityTests
{
    static TreasureRunnerIdentityTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.Identity",false)){SessionState.SetBool("TR.Identity",false);new GameObject("Identity privacy checks").AddComponent<TreasureRunner.IdentityChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.Identity",true);EditorApplication.EnterPlaymode();}
    public static void CompileAndroidScripts()
    {
        var settings=new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android};
        var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings,"Logs/GoogleIdentity/PlayerScripts");
        if(result.assemblies==null || result.assemblies.Count==0)throw new System.Exception("Android scripts did not compile");
        System.IO.File.WriteAllText("Logs/GoogleIdentity/AndroidScripts.txt","Android C# player scripts compiled to DLLs only; no APK.\n"+string.Join("\n",result.assemblies));
    }
}
#endif
