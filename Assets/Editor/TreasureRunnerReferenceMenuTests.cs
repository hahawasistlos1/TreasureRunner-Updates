#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad] public static class TreasureRunnerReferenceMenuTests
{
    static TreasureRunnerReferenceMenuTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.ReferenceMenu",false)){SessionState.SetBool("TR.ReferenceMenu",false);if(SessionState.GetBool("TR.ReferenceRegression",false)){SessionState.SetBool("TR.ReferenceRegression",false);TreasureRunner.ToolsChecks.ReportPath="Logs/ReferenceMenuRegression.txt";TreasureRunner.ToolsChecks.PreviewDirectory="Builds/ReferenceMenuRegression";new GameObject("Reference menu regression").AddComponent<TreasureRunner.ToolsChecks>();}else new GameObject("Reference menu checks").AddComponent<TreasureRunner.ReferenceMenuChecks>();}};}
    public static void Run()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/MenuArt/Reference"})){
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.isReadable=false;importer.maxTextureSize=2048;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        }
        ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.ReferenceMenu",true);EditorApplication.EnterPlaymode();
    }
    public static void Regression(){SessionState.SetBool("TR.ReferenceRegression",true);Run();}
}
#endif
