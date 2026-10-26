#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerAutomaticPatchTests
{
    static TreasureRunnerAutomaticPatchTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.AutomaticPatch",false)){SessionState.SetBool("TR.AutomaticPatch",false);new GameObject("Automatic patch checks").AddComponent<TreasureRunner.AutomaticPatchChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.AutomaticPatch",true);EditorApplication.EnterPlaymode();}
    public static void CompileAndroidScripts()
    {
        var settings=new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android};
        var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings,"Logs/AutomaticPatchFlow/PlayerScripts");
        if(result.assemblies==null || result.assemblies.Count==0)throw new System.Exception("Android scripts did not compile");
        System.IO.File.WriteAllText("Logs/AutomaticPatchFlow/AndroidScriptCompilation.txt","Android player C# compiled to DLLs only; no APK built.\n"+string.Join("\n",result.assemblies));
    }
}
#endif
