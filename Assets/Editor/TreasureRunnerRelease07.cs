#if UNITY_EDITOR
using UnityEditor;
public static class TreasureRunnerRelease07
{
    public static void Configure(){PlayerSettings.bundleVersion="0.7.0";PlayerSettings.Android.bundleVersionCode=8;AssetDatabase.SaveAssets();}
    public static void Check(){Configure();TreasureRunnerToolsTests.Run();}
}
#endif
