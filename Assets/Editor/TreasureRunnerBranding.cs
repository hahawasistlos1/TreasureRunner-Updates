using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.IO;
public static class TreasureRunnerBranding
{
    public static void Configure()
    {
        const string path="Assets/Resources/Brand/TreasureRunnerIcon-v2.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;
        importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.SaveAndReimport();
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var sizes=PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone,IconKind.Any);
        var icons=new Texture2D[sizes.Length];for(int i=0;i<icons.Length;i++)icons[i]=icon;
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone,icons,IconKind.Any);
#if UNITY_ANDROID
        foreach(var kind in new[]{UnityEditor.Android.AndroidPlatformIconKind.Legacy,UnityEditor.Android.AndroidPlatformIconKind.Round})
        {
            var slots=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);foreach(var slot in slots)slot.SetTexture(icon);
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,slots);
        }
#endif
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Logs");File.WriteAllText("Logs/Branding-Current.txt","Generated comic chest and crystal icon configured locally. Source: "+path+". No APK or release created.");
    }
}
