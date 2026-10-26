#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerArsenalTests
{
    static TreasureRunnerArsenalTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.Arsenal",false)){SessionState.SetBool("TR.Arsenal",false);new GameObject("Arsenal progression checks").AddComponent<TreasureRunner.ArsenalChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.Arsenal",true);EditorApplication.EnterPlaymode();}
    public static void CompileAndroidScripts()
    {
        var settings=new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android};
        var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings,"Logs/Arsenal/PlayerScripts");
        if(result.assemblies==null || result.assemblies.Count==0)throw new System.Exception("Android player scripts did not compile");
        System.IO.File.WriteAllText("Logs/Arsenal/AndroidScriptCompilation.txt","Android player C# scripts compiled to DLLs only. No APK built.\n"+string.Join("\n",result.assemblies));
    }
}
#endif
