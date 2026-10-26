#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerBackgroundUpdateTests
{
    static TreasureRunnerBackgroundUpdateTests()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.BackgroundUpdates",false))
            {SessionState.SetBool("TR.BackgroundUpdates",false);new GameObject("Background update checks").AddComponent<TreasureRunner.BackgroundUpdateChecks>();}
        };
    }
    public static void Run()
    {
        Directory.CreateDirectory("Logs/BackgroundUpdates");
        // Compile Android-only JNI/permission code, without building an APK or changing version settings.
        var settings=new ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android};
        var compilation=PlayerBuildInterface.CompilePlayerScripts(settings,"Logs/BackgroundUpdates/PlayerScripts");
        if(compilation.assemblies==null || compilation.assemblies.Count==0)throw new Exception("Android player scripts did not compile");
        File.WriteAllText("Logs/BackgroundUpdates/AndroidScriptCompilation.txt","Android C# player scripts compiled. No APK built.\n"+string.Join("\n",compilation.assemblies));
        ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool("TR.BackgroundUpdates",true);EditorApplication.EnterPlaymode();
    }
}
#endif
