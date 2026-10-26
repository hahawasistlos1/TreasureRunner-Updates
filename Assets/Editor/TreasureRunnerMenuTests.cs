#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class TreasureRunnerMenuTests
{
    static TreasureRunnerMenuTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.MenuFantasy",false)){SessionState.SetBool("TR.MenuFantasy",false);if(SessionState.GetBool("TR.MenuRegression",false)){SessionState.SetBool("TR.MenuRegression",false);TreasureRunner.ToolsChecks.ReportPath="Logs/FantasyMenuRegression.txt";TreasureRunner.ToolsChecks.PreviewDirectory="Builds/FantasyMenuRegression";new GameObject("Fantasy menu regression").AddComponent<TreasureRunner.ToolsChecks>();}else new GameObject("Fantasy menu checks").AddComponent<TreasureRunner.FantasyMenuChecks>();}};}
    public static void Regression(){SessionState.SetBool("TR.MenuRegression",true);Run();}
    public static void Run(){ConfigureArt();ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.MenuFantasy",true);EditorApplication.EnterPlaymode();}
    static void ConfigureArt(){foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/MenuArt"})){var path=AssetDatabase.GUIDToAssetPath(guid);var t=(TextureImporter)AssetImporter.GetAtPath(path);t.textureType=TextureImporterType.Default;t.mipmapEnabled=false;t.maxTextureSize=2048;t.isReadable=false;t.textureCompression=TextureImporterCompression.Compressed;t.SaveAndReimport();}}
}
#endif
