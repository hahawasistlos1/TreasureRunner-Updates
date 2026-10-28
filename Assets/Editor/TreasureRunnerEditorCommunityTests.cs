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
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TreasureRunner;
[InitializeOnLoad] public static class TreasureRunnerEditorCommunityTests
{
    static TreasureRunnerEditorCommunityTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.EditorCommunity",false)){SessionState.SetBool("TR.EditorCommunity",false);new GameObject("Editor/community checks").AddComponent<EditorCommunityChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.EditorCommunity",true);EditorApplication.EnterPlaymode();}
}
public class EditorCommunityChecks:MonoBehaviour
{
    readonly List<string> report=new List<string>();bool failed;Touchscreen simulatedTouch;InputSettings savedInput,testInput;
    void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS: "+message);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){failed=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/Release10/Editor");
        savedInput=InputSystem.settings;testInput=Instantiate(savedInput);testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=testInput;
        string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
        var tests=Scenarios();while(true){bool more;object next=null;try{more=tests.MoveNext();if(more)next=tests.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return next;}
        if(simulatedTouch!=null)InputSystem.RemoveDevice(simulatedTouch);InputSystem.settings=savedInput;Destroy(testInput);Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
        if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real saves changed");}else report.Add("PASS: Real save and backup unchanged");
        report.Add(failed ? "EDITOR COMMUNITY CHECKS FAILED" : "ALL EDITOR COMMUNITY CHECKS PASSED");File.WriteAllLines("Logs/Release10/Editor/Checks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
    }
    IEnumerator Scenarios()
    {
        TreasureRunnerBranding.Configure();ProgressStore.Data.highestUnlockedLevel=79;
        LevelRun.StartLevel(CampaignCatalog.Id(1,1));yield return null;yield return null;yield return null;
        var run=LevelRun.Instance;Check(run!=null && run.Player!=null,"Actual Ice world stage 2 loads");
        var room=FindFirstObjectByType<SecretRoom>();Check(room!=null,"Ice stage 2 contains its real secret chamber");
        var player=run.Player;player.GrantInvulnerability(1000);player.GetComponent<PlayerMovement>().ControlLocked=true;var body=player.GetComponent<Rigidbody>();body.useGravity=false;
        var door=room.GetComponentsInChildren<SecretDoorTrigger>().First(t=>t.Action==1).GetComponent<Collider>();
        room.Interact(0,player);
        for(int cycle=0;cycle<3;cycle++)
        {
            room.Interact(1,player);yield return new WaitForFixedUpdate();Check(room.IsInside && body.position.x< -100,"Secret chamber entry works, cycle "+cycle);
            player.GetComponent<PlayerMovement>().Face(cycle%2==0 ? -1 : 1);
            room.Interact(3,player);Vector3 outside=body.position;
            Check(Mathf.Sign(outside.x-door.bounds.center.x)==player.GetComponent<PlayerMovement>().Facing,"Exit places player in walking direction away from entrance, cycle "+cycle);
            for(int callback=0;callback<8;callback++){room.Interact(1,player);room.Interact(3,player);}
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Check(!room.IsInside && body.position.x>0 && !door.bounds.Intersects(player.GetComponent<Collider>().bounds),"Exit stays outside despite repeated/stale callbacks, cycle "+cycle);
            yield return new WaitForSeconds(.65f);
            Check(!room.IsInside && Vector3.Distance(body.position,outside)<.5f,"No automatic return into chamber after exit, cycle "+cycle);
        }
        room.Interact(1,player);yield return new WaitForFixedUpdate();Check(room.IsInside,"Door re-arms for a deliberate later visit without taking treasure");
        room.Interact(2,player);room.Interact(3,player);yield return new WaitForFixedUpdate();
        Check(room.Claimed && room.DoorRetired && !door.enabled && !door.gameObject.activeInHierarchy,"Taking treasure and leaving removes door and entrance trigger for the run");
        room.Interact(1,player);Check(!room.IsInside,"Retired door ignores stale or repeated entry callbacks");
        for(int world=0;world<8;world++)
        {
            var parent=new GameObject("Secret-door world fixture "+world);var sample=SecretRoom.Build(parent.transform,new RouteSection{x=1000+world*20,width=11,height=0},world,CampaignCatalog.Id(world,1));
            sample.Interact(0,player);sample.Interact(1,player);sample.Interact(2,player);sample.Interact(3,player);
            Check(sample.DoorRetired && !sample.IsInside && sample.GetComponentsInChildren<SecretDoorTrigger>().All(t=>t.Action!=1),"Treasure exit retires entrance in world "+world);Destroy(parent);yield return null;
        }
        run.Retry();yield return null;yield return null;yield return null;
        Check(FindFirstObjectByType<SecretRoom>() is SecretRoom restarted && !restarted.DoorRetired && !restarted.Claimed && restarted.GetComponentsInChildren<SecretDoorTrigger>().Any(t=>t.Action==1),"Starting a new run restores the secret door despite the permanent treasure badge");
        LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
        LevelEditorScreen.Working=LevelCodec.Starter();LevelEditorScreen.Open();yield return null;yield return null;Canvas.ForceUpdateCanvases();
        var workspace=FindFirstObjectByType<LevelEditorScreen>();var workspaceView=FindFirstObjectByType<EditorLevelScene>();
        Check(LevelEditorScreen.Working.tiles.Count==0 && workspaceView.VisibleTileCount==0 && LevelEditorScreen.Working.width==384 && LevelEditorScreen.Working.height==32,"Normal entry always opens an empty 24-section world without auto-loading a saved draft");
        Check(workspaceView.Geometry.Find("Startfigur (Vorschau)")==null,"Empty world has no preplaced player or gameplay geometry");
        var full=(RectTransform)workspaceView.transform;
        Check(full.rect.height*full.lossyScale.y>Screen.height*.9f && full.rect.width*full.lossyScale.x>Screen.width*.9f,"Build world fills the usable screen");
        Check(full.GetComponentInParent<Canvas>().GetComponentsInChildren<Button>().Length==2,"Only the gear and functions buttons cover the build view");Capture("01-Leere-Bauwelt");
        workspaceView.PanMode=true;GameObject.Find("Bauteile").GetComponent<Button>().onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
        Check(BuilderPalette.Categories.All(category=>FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>()?.text==category)),"Gear opens all world-independent asset categories");Capture("02-Bauteilmenue");
        var pick=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<RawImage>()?.name=="Bauteilvorschau");pick.onClick.Invoke();yield return null;
        Check(!workspaceView.PanMode,"Selecting an asset always restores build input after hand mode");
        for(int state=0;state<2;state++)
        {
            workspaceView.SetSimulation(state==1);Vector2 point=ScreenPoint(workspaceView,new Vector2Int(5+state,3));
            var touch=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};workspaceView.OnPointerDown(touch);workspaceView.OnPointerUp(touch);yield return null;
            Check(LevelEditorScreen.Working.tiles.Any(t=>t.x==5+state && t.y==3),"Pointer places real blocks with simulation "+(state==1 ? "enabled" : "disabled"));
        }
        BuilderTestNavigation.Button("Radierer").onClick.Invoke();yield return null;
        var erase=new PointerEventData(EventSystem.current){position=ScreenPoint(workspaceView,new Vector2Int(6,3))};workspaceView.OnPointerDown(erase);workspaceView.OnPointerUp(erase);yield return null;
        Check(!LevelEditorScreen.Working.tiles.Any(t=>t.x==6 && t.y==3),"Erasing remains available while simulation is running");
        workspace.LoadDocument(LevelCodec.Empty());workspaceView.PanMode=false;
        GameObject.Find("Bauteile").GetComponent<Button>().onClick.Invoke();yield return null;FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<RawImage>()?.name=="Bauteilvorschau").onClick.Invoke();yield return null;
        float zoomZ=workspaceView.SceneCamera.transform.position.z;workspaceView.PanBy(new Vector2(0,20));yield return null;
        var highPoint=ScreenPoint(workspaceView,new Vector2Int(6,27));var highTouch=new PointerEventData(EventSystem.current){position=highPoint};
        Check(workspaceView.CellAt(highPoint,null,out var highCell) && highCell.y==27,"Camera movement reaches high build rows without zooming out");workspaceView.OnPointerDown(highTouch);workspaceView.OnPointerUp(highTouch);yield return null;
        Check(LevelEditorScreen.Working.tiles.Any(t=>t.x==6 && t.y==27) && workspaceView.SceneCamera.transform.position.z==zoomZ,"High block is placed at the intended cell with unchanged zoom");Capture("03-Oben-Bauen");
        var cornerPoint=ViewportPoint(workspaceView,new Vector2(.6f,.98f));var edgeTouch=new PointerEventData(EventSystem.current){position=cornerPoint};float beforePan=workspaceView.ViewCenter.y;
        workspaceView.OnPointerDown(edgeTouch);yield return new WaitForSeconds(.45f);workspaceView.OnPointerUp(edgeTouch);
        Check(workspaceView.ViewCenter.y>beforePan+.5f && LevelEditorScreen.Working.tiles.All(t=>t.y<32),"Holding the build pointer near the upper edge scrolls upward within world bounds");
        workspace.LoadDocument(LevelCodec.Empty());workspaceView.ResetView();yield return null;
        simulatedTouch=InputSystem.AddDevice<Touchscreen>();Vector2 mid=new Vector2(Screen.width*.55f,Screen.height*.5f),shift=Vector2.down*(Screen.height*.18f);float centerBefore=workspaceView.ViewCenter.y;
        SendTouch(1,UnityEngine.InputSystem.TouchPhase.Began,mid+Vector2.left*60);SendTouch(2,UnityEngine.InputSystem.TouchPhase.Began,mid+Vector2.right*60);yield return null;yield return null;
        SendTouch(1,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.left*60);SendTouch(2,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.right*60);yield return null;yield return null;
        Check(workspaceView.ViewCenter.y>centerBefore+.5f && LevelEditorScreen.Working.tiles.Count==0,"Real Input System two-finger drag pans vertically without painting; center="+centerBefore+"→"+workspaceView.ViewCenter+"; tiles="+LevelEditorScreen.Working.tiles.Count+"; touches="+simulatedTouch.touches.Count(t=>t.press.isPressed)+"; enabled="+simulatedTouch.enabled);
        float cameraBefore=workspaceView.SceneCamera.transform.position.z;SendTouch(1,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.left*110);SendTouch(2,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.right*110);yield return null;yield return null;
        Check(workspaceView.SceneCamera.transform.position.z>cameraBefore && LevelEditorScreen.Working.tiles.Count==0,"Two-finger pinch changes zoom without adding blocks");
        SendTouch(2,UnityEngine.InputSystem.TouchPhase.Ended,mid+shift+Vector2.right*110);yield return null;yield return new WaitForSeconds(.3f);
        SendTouch(1,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.left*130);yield return null;yield return null;
        Check(LevelEditorScreen.Working.tiles.Count==0,"Remaining finger after a two-finger gesture cannot accidentally paint");
        SendTouch(1,UnityEngine.InputSystem.TouchPhase.Ended,mid+shift+Vector2.left*130);yield return null;yield return new WaitForSeconds(.2f);
        Check(LevelEditorScreen.Working.tiles.Count==0,"Lifting fingers after a gesture does not create phantom blocks");InputSystem.RemoveDevice(simulatedTouch);simulatedTouch=null;workspaceView.CancelInteraction();
        var tall=LevelCodec.Starter();tall.height=32;tall.format=5;tall.tiles.Add(new LevelBlock{x=5,y=31,kind=LevelTile.Ground});Check(LevelCodec.Validate(tall,out _),"New format supports tall worlds and uppermost build row");tall.format=4;Check(!LevelCodec.Validate(tall,out _),"Legacy level formats keep their previous height limit");
        BuilderTestNavigation.Button("Menü").onClick.Invoke();yield return null;yield return null;
        var longLevel=LevelCodec.Starter();longLevel.width=LevelCodec.MaxWidth;longLevel.format=5;longLevel.tiles.Clear();
        for(int x=0;x<longLevel.width;x++)longLevel.tiles.Add(new LevelBlock{x=x,y=0,kind=x==longLevel.width-1 ? LevelTile.Goal : LevelTile.Ground});
        longLevel.tiles.Add(new LevelBlock{x=0,y=1,kind=LevelTile.Ground});
        Check(LevelCodec.Validate(longLevel,out var why),"24-section level validates without old 96-column key collisions: "+why);
        var decoded=LevelCodec.Decode(LevelCodec.Encode(longLevel));Check(decoded.width==384 && decoded.tiles.Count==385,"Large level survives compressed sharing round-trip");
        Check(LevelModeration.CanPublish(decoded)==(ReleaseInfo.Version!="0.9.0"),"Expanded levels follow the released version contract");
        decoded.format=4;Check(!LevelCodec.Validate(decoded,out _),"Old format cannot silently accept incompatible expanded bounds");decoded.format=5;decoded.width=385;Check(!LevelCodec.Validate(decoded,out _),"Oversized level is rejected");
        for(int stars=1;stars<=5;stars++)Check(ReviewPolicy.Valid(stars,"Sterne: "+stars),"Positive and negative rating accepted: "+stars);
        Check(!ReviewPolicy.Valid(0,"") && !ReviewPolicy.Valid(6,"") && !ReviewPolicy.Valid(3,new string('a',401)) && !ReviewPolicy.Valid(3,"a\tbad") && ReviewPolicy.Valid(2,"Zu schwer!\nSchöne Welt."),"Review input bounds and safe multiline comments");
        Check(ReviewPolicy.Key("id")==ReviewPolicy.Key("id") && ReviewPolicy.Key("id")!=ReviewPolicy.Key("id2") && ReviewPolicy.Key("../path").Length==64,"Stable review namespace cannot inject collection paths");
        LevelEditorScreen.OpenDocument(longLevel);yield return null;yield return null;Canvas.ForceUpdateCanvases();
        var editor=FindFirstObjectByType<LevelEditorScreen>();var scene=FindFirstObjectByType<EditorLevelScene>();
        BuilderTestNavigation.Button("Level einrichten").onClick.Invoke();yield return null;Canvas.ForceUpdateCanvases();
        var lastSection=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Abschnitt 24");
        var corners=new Vector3[4];lastSection.GetComponent<RectTransform>().GetWorldCorners(corners);
        Check(corners.All(p=>p.x>=0 && p.x<=Screen.width && p.y>=0 && p.y<=Screen.height),"All 24 section selectors fit inside the settings dialog");
        lastSection.onClick.Invoke();yield return null;yield return null;
        Check(scene.Geometry.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Bauteil 383,0")),"Last of 24 sections shows its real level geometry");
        var live=LevelCodec.Starter();live.width=32;live.tiles.RemoveAll(t=>t.x>=32 || t.kind==LevelTile.Coin);live.tiles.First(t=>t.x==31).kind=LevelTile.Goal;
        foreach(var pair in new[]{(5,LevelTile.Enemy),(11,LevelTile.Shooter),(8,LevelTile.Crumble),(16,LevelTile.Cannon),(20,LevelTile.Flame),(4,LevelTile.Hopper)})live.tiles.First(t=>t.x==pair.Item1 && t.y==0).kind=pair.Item2;
        live.tiles.Add(new LevelBlock{x=12,y=2,kind=LevelTile.Lift});live.tiles.Add(new LevelBlock{x=14,y=5,kind=LevelTile.FallingRock});
        editor.LoadDocument(live);scene.SetSimulation(true);scene.Zoom(1.9f);yield return null;yield return null;yield return new WaitForFixedUpdate();
        var ride=scene.Geometry.GetComponentInChildren<MovingRide>();var patrols=scene.Geometry.GetComponentsInChildren<EnemyPatrol>();var traps=scene.Geometry.GetComponentsInChildren<WorldTrap>();var shooter=scene.Geometry.GetComponentInChildren<EnemyRangedAttack>();
        Check(ride!=null && patrols.Length>=3 && traps.Length==4 && shooter!=null,"Builder activates actual moving platform, enemy variants and four trap types");
        Vector3 rideStart=ride.transform.position,enemyStart=patrols[0].transform.position;string before=LevelCodec.Encode(LevelEditorScreen.Working);
        yield return new WaitForSeconds(3.3f);
        Check(Vector3.Distance(rideStart,ride.transform.position)>.1f,"Lift really moves while building");
        Check(Vector3.Distance(enemyStart,patrols[0].transform.position)>.05f || patrols.Any(p=>p.IsMoving),"Enemies patrol in the build view");
        Check(traps.All(t=>t.Activations>0),"Falling rock, flame, cannon and crumbling floor all trigger during building");
        Check(shooter.ShotsFired>0,"Ranged enemy actually fires preview projectiles");
        Check(scene.Geometry.GetComponentsInChildren<Enemy>(true).All(e=>!e.enabled) && scene.Geometry.GetComponentsInChildren<Collider>().All(c=>!c.isTrigger || !c.enabled),"Simulation cannot damage a player or collect rewards; active enemies="+string.Join(",",scene.Geometry.GetComponentsInChildren<Enemy>(true).Where(e=>e.enabled).Select(e=>e.name))+"; triggers="+string.Join(",",scene.Geometry.GetComponentsInChildren<Collider>().Where(c=>c.isTrigger && c.enabled).Select(c=>c.name)));
        Check(scene.Geometry.GetComponentsInChildren<Renderer>().All(r=>r.gameObject.layer==28 && Mathf.Abs(r.bounds.center.z-20000)<100),"Generated trap effects and shots remain in isolated rendered scene");
        Check(LevelCodec.Encode(LevelEditorScreen.Working)==before,"Simulation leaves serialized placements and document unchanged");
        scene.SetSimulation(false);yield return null;yield return null;Check(!scene.Geometry.GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Stopping simulation restores frozen exact-placement geometry");
        scene.SetSimulation(true);yield return null;yield return null;Check(scene.Geometry.GetComponentsInChildren<WorldTrap>().All(t=>t.Activations==0),"Restart resets trap state without changing level document");
        scene.ResetView();yield return null;yield return null;
        Capture("Editor-Lebendige-Bauansicht");
        var buttons=scene.GetComponentInParent<Canvas>().GetComponentsInChildren<Button>();
        Check(buttons.Length==2 && GameObject.Find("Bauteile")!=null && GameObject.Find("Funktionen")!=null,"Full-screen editor has exactly two floating menu buttons");
        CommunityReviewPanel.Open(FindFirstObjectByType<Canvas>().transform,longLevel);yield return null;yield return null;Canvas.ForceUpdateCanvases();
        var panel=FindFirstObjectByType<CommunityReviewPanel>();Check(panel!=null && panel.GetComponentInChildren<InputField>().characterLimit==400,"Public review UI offers bounded optional comment");
        Check(panel.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>()?.text.EndsWith(" ★")==true)==5,"Review UI offers all five star ratings");
        Capture("Community-Sterne-Kommentare");Destroy(panel.gameObject);
    }
    void SendTouch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point)=>InputSystem.QueueStateEvent(simulatedTouch,new TouchState{touchId=id,phase=phase,position=point});
    static Vector2 ScreenPoint(EditorLevelScene scene,Vector2Int cell)
    {
        var uv=scene.SceneCamera.WorldToViewportPoint(new Vector3(cell.x,cell.y-.25f,20000));return ViewportPoint(scene,new Vector2(uv.x,uv.y));
    }
    static Vector2 ViewportPoint(EditorLevelScene scene,Vector2 uv)
    {
        var r=(RectTransform)scene.transform;var local=new Vector3(Mathf.Lerp(r.rect.xMin,r.rect.xMax,uv.x),Mathf.Lerp(r.rect.yMin,r.rect.yMax,uv.y),0);return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));
    }
    void Capture(string name)
    {
        var camera=new GameObject("Test screenshot camera").AddComponent<Camera>();camera.cullingMask=~(1<<28);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
        Canvas.ForceUpdateCanvases();
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Logs/Release10/Editor/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;
        for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}camera.targetTexture=null;target.Release();Destroy(target);Destroy(image);Destroy(camera.gameObject);
    }
}
#endif
