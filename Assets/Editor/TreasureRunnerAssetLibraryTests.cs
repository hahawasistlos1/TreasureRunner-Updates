#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TreasureRunner;

[InitializeOnLoad] public static class TreasureRunnerAssetLibraryTests
{
    static TreasureRunnerAssetLibraryTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.AssetLibrary.Tests",false)){SessionState.SetBool("TR.AssetLibrary.Tests",false);new GameObject("Asset expansion checks").AddComponent<AssetLibraryChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.AssetLibrary.Tests",true);EditorApplication.EnterPlaymode();}
    public static void Diagnose()
    {
        Directory.CreateDirectory("Logs/AssetExpansion");var lines=new List<string>();
        var diagnosticModels=CharacterCatalog.All.Select(c=>(c.id,c.Model)).Concat(new[]{"FBX_SCALE_UNITS","FBX_SCALE_ALL"}.Select(n=>(n,AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/FreeLibrary/kay-adventurers/Diagnostics/"+n+".fbx"))));
        foreach(var pair in diagnosticModels)
        {
            var parent=new GameObject("Diagnostic parent");var visual=LevelFactory.AddVisual(pair.Item2,parent.transform,1.8f,-1);if(visual==null)continue;
            foreach(var animation in visual.GetComponentsInChildren<Animation>())animation.enabled=false;
            lines.Add(pair.Item1+" scale="+visual.lossyScale+" pos="+visual.position);
            foreach(var r in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh=new Mesh();r.BakeMesh(mesh);lines.Add(r.name+" enabled="+r.enabled+" bounds="+r.bounds+" local="+r.localBounds+" bakedSize="+mesh.bounds.size.ToString("F6")+" scale="+r.transform.lossyScale+" vertices="+mesh.vertexCount);UnityEngine.Object.DestroyImmediate(mesh);
            }
            UnityEngine.Object.DestroyImmediate(parent);
        }
        File.WriteAllLines("Logs/AssetExpansion/Figure-Diagnostics.txt",lines);
    }
    public static void AndroidScripts()
    {
        Directory.CreateDirectory("Logs/AssetExpansion/AndroidScripts");
        var settings=new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android,options=UnityEditor.Build.Player.ScriptCompilationOptions.None};
        var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings,"Logs/AssetExpansion/AndroidScripts");
        if(result.assemblies==null || result.assemblies.Count==0)throw new Exception("Android script compilation failed");
        File.WriteAllText("Logs/AssetExpansion/AndroidScripts.txt","Android C# compiled successfully; no APK built.");
    }
}
public class AssetLibraryChecks:MonoBehaviour
{
    readonly List<string> report=new List<string>();bool errors;
    const string Shots="Builds/AssetExpansion-Preview";
    void Check(bool value,string text){if(!value)throw new Exception(text);report.Add("PASS: "+text);}
    void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory(Shots);Directory.CreateDirectory("Logs/AssetExpansion");
        string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
        var checks=Scenarios();bool failed=false;
        while(true){bool more;object current=null;try{more=checks.MoveNext();if(more)current=checks.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
        Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
        if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Actual player preferences changed");}
        else report.Add("PASS: Real save and backup unchanged");
        failed|=errors;report.Add(failed ? "ASSET LIBRARY CHECKS FAILED" : "ALL ASSET LIBRARY CHECKS PASSED");File.WriteAllLines("Logs/AssetExpansion/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
    }
    IEnumerator Scenarios()
    {
        ProgressStore.Data.highestUnlockedLevel=23;
        var data=AssetLibrary.Data;Check(data.packs.Length>=20,"All acquired package editions indexed");Check(data.characters.Length==6,"Six complete adventurer variants, no accessory mistaken for a character");
        Check(data.entries.Select(e=>e.id).Distinct().Count()==data.entries.Length,"Unique stable identifiers for every imported world asset");
        foreach(var pack in data.packs)Check(Resources.Load<TextAsset>(pack.licenseResource)!=null,"Bundled complete license: "+pack.id);
        foreach(var entry in data.entries)
        {
            Check(File.Exists("Assets/Resources/"+entry.resource+".prefab"),"Prepared resource: "+entry.id);
            var definition=BuilderPalette.Get(entry.id);Check(definition!=null && BuilderPalette.Categories.Contains(definition.category),"World-independent palette entry: "+entry.id);
            var level=LevelCodec.Starter();level.format=4;level.tiles.Add(new LevelBlock{x=5,y=4,kind=definition.kind,asset=definition.id});
            Check(LevelCodec.Validate(level,out var reason),"Valid level round trip source: "+entry.id+" "+reason);
            Check(LevelCodec.Decode(LevelCodec.Encode(level)).tiles.Any(t=>t.asset==entry.id),"Level code preserves asset identity: "+entry.id);
        }
        var development=LevelCodec.Starter();development.format=4;Check(!LevelModeration.CanPublish(development),"Unreleased format cannot enter public 0.9 catalog");
        Check(LevelModeration.CanPublish(LevelCodec.Starter()),"Old valid levels retain owner approval without a playthrough requirement");
        foreach(var pack in data.packs)
        {
            var entry=data.entries.FirstOrDefault(e=>e.pack==pack.id);if(entry==null)continue;
            var texture=EditorTilePreview.Get(new LevelBlock{kind=(LevelTile)entry.kind,asset=entry.id},0);
            Check(texture.GetPixels32().Count(p=>p.a>10)>20,"Actual visible prefab thumbnail: "+pack.id);File.WriteAllBytes(Shots+"/Bauteil-"+pack.id+".png",texture.EncodeToPNG());
        }
        foreach(string key in new[]{"click","hit","jump","step"})Check(LibraryAudioBank.Effect(key)!=null,"Real imported audio used for "+key);
        foreach(var character in CharacterCatalog.All)
        {
            Check(CharacterCatalog.Select(character.id),"Selectable complete figure: "+character.id);Check(CharacterSkills.Index(character.id)>=0,"Character class upgrade mapping: "+character.id);
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;Freeze(run);var visual=run.Player.GetComponentInChildren<CharacterVisualId>();
            Check(visual!=null && visual.id==character.id,"Gameplay uses selected figure: "+character.id);
            foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand})
                Check(CharacterRig.Bone(visual.transform,bone)!=null,"Usable rig bone "+bone+": "+character.id);
            var capsule=run.Player.GetComponent<CapsuleCollider>();Check(capsule.height==2 && capsule.radius==.5f && run.Player.HeartUnits==6,"Character preserves hitbox/hearts: "+character.id);
            var bodies=visual.GetComponentsInChildren<Renderer>().Where(r=>r.GetComponentInParent<WeaponModel>()==null).ToArray();Bounds bodyBounds=AssetBlockSurface.GeometryBounds(bodies[0]);foreach(var skin in bodies.Skip(1))bodyBounds.Encapsulate(AssetBlockSurface.GeometryBounds(skin));
            Camera.main.transform.position=run.Player.transform.position+new Vector3(1,1.7f,-6);Camera.main.transform.LookAt(run.Player.transform.position);Capture("Figur-"+character.id,Camera.main);
            report.Add("GEOMETRY: "+character.id+" "+bodyBounds+" visualScale="+visual.transform.lossyScale);
            if(bodyBounds.size.y<=1.4f || bodyBounds.size.y>=2.3f){errors=true;report.Add("FAIL: Full-sized gameplay geometry "+character.id+" height="+bodyBounds.size.y);}
            else Check(true,"Actual skinned geometry remains full-sized in gameplay: "+character.id);
        }
        Check(CharacterSkills.Index("unknown")==-1,"Unknown character cannot purchase another class skill");
        foreach(var entry in data.entries.Where(e=>e.role==(int)BuilderRole.Block && (e.kind==(int)LevelTile.Lift || e.pack=="kenney-platformer2d" || e.title.ToLowerInvariant().Contains("stairs") || e.title.ToLowerInvariant().Contains("ramp"))))
        {
            var root=new GameObject("Solid block check");root.AddComponent<LevelFactory>().BuildPreview(new LevelBlock{kind=(LevelTile)entry.kind,asset=entry.id},0);foreach(var ride in root.GetComponentsInChildren<MovingRide>())ride.enabled=false;root.transform.position=new Vector3(0,0,1000);yield return null;
            var collider=root.GetComponentInChildren<MeshCollider>();Check(collider!=null && collider.enabled && !collider.isTrigger,"Real solid collision: "+entry.id);
            Physics.SyncTransforms();bool supported=false;for(int x=0;x<5;x++)if(collider.Raycast(new Ray(new Vector3(-.4f+x*.2f,3,1000),Vector3.down),out var hit,5)){supported=true;Check(hit.point.y<=.02f && hit.point.y>=-.67f,"Collider follows fitted tile surface: "+entry.id);}
            Check(supported,"Tile supports the playable lane: "+entry.id);Destroy(root);yield return null;
        }
        foreach(var id in new[]{"sword","axe","bow","staff","spear","hammer","crossbow","froststaff"})
        {
            var holder=new GameObject("Weapon presentation test");var model=WeaponModel.Build(holder.transform,id);
            if(id=="sword" || id=="axe")Check(model.GetComponentInChildren<LibraryWeaponSource>()!=null,"Equipped "+id+" uses imported model");
            if(id=="sword" || id=="axe" || id=="spear" || id=="hammer")Check(model.bladeBase!=null && model.bladeTip!=null && Vector3.Distance(model.bladeBase.position,model.bladeTip.position)<1.5f,"Visible weapon keeps bounded blade contact markers: "+id);
            Destroy(holder);
        }
        for(int world=0;world<8;world++)
        {
            LevelRun.StartLevel(CampaignCatalog.Id(world,0));yield return null;yield return null;yield return null;var run=LevelRun.Instance;Freeze(run);
            Check(LevelRun.SelectedLevel==CampaignCatalog.Id(world,0),"Actual requested world opened: "+world);
            Check(LibraryWorldArt.Scenery(world,0)!=null,"Installed landscape assets used in world "+world);
            var route=FindFirstObjectByType<LevelFactory>().Route;var section=route[Mathf.Min(4,route.Count-1)];run.Player.GetComponent<Rigidbody>().position=new Vector3(section.Center,section.height+1.1f,0);
            Camera.main.transform.position=run.Player.transform.position+new Vector3(2,2.7f,-12);Camera.main.transform.rotation=Quaternion.Euler(8,0,0);yield return null;Capture("Welt-"+world,Camera.main);
        }
        LevelRun.Instance.Menu();yield return null;yield return null;LevelEditorScreen.OpenDocument(LevelCodec.Starter());yield return null;Capture("Editor",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));
        LibraryMediaBrowser.Open();yield return null;Canvas.ForceUpdateCanvases();
        var title=FindObjectsByType<Text>(FindObjectsSortMode.None).First(t=>t.text.StartsWith("ASSETBIBLIOTHEK"));Check(title.rectTransform.rect.width>800,"Library header spans the modal instead of collapsing");
        Check(FindObjectsByType<RawImage>(FindObjectsSortMode.None).Where(i=>i.transform.parent.name=="Vorschau").All(i=>Mathf.Abs(i.rectTransform.rect.width/Mathf.Max(1,i.rectTransform.rect.height)-1)<.05f),"Model thumbnails retain their square aspect ratio");
        Capture("Bibliothek",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));
        FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Bilder & Menüdesign").onClick.Invoke();yield return null;Capture("Bilder",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));
        FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Audio").onClick.Invoke();yield return null;Capture("Audio",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));
        Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Existing app version unchanged");
    }
    void Freeze(LevelRun run)
    {
        run.Player.GetComponent<PlayerMovement>().ControlLocked=true;run.Player.GetComponent<Rigidbody>().useGravity=false;run.Player.GrantInvulnerability(999);
        foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
        foreach(var trap in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))trap.enabled=false;
        Camera.main.GetComponent<CameraFollow>().enabled=false;
    }
    void Capture(string name,Camera camera)
    {
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(1600,900,24);var before=camera.targetTexture;camera.targetTexture=rt;
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(Shots+"/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=active;camera.targetTexture=before;rt.Release();Destroy(rt);Destroy(image);for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
    }
}
#endif
