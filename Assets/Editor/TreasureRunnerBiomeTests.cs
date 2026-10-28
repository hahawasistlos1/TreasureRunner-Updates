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

[InitializeOnLoad] public static class TreasureRunnerBiomeTests
{
    static TreasureRunnerBiomeTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.Biomes",false)){SessionState.SetBool("TR.Biomes",false);new GameObject("Biome checks").AddComponent<BiomeChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.Biomes",true);EditorApplication.EnterPlaymode();}
}
public class BiomeChecks:MonoBehaviour
{
    readonly List<string> report=new List<string>();bool failed;
    void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS: "+message);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){failed=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/Biomes");Directory.CreateDirectory("Builds/Biome-Preview");
        string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
        var tests=Scenarios();while(true){bool more;object value=null;try{more=tests.MoveNext();if(more)value=tests.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return value;}
        Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
        if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real player preferences changed");}else report.Add("PASS: Real save and backup unchanged");
        report.Add(failed ? "BIOME CHECKS FAILED" : "ALL BIOME CHECKS PASSED");File.WriteAllLines("Logs/Biomes/Checks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
    }
    IEnumerator Scenarios()
    {
        ProgressStore.Data.highestUnlockedLevel=23;for(int id=0;id<80;id++)ProgressStore.Data.levels.Add(new LevelRecord{id=id,completions=1});
        var signatures=new HashSet<string>();
        for(int world=0;world<8;world++)
        {
            var ids=new List<string>();for(int zone=0;zone<4;zone++)
            {
                var landmark=LibraryWorldArt.Landmark(world,0,zone);var detail=LibraryWorldArt.Detail(world,0,zone);
                Check(landmark!=null && detail!=null,"Authored landmark and detail exist: world "+world+", variant "+zone);ids.Add(landmark.id);
            }
            Check(signatures.Add(string.Join("|",ids)),"World has a distinct collection of main scenery: "+world);
            Check(LibraryWorldArt.Floor(world)!=null,"Authored ground material exists: "+world);
            Check(LibraryWorldArt.Sky(world,0)!=LibraryWorldArt.Sky(world,5),"Later stages vary lighting within world "+world);
            for(int index=0;index<6;index++)foreach(bool ranged in new[]{false,true})Check(CampaignEnemyArt.Select(world,index,ranged).Model!=null,"Complete enemy model available: "+world+"/"+index+"/"+ranged);
            foreach(int stage in new[]{0,5})
            {
                LevelRun.StartLevel(CampaignCatalog.Id(world,stage));yield return null;yield return null;yield return null;var run=LevelRun.Instance;
                Check(LevelRun.SelectedLevel==CampaignCatalog.Id(world,stage),"Requested campaign stage loaded: "+world+"/"+stage);
                run.Player.GrantInvulnerability(999);run.Player.GetComponent<PlayerMovement>().ControlLocked=true;run.Player.GetComponent<Rigidbody>().useGravity=false;
                var props=FindObjectsByType<CampaignSceneryMarker>(FindObjectsSortMode.None);Check(props.Length>=4,"Imported composed scenery is instantiated: "+world+"/"+stage);
                foreach(var prop in props)
                {
                    Check(!prop.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Background has no active colliders: "+prop.assetId);
                    foreach(var renderer in prop.GetComponentsInChildren<Renderer>())
                    {
                        Check(AssetBlockSurface.GeometryBounds(renderer).min.z>=2.28f,"Scenery stays behind the player lane: "+prop.assetId);
                        for(int slot=0;slot<renderer.sharedMaterials.Length;slot++){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot);var material=renderer.sharedMaterials[slot];Check(block.GetColor("_BaseColor")==material.GetColor("_BaseColor")*LibraryWorldArt.Tint(world),"Per-material source colors retained: "+prop.assetId);}
                    }
                }
                var equipped=FindObjectsByType<LibraryEnemyEquipment>(FindObjectsSortMode.None);Check(equipped.Length>0,"Campaign uses actual equipped asset enemies: "+world);
                foreach(var npc in equipped)
                {
                    Check(npc.world==world && npc.GetComponent<BoxCollider>().size.y==1.5f,"Readable enemy hitbox belongs to its world: "+npc.skinId);
                    Check(npc.visual.GetComponentInChildren<WeaponModel>()!=null,"Actual weapon attached to enemy rig: "+npc.skinId+"/"+npc.weaponId);
                }
                if(world==1 && stage==0)
                {
                    var npc=equipped[0];var renderer=npc.visual.GetComponentInChildren<Renderer>();var before=new MaterialPropertyBlock();renderer.GetPropertyBlock(before,0);npc.GetComponent<Enemy>().TakeDamage(1);yield return null;
                    var flash=new MaterialPropertyBlock();renderer.GetPropertyBlock(flash,0);Check(flash.GetColor("_BaseColor")==Color.white*2,"Tinted enemy shows hit flash");yield return new WaitForSeconds(.2f);
                    renderer.GetPropertyBlock(flash,0);Check(flash.GetColor("_BaseColor")==before.GetColor("_BaseColor"),"Hit flash restores original per-material tint");
                }
                foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){enemy.GetComponent<EnemyPatrol>().enabled=false;if(enemy.GetComponent<EnemyAwareness>()!=null)enemy.GetComponent<EnemyAwareness>().enabled=false;if(enemy.GetComponent<EnemyRangedAttack>()!=null)enemy.GetComponent<EnemyRangedAttack>().enabled=false;}
                var body=run.Player.GetComponent<Rigidbody>();body.interpolation=RigidbodyInterpolation.None;body.position=new Vector3(2,1.05f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return null;
                var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(5,3.3f,-12);camera.transform.rotation=Quaternion.Euler(8,0,0);Capture("Welt-"+world+"-Abschnitt-"+stage,camera);
            }
        }
        // One full enemy attack in an isolated part of the actual campaign scene.
        LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
        var game=LevelRun.Instance;var shooter=FindObjectsByType<LibraryEnemyEquipment>(FindObjectsSortMode.None).First(e=>e.weaponId=="bow" && e.GetComponent<EnemyRangedAttack>()!=null);
        foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){enemy.GetComponent<EnemyPatrol>().enabled=false;enemy.GetComponent<EnemyAwareness>().enabled=false;if(enemy.gameObject!=shooter.gameObject)enemy.gameObject.SetActive(false);}
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Isolated attack floor";floor.transform.position=new Vector3(1002,-.5f,0);floor.transform.localScale=new Vector3(30,1,3);
        shooter.transform.position=new Vector3(1000,.75f,0);var player=game.Player;var playerBody=player.GetComponent<Rigidbody>();playerBody.interpolation=RigidbodyInterpolation.None;playerBody.position=new Vector3(1004,1.05f,0);playerBody.linearVelocity=Vector3.zero;player.GetComponent<PlayerMovement>().ControlLocked=true;player.RestoreFullHealth();Physics.SyncTransforms();
        var attack=shooter.GetComponent<EnemyRangedAttack>();attack.windup=.25f;attack.cooldown=1;bool warning=false,arrow=false;float deadline=Time.time+5;
        while(Time.time<deadline && player.HeartUnits==6){warning|=attack.IsPreparing;arrow|=FindObjectsByType<EnemyBolt>(FindObjectsSortMode.None).Any(b=>b.IsArrow);yield return null;}
        Check(warning && arrow && attack.ShotsFired>0,"Archer telegraphs and fires a visible asset arrow");Check(player.HeartUnits==5,"Real arrow hit removes exactly half a heart");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(1002,1,0);wall.transform.localScale=new Vector3(.4f,2,2);Physics.SyncTransforms();yield return new WaitForSeconds(1);
        int shots=attack.ShotsFired,hearts=player.HeartUnits;yield return new WaitForSeconds(1.5f);Check(attack.ShotsFired==shots && player.HeartUnits==hearts,"Solid cover blocks enemy shots and damage");
        // Reusing the bounded projectile pool changes graphics without retaining arrows on magic shots.
        attack.enabled=false;EnemyBolt.Fire(new Vector3(1001,5,0),Vector3.right,1,true);EnemyBolt.Fire(new Vector3(1001,6,0),Vector3.right,1,false);yield return null;
        Check(FindObjectsByType<EnemyBolt>(FindObjectsSortMode.None).Any(b=>b.IsArrow) && FindObjectsByType<EnemyBolt>(FindObjectsSortMode.None).Any(b=>!b.IsArrow),"Arrow and magic graphics coexist in the scene pool");
        var custom=LevelCodec.Starter();custom.format=2;custom.tiles.RemoveAll(t=>t.x==10 && t.y==0);custom.tiles.Add(new LevelBlock{x=10,y=0,kind=LevelTile.Shooter,asset="art.Skeleton_Mage"});Check(LevelCodec.Validate(custom,out var reason),"Legacy fixture is valid: "+reason);LevelRun.StartCustom(custom);yield return null;yield return null;yield return null;
        var skeleton=FindFirstObjectByType<SkeletonMotion>();Check(skeleton!=null && skeleton.GetComponentInChildren<Animation>().enabled,"Existing custom-level skeleton keeps its legacy animation enabled");
        Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App version unchanged");
    }
    void Capture(string name,Camera camera)
    {
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var target=new RenderTexture(1600,900,24);var previous=camera.targetTexture;camera.targetTexture=target;
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes("Builds/Biome-Preview/"+name+".png",texture.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=previous;target.Release();Destroy(target);Destroy(texture);
        for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
    }
}
#endif
