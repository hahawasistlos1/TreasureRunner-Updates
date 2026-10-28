#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TreasureRunner;
[InitializeOnLoad] public static class TreasureRunnerWeaponPoseTests
{
    static TreasureRunnerWeaponPoseTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.WeaponPose",false)){SessionState.SetBool("TR.WeaponPose",false);new GameObject("Weapon pose checks").AddComponent<WeaponPoseChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.WeaponPose",true);EditorApplication.EnterPlaymode();}
}
public class WeaponPoseChecks:MonoBehaviour
{
    readonly List<string> report=new List<string>();bool failed;
    void Check(bool value,string message){if(!value){failed=true;report.Add("FAIL: "+message);}else report.Add("PASS: "+message);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){failed=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/WeaponPose");Directory.CreateDirectory("Builds/WeaponPose-Preview");
        string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
        var tests=Scenarios();while(true){bool more;object next=null;try{more=tests.MoveNext();if(more)next=tests.Current;}catch(Exception e){failed=true;report.Add("EXCEPTION: "+e);break;}if(!more)break;yield return next;}
        Application.logMessageReceived-=Log;EditorTestSession.Clear();ProgressStore.EndMemoryTest();Time.timeScale=1;
        Check(saved==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real save and backup remain unchanged");
        report.Add(failed ? "WEAPON POSE CHECKS FAILED" : "ALL WEAPON POSE CHECKS PASSED");File.WriteAllLines("Logs/WeaponPose/Checks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
    }
    static Bounds Bounds(WeaponModel model)
    {
        // Measure actual vertices independently of the fitting helper's cached mesh bounds.
        bool found=false;var bounds=new Bounds();
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices)
        {var point=filter.transform.TransformPoint(vertex);if(!found){bounds=new Bounds(point,Vector3.zero);found=true;}else bounds.Encapsulate(point);}
        if(!found)throw new Exception("Weapon has no measurable vertices");return bounds;
    }
    IEnumerator Scenarios()
    {
        // Start with the same figure and equip order as the handset reproduction.
        var choices=CharacterCatalog.All.OrderBy(c=>c.id=="skeleton-mage" ? 0 : 1).ToArray();
        foreach(var choice in choices)
        {
            CharacterCatalog.Select(choice.id);var level=LevelCodec.Starter();level.tiles.RemoveAll(t=>t.kind==LevelTile.Coin);LevelRun.StartCustom(level);yield return null;yield return null;yield return null;
            var player=LevelRun.Instance.Player;var move=player.GetComponent<PlayerMovement>();var body=player.GetComponent<Rigidbody>();var presentation=player.GetComponent<CharacterPresentation>();player.GrantInvulnerability(999);
            foreach(string id in new[]{"sword","axe","bow","staff","spear","hammer","crossbow","froststaff","bow"})
            {
                LevelRun.CustomWeapon=id;presentation.RefreshWeapon();yield return null;yield return null;
                var model=player.GetComponentsInChildren<WeaponModel>().Single(m=>m.gameObject.activeInHierarchy);var hand=CharacterRig.Bone(presentation.visual,HumanBodyBones.RightHand);
                report.Add("INFO: "+choice.id+"/"+id+" parent="+model.transform.parent.name+" handScale="+hand.lossyScale.ToString("F4")+" weaponScale="+model.transform.lossyScale.ToString("F4")+" bounds="+Bounds(model).size.ToString("F4"));
                foreach(int facing in new[]{1,-1,1,-1})
                {
                    body.position=new Vector3(6,1.05f,0);body.linearVelocity=Vector3.zero;move.ClearInput();move.SetTouchDirection(facing,true);
                    for(int frame=0;frame<5;frame++){yield return new WaitForFixedUpdate();yield return null;}
                    yield return null;var bounds=Bounds(model);float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                    Check(move.Facing==facing && body.linearVelocity.x*facing>0,"Requested direction is really walking and facing: "+choice.id+"/"+id+" "+facing);
                    Check(longest>.35f && longest<1.6f,"Weapon stays proportional while walking/turning: "+choice.id+"/"+id+" facing "+facing+" size "+bounds.size.ToString("F3"));
                    Check(Vector3.Distance(model.transform.position,hand.position)<.01f,"Weapon grip follows actual hand: "+choice.id+"/"+id+" facing "+facing);
                    if(choice.id=="skeleton-mage" && id=="bow")Capture(Camera.main,"Mage-Bow-"+facing);
                }
                move.ClearInput();body.linearVelocity=Vector3.zero;
                player.GetComponent<PlayerCombat>().Attack();yield return new WaitForSeconds(.15f);yield return null;var aiming=Bounds(model);
                Check(Mathf.Max(aiming.size.x,Mathf.Max(aiming.size.y,aiming.size.z))<1.6f,"Attack pose keeps physical weapon size: "+choice.id+"/"+id);
                yield return new WaitForSeconds(.9f);
            }
            // Campaign upgrades select the second imported bow; custom play always uses rank0.
            LevelRun.CustomLevel=null;ProgressStore.Data.equippedWeapon="bow";
            ProgressStore.Data.weaponUpgrades=new int[ArsenalProgression.WeaponCount];
            for(int rank=0;rank<=WeaponUpgrades.Cap;rank++)
            {
                ProgressStore.Data.weaponUpgrades[WeaponUpgrades.Index("bow")]=rank;presentation.RefreshWeapon();yield return null;yield return null;
                var model=player.GetComponentsInChildren<WeaponModel>().Single(m=>m.gameObject.activeInHierarchy);
                foreach(int facing in new[]{1,-1})
                {
                    move.ClearInput();move.SetTouchDirection(facing,true);for(int frame=0;frame<4;frame++){yield return new WaitForFixedUpdate();yield return null;}
                    yield return null;var bounds=Bounds(model);float longest=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
                    Check(move.Facing==facing && longest>.35f && longest<1.6f,"Upgraded bow stays proportional: "+choice.id+" rank "+rank+" direction "+facing+" size "+bounds.size.ToString("F3"));
                }
            }
            move.ClearInput();LevelRun.CustomLevel=level;
        }
    }
    void Capture(Camera camera,string name)
    {
        var follow=camera.GetComponent<CameraFollow>();follow.enabled=false;var player=LevelRun.Instance.Player;camera.transform.position=player.transform.position+new Vector3(1,1,-6);camera.transform.LookAt(player.transform.position+Vector3.up*.2f);
        var target=new RenderTexture(1000,700,24);var oldTarget=camera.targetTexture;camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1000,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,700),0,0);image.Apply();File.WriteAllBytes("Builds/WeaponPose-Preview/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=oldTarget;target.Release();Destroy(target);Destroy(image);
    }
}
#endif
