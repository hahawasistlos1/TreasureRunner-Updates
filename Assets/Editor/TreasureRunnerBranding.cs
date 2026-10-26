using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
public static class TreasureRunnerBranding
{
    public static void Configure()
    {
        const string path="Assets/Resources/Brand/TreasureRunnerIcon.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
        importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#if UNITY_ANDROID
        foreach(var kind in new[]{UnityEditor.Android.AndroidPlatformIconKind.Legacy,UnityEditor.Android.AndroidPlatformIconKind.Round})
        {
            var slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);foreach(var slot in slots)slot.SetTexture(icon);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,slots);
        }
#endif
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Branding06.txt","Original comic chest icon configured for Android legacy and round launcher slots. Adaptive launcher wrapping uses Android fallback. Source: Assets/Resources/Brand/TreasureRunnerIcon.png");
    }
}
