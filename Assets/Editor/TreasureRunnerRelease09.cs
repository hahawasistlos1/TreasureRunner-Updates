#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using TreasureRunner;
public static class TreasureRunnerRelease09
{
    public static void Configure(){TreasureRunnerRelease08.Configure();}
    public static void Android(){Configure();TreasureRunnerCampaignSetup.BuildAndroid();}
    public static void WindowsEditor()=>Windows(false);
    public static void WindowsAdmin()=>Windows(true);
    static void Windows(bool admin)
    {
        Configure();
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Start with -buildTarget Win64");
        string folder=admin ? "Builds/Private-0.9.0/Verwaltung" : ReleaseInfo.Output+"/Windows-Editor";
        string output=folder+"/Runtime/TreasureRunnerTools.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/CampaignLevel.unity"},target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None,extraScriptingDefines=admin ? new[]{"TREASURERUNNER_ADMIN"} : new string[0]});
        File.WriteAllText("Logs/Release09/Windows-"+(admin ? "admin" : "editor")+"-Build.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize);
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed");
    }
}
#endif
