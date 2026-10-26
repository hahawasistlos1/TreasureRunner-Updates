#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class TreasureRunnerWindowsBuild
{
    public static void Build()
    {
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Start Unity with -buildTarget Win64 so Windows renderer resources are included correctly.");
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        string output=TreasureRunner.ReleaseInfo.Output+"/Windows-Werkzeuge/Runtime/TreasureRunnerTools.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/CampaignLevel.unity"},target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None});
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)throw new Exception("Version was unexpectedly changed");
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Windows tools build failed: "+result.summary.result);
        File.WriteAllText("Logs/WindowsToolsBuild.txt","Succeeded\nVersion unchanged: "+version+" / "+code+"\nBytes: "+result.summary.totalSize+"\nErrors: "+result.summary.totalErrors);
    }
}
#endif
