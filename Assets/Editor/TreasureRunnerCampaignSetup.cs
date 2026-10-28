using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using TreasureRunner;

public static class TreasureRunnerCampaignSetup
{
    [MenuItem("TreasureRunner/Kampagne und Hauptmenü einrichten")]
    public static void Setup()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/Resources");
        Directory.CreateDirectory("Assets/Art/TreasureRunner");
        AssetDatabase.Refresh();
        var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/TreasureRunnerConfig.asset");
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.upgrades = new[]
            {
                Upgrade("Leben","Ein zusätzliches Leben je Stufe. Drei Herzen pro Leben.",100,1),
                Upgrade("Tempo","Mehr Bewegungsgeschwindigkeit.",85,.15f),
                Upgrade("Sprungkraft","Höhere Sprünge ohne Mehrfachsprung.",85,.12f),
                Upgrade("Angriff","Mehr Nahkampfschaden alle zwei Stufen. Fernkampf separat verbessern.",120,1),
                Upgrade("Verteidigung","Mehr Schutzzeit nach einem Treffer.",140,1)
            };
            config.weapons = new[]
            {
                Weapon("sword","Abenteuerschwert","Ausgewogenes Nahkampfschwert.",0,0,1,.55f,1.7f,false,new Color(.8f,.88f,.9f)),
                Weapon("axe","Waldläuferaxt","Langsam, aber kräftig.",180,1,3,.9f,1.65f,false,new Color(.9f,.65f,.3f)),
                Weapon("bow","Jägerbogen","Schnelle Pfeile auf Entfernung.",250,2,1,.6f,10,true,new Color(.87f,.67f,.35f)),
                Weapon("staff","Runenstab","Gezielte Elementkugeln. Langsam und gegen manche Schilde wirksam.",500,5,2,1.25f,10,true,new Color(.48f,.88f,1)),
                Weapon("spear","Schatzsucherspeer","Goblin-Waffe: längere sichtbare Spitze, mittlere Kraft.",900,0,2,.78f,2.05f,false,new Color(.4f,1,.55f)),
                Weapon("hammer","Knochenhammer","Ritter-Waffe: langsamer schwerer Schlag, bricht Panzerung.",1400,0,4,1.25f,1.7f,false,new Color(.92f,.88f,.72f)),
                Weapon("crossbow","Späherarmbrust","Späher-Waffe: schnelle sichtbare Bolzen, geringer Einzelschaden.",2100,0,1,.82f,10,true,new Color(1,.7f,.2f)),
                Weapon("froststaff","Kristallfroststab","Magier-Waffe: Frostkugeln bremsen Gegner kurz, geringer Schaden.",2800,0,1,1.35f,10,true,new Color(.25f,.85f,1))
            };
            AssetDatabase.CreateAsset(config,"Assets/Resources/TreasureRunnerConfig.asset");
        }
        config.goblinVisual = Load("Assets/Standout7/LOWPO_Goblin_Pack/Prefabs/PFB_Basic_Goblin.prefab");
        config.characterVisual = Load("Assets/Standout7/LOWPO_Goblin_Pack/Prefabs/PFB_Goblin_Warrior.prefab");
        config.monsterVisual = ConvertVisual("Assets/RPGMonsterBuddiesPBRPA/Prefab/CactusPA.prefab");
        config.swordVisual = Load("Assets/Standout7/LOWPO_Goblin_Pack/Prefabs/PFB_Goblin_Weapon_Notched_Shortsword.prefab");
        config.bowVisual = Load("Assets/Standout7/LOWPO_Goblin_Pack/Prefabs/PFB_Goblin_Weapon_Recurve_Shortbow.prefab");
        config.trees = new[] {
            Load("Assets/AngeloMaN87/Natural Environment (Mobile)/Prefabs/Tree01.prefab"),
            Load("Assets/AngeloMaN87/Natural Environment (Mobile)/Prefabs/Tree03.prefab") };
        config.rock = Load("Assets/AngeloMaN87/Natural Environment (Mobile)/Prefabs/Rock01A.prefab");
        config.grass = Load("Assets/AngeloMaN87/Natural Environment (Mobile)/Prefabs/Grass01.prefab");
        config.groundMaterial = EnsureMaterial("Ground",new Color(.2f,.48f,.3f));
        config.coinMaterial = EnsureMaterial("Gold",GameUI.Gold);
        EditorUtility.SetDirty(config);
        CreateScene("MainMenu",true);
        CreateScene("CampaignLevel",false);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",true),
            new EditorBuildSettingsScene("Assets/Scenes/CampaignLevel.unity",true),
            new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity",true)
        };
        // Preserve the existing Windows/Editor PlayerPrefs namespace from the source project.
        PlayerSettings.companyName = "DefaultCompany";
        PlayerSettings.productName = "TreasureRunner";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.treasurerunner.game");
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.bundleVersion=TreasureRunner.ReleaseInfo.Version;
            PlayerSettings.Android.bundleVersionCode=TreasureRunner.ReleaseInfo.AndroidCode;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("CAMPAIGN_SETUP_OK");
    }

    private static UpgradeDefinition Upgrade(string title,string description,int price,float step) =>
        new UpgradeDefinition { title = title,description = description,basePrice = price,increase = step,maxLevel = 5 };
    private static WeaponDefinition Weapon(string id,string name,string description,int price,int level,int damage,
        float cooldown,float range,bool ranged,Color color) => new WeaponDefinition
        { id = id,title = name,description = description,price = price,requiredLevel = level,damage = damage,
          cooldown = cooldown,range = range,ranged = ranged,color = color };
    private static GameObject Load(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new InvalidOperationException("Asset fehlt: " + path);
        return asset;
    }
    private static Material EnsureMaterial(string name,Color color)
    {
        string path = "Assets/Art/TreasureRunner/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        material.SetColor("_BaseColor",color); AssetDatabase.CreateAsset(material,path);
        return material;
    }
    private static GameObject ConvertVisual(string path)
    {
        string target = "Assets/Art/TreasureRunner/CactusVisual.prefab";
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(Load(path));
        var map = new System.Collections.Generic.Dictionary<Material,Material>();
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var original = materials[i];
                if (original == null || original.shader.name.StartsWith("Universal")) continue;
                if (!map.TryGetValue(original,out Material converted))
                {
                    string materialPath = "Assets/Art/TreasureRunner/" + original.name + "_URP.mat";
                    converted = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (converted == null)
                    {
                        converted = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                        if (original.HasProperty("_MainTex")) converted.SetTexture("_BaseMap",original.GetTexture("_MainTex"));
                        if (original.HasProperty("_Color")) converted.SetColor("_BaseColor",original.GetColor("_Color"));
                        AssetDatabase.CreateAsset(converted,materialPath);
                    }
                    map[original] = converted;
                }
                materials[i] = converted;
            }
            renderer.sharedMaterials = materials;
        }
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance,target);
        UnityEngine.Object.DestroyImmediate(instance);
        return saved;
    }
    private static void CreateScene(string name,bool menu)
    {
        string path = "Assets/Scenes/" + name + ".unity";
        if (File.Exists(path)) return; // Preserve user edits on repeated setup.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root = new GameObject("TreasureRunner");
        root.AddComponent<TreasureGameBootstrap>().mainMenu = menu;
        EditorSceneManager.SaveScene(scene,path);
    }

    [MenuItem("TreasureRunner/Android APK erstellen")]
    // User rule: build, version increment and publication each require an explicit request.
    public static void BuildAndroid()=>BuildAndroidInternal(TreasureRunner.ReleaseInfo.Version,TreasureRunner.ReleaseInfo.AndroidCode,TreasureRunner.ReleaseInfo.Output+"/TreasureRunner.apk");
    // Explicitly authorized local test build. Never use the immutable release output.
    public static void BuildLiveEditorTest()
    {
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        const string output="Builds/LiveEditor-Test/TreasureRunner-Test.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        BuildAndroidInternal(version,code,output);
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)
            throw new InvalidOperationException("Test build unexpectedly changed the app version.");
        File.WriteAllText("Logs/LiveEditor/TestApkBuild.txt","Local test APK succeeded.\nPath: "+output+"\nVersion unchanged: "+version+" / "+code+"\nNot published.");
    }
    public static void BuildBowFixTest()
    {
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        const string output="Builds/BowFix-Test/TreasureRunner-BowFix-Test.apk";
        Directory.CreateDirectory(Path.GetDirectoryName(output));Directory.CreateDirectory("Logs/WeaponPose");
        BuildAndroidInternal(version,code,output);
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)
            throw new InvalidOperationException("Bow fix test changed the app version.");
        File.WriteAllText("Logs/WeaponPose/TestApkBuild.txt","Local bow fix test APK succeeded.\nPath: "+output+"\nVersion unchanged: "+version+" / "+code+"\nNot published.");
    }
    public static void BuildEditorCommunityTest()
    {
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        const string output="Builds/EditorCommunity-Test/TreasureRunner-EditorCommunity-Test.apk";
        if(File.Exists(output))throw new InvalidOperationException("Existing test APK must be preserved.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));Directory.CreateDirectory("Logs/EditorCommunity/TestApk");
        BuildAndroidInternal(version,code,output);
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)
            throw new InvalidOperationException("Editor/community test changed the app version.");
        File.WriteAllText("Logs/EditorCommunity/TestApk/Build.txt","Local editor/community test APK succeeded.\nPath: "+output+"\nVersion unchanged: "+version+" / "+code+"\nNot published.");
    }
    public static void BuildWorkspaceR2Test()
    {
        string version=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        const string output="Builds/EditorWorkspace-R2-Test/TreasureRunner-Workspace-R2-Test.apk";
        if(File.Exists(output))throw new InvalidOperationException("Existing test APK must be preserved.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));Directory.CreateDirectory("Logs/EditorWorkspace-R2/TestApk");
        BuildAndroidInternal(version,code,output);
        if(PlayerSettings.bundleVersion!=version || PlayerSettings.Android.bundleVersionCode!=code)
            throw new InvalidOperationException("Workspace test changed the app version.");
        File.WriteAllText("Logs/EditorWorkspace-R2/TestApk/Build.txt","Local corrected workspace test APK succeeded.\nPath: "+output+"\nVersion unchanged: "+version+" / "+code+"\nNot published.");
    }
    public static void BuildUpdateTestClient()
    {
        string previous=PlayerSettings.bundleVersion;int code=PlayerSettings.Android.bundleVersionCode;
        try{BuildAndroidInternal("0.2.9",4,"Builds/UpdateTestClient.apk");}
        finally{PlayerSettings.bundleVersion=previous;PlayerSettings.Android.bundleVersionCode=code;AssetDatabase.SaveAssets();}
    }
    private static void BuildAndroidInternal(string version,int code,string output)
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        // The offline alpha does not require analytics or remote engine diagnostics.
        UnityEditor.EngineDiagnostics.EngineDiagnosticsSettings.enabled = false;
        UnityEditor.Analytics.AnalyticsSettings.enabled = false;
        UnityEditor.Analytics.AnalyticsSettings.initializeOnStartup = false;
        // URP must select Android's quality assets before shader variants are stripped.
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android &&
            !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))
            throw new InvalidOperationException("Android-Buildplattform konnte nicht aktiviert werden.");
        TreasureRunnerBranding.Configure();
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Builds"); Directory.CreateDirectory("Logs");
        Application.logMessageReceived += BuildLog;
        try
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.bundleVersion=version;
            PlayerSettings.Android.bundleVersionCode=code;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainMenu.unity","Assets/Scenes/CampaignLevel.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            File.WriteAllText(output.Contains("UpdateTestClient") ? "Logs/UpdateTestBuildResult.txt" : "Logs/AndroidBuildResult.txt",report.summary.result + "\nErrors: " +
                report.summary.totalErrors + "\nBytes: " + report.summary.totalSize + "\nTime: " + report.summary.totalTime);
            Debug.Log("ANDROID_BUILD_" + report.summary.result);
            if(report.summary.result!=BuildResult.Succeeded) throw new BuildFailedException("Android build failed: "+report.summary.totalErrors+" errors");
        }
        finally { Application.logMessageReceived -= BuildLog; }
    }
    private static void BuildLog(string message,string trace,LogType type)
    {
        File.AppendAllText("Logs/CampaignAndroidBuild.log",DateTime.UtcNow.ToString("o") + " " + type + ": " + message + "\n");
    }
}
