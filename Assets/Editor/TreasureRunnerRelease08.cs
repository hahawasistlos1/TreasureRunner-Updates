#if UNITY_EDITOR
using UnityEditor;
using TreasureRunner;
public static class TreasureRunnerRelease08
{
    public static void Configure(){PlayerSettings.bundleVersion=ReleaseInfo.Version;PlayerSettings.Android.bundleVersionCode=ReleaseInfo.AndroidCode;const string path="Assets/Resources/ShieldRing.mat";if(AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path)==null){var shader=UnityEngine.Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new System.InvalidOperationException("Unlit shader missing");var material=new UnityEngine.Material(shader);material.SetColor("_BaseColor",new UnityEngine.Color(.3f,.95f,1));AssetDatabase.CreateAsset(material,path);}AssetDatabase.SaveAssets();}
    public static void Android(){Configure();TreasureRunnerCampaignSetup.BuildAndroid();}
    public static void Windows(){Configure();TreasureRunnerWindowsBuild.Build();}
}
#endif
