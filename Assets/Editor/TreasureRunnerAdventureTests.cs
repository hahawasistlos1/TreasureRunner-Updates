using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TreasureRunner;
[InitializeOnLoad]
public static class TreasureRunnerAdventureTests
{
    const string Pending="TreasureRunner.AdventureTests.Pending";
    static TreasureRunnerAdventureTests(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Adventure test runner").AddComponent<AdventureSmokeTests>();}};}
    [MenuItem("TreasureRunner/Abenteuer-Update testen")]
    public static void Run(){TreasureRunnerRevision06Tests.Run();}
    public static void RunFocused(){TreasureRunnerRevision06Tests.RunFocused();}
    static void Begin(bool focused){SessionState.SetBool("TreasureRunner.Tests.Focused",focused);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
}
