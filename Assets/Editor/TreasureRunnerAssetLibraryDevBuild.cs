#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using TreasureRunner;
public static class TreasureRunnerAssetLibraryDevBuild
{
    const string Folder="Builds/AssetExpansion-Dev";
    public static void Editor()=>Build(false);
    public static void Admin()=>Build(true);
    public static void ArchiveGenerated()
    {
        const string source="Assets/Resources/AssetLibrary/",archive="Assets/Art/LibraryGeneratedArchive/";
        var catalog=AssetLibrary.Data;var roots=catalog.entries.Select(e=>"Assets/Resources/"+e.resource+".prefab")
            .Concat(catalog.images.Select(i=>AssetDatabase.GetAssetPath(Resources.Load<Texture2D>(i.resource))))
            .Concat(catalog.audio.Select(a=>AssetDatabase.GetAssetPath(Resources.Load<AudioClip>(a.resource)))).ToArray();
        if(roots.Any(p=>string.IsNullOrEmpty(p) || !File.Exists(p)))throw new Exception("A catalog resource is missing; archive cancelled");
        var used=new HashSet<string>(AssetDatabase.GetDependencies(roots,true),StringComparer.Ordinal);
        var files=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith(source) && !AssetDatabase.IsValidFolder(p) && !used.Contains(p) && new[]{".prefab",".mat",".png",".ogg",".wav",".mp3",".asset"}.Contains(Path.GetExtension(p))).ToArray();
        var report=new List<string>();
        foreach(string directory in files.Select(p=>Path.GetDirectoryName(archive+p.Substring(source.Length))).Distinct())Directory.CreateDirectory(directory);
        AssetDatabase.Refresh();
        foreach(string path in files)
        {
            string target=archive+path.Substring(source.Length);string absolute=Path.GetFullPath(target);string permitted=Path.GetFullPath(archive);
            if(!absolute.StartsWith(permitted,StringComparison.OrdinalIgnoreCase))throw new Exception("Archive escaped project folder");
            if(File.Exists(target))throw new Exception("Archive target already exists: "+target);
            string error=AssetDatabase.MoveAsset(path,target);if(!string.IsNullOrEmpty(error))throw new Exception(error);report.Add(path+" -> "+target);
        }
        File.WriteAllLines("Logs/AssetExpansion/Generated-Archive.txt",report);AssetDatabase.SaveAssets();
    }
    static void Build(bool admin)
    {
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Start with -buildTarget Win64");
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        string kind=admin ? "Verwaltung" : "Welteneditor",output=Folder+"/"+kind+"/Runtime/TreasureRunnerTools.exe";Directory.CreateDirectory(Path.GetDirectoryName(output));
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/CampaignLevel.unity"},target=BuildTarget.StandaloneWindows64,locationPathName=output,options=BuildOptions.None,extraScriptingDefines=admin ? new[]{"TREASURERUNNER_ADMIN"} : Array.Empty<string>()});
        File.WriteAllText("Logs/AssetExpansion/Windows-"+kind+"-Build.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize+"\nDevelopment only. App version unchanged: "+version+" / "+code);
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)throw new Exception("App version changed");
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Development Windows build failed");
    }
}
#endif
