#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TreasureRunner;
[InitializeOnLoad] public static class TreasureRunnerLiveEditorTests
{
    static TreasureRunnerLiveEditorTests(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.LiveEditor",false)){SessionState.SetBool("TR.LiveEditor",false);new GameObject("Live editor checks").AddComponent<LiveEditorChecks>();}};}
    public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.LiveEditor",true);EditorApplication.EnterPlaymode();}
}
public class LiveEditorChecks:MonoBehaviour
{
    readonly List<string> report=new List<string>();bool failed;
    void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS: "+message);}
    void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){failed=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
    Button Button(string text)=>BuilderTestNavigation.Button(text);
    IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/Release10/LiveEditor");Directory.CreateDirectory("Builds/LiveEditor-Preview");
        string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
        var tests=Scenarios();while(true){bool more;object next=null;try{more=tests.MoveNext();if(more)next=tests.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return next;}
        Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
        if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real saves changed");}else report.Add("PASS: Real save and backup unchanged");
        report.Add(failed ? "LIVE EDITOR CHECKS FAILED" : "ALL LIVE EDITOR CHECKS PASSED");File.WriteAllLines("Logs/Release10/LiveEditor/Checks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
    }
    IEnumerator Scenarios()
    {
        TreasureRunnerBranding.Configure();
        Check(LevelCodec.Empty().tiles.Count==0,"New builder document is empty without overwriting existing levels");
        string issue="https://github.com/hahawasistlos1/TreasureRunner-Updates/issues/12";
        Check(CommunityIssueImporter.IsReference("Mein Level:\n"+issue,out int id) && id==12,"Import accepts a GitHub link inside copied message text");
        Check(!CommunityIssueImporter.IsReference(issue.Replace("github.com","github.com.evil.example"),out _) && !CommunityIssueImporter.IsReference(issue.Replace("https://","https://user@"),out _) && !CommunityIssueImporter.IsReference(null,out _),"Importer still rejects foreign origins and invalid input");
        string invitation=FriendInvites.Create("0123456789abcdef0123456789abcdef","Mia");
        Check(FriendInvites.TryParse("Komm mit auf Schatzsuche!\n"+invitation,out _,out _) && !FriendInvites.TryParse(invitation.Replace(".io/",".io:444/"),out _,out _),"Shared invitations paste while origin checks remain enforced");
        LevelEditorScreen.OpenDocument(LevelCodec.Empty());yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return null;
        var editor=FindFirstObjectByType<LevelEditorScreen>();var scene=FindFirstObjectByType<EditorLevelScene>();
        scene.SetSimulation(false);
        Check(scene!=null && scene.SceneCamera!=null && scene.GetComponent<RawImage>().texture is RenderTexture,"Actual editor uses a live rendered scene rather than a tile image matrix");
        Check(scene.VisibleTileCount==0,"Empty document has no preplaced gameplay blocks");Capture("Leeres-Level");
        var cell=new Vector2Int(5,1);Vector2 pointer=ScreenPoint(scene,cell);
        Check(scene.CellAt(pointer,null,out var mapped) && mapped==cell,"Perspective pointer maps to correct world cell");
        var e=new PointerEventData(EventSystem.current){position=pointer,button=PointerEventData.InputButton.Left};scene.OnPointerDown(e);scene.OnPointerUp(e);yield return null;
        Check(LevelEditorScreen.Working.tiles.Any(t=>t.x==5 && t.y==1 && t.kind==LevelTile.Ground) && scene.VisibleTileCount==1,"Click places actual floor geometry in the scene");
        Button("Rückgängig").onClick.Invoke();yield return null;Check(LevelEditorScreen.Working.tiles.Count==0 && scene.VisibleTileCount==0,"Undo removes actual scene geometry");
        Button("Wiederholen").onClick.Invoke();yield return null;Check(LevelEditorScreen.Working.tiles.Count==1 && scene.VisibleTileCount==1,"Redo restores actual scene geometry");
        int count=LevelEditorScreen.Working.tiles.Count;scene.PanMode=true;scene.OnPointerDown(e);e.position+=new Vector2(90,0);scene.OnDrag(e);scene.OnPointerUp(e);scene.PanMode=false;
        Check(LevelEditorScreen.Working.tiles.Count==count,"Hand drag moves view without painting");scene.ResetView();scene.Zoom(.8f);
        pointer=ScreenPoint(scene,cell);Check(scene.CellAt(pointer,null,out mapped) && mapped==cell,"Zoom preserves placement coordinates");
        Button("Radierer").onClick.Invoke();e.position=pointer;scene.OnPointerDown(e);scene.OnPointerUp(e);yield return null;Check(LevelEditorScreen.Working.tiles.Count==0,"Eraser removes selected world cell");
        var example=LevelCodec.Starter();example.tiles.RemoveAll(t=>t.kind==LevelTile.Coin || (t.x>=9 && t.x<=11) || (t.x>=22 && t.x<=24));
        foreach(int x in new[]{5,7,13,15})example.tiles.Add(new LevelBlock{x=x,y=x==13 ? 2 : 1,kind=LevelTile.Coin});
        example.tiles.RemoveAll(t=>t.x==18 && t.y==0);example.tiles.Add(new LevelBlock{x=18,y=0,kind=LevelTile.Enemy});
        example.tiles.RemoveAll(t=>t.x==26 && t.y==0);example.tiles.Add(new LevelBlock{x=26,y=0,kind=LevelTile.Cannon});
        example.tiles.Add(new LevelBlock{x=12,y=1,kind=LevelTile.Lift});
        var asset=BuilderPalette.Entries.First(a=>a.id.StartsWith("library.") && a.role==BuilderRole.Block && a.kind==LevelTile.Ground && a.Model!=null);
        example.tiles.RemoveAll(t=>t.x==4 && t.y==0);example.tiles.Add(new LevelBlock{x=4,y=0,kind=asset.kind,asset=asset.id});example.format=4;
        editor.LoadDocument(example);scene.ResetView();yield return null;yield return null;
        Check(scene.Geometry.GetComponentsInChildren<Renderer>().Length>20 && scene.VisibleTileCount>15,"Live scene includes terrain, coins, imported block, scenery and actors");
        Check(!scene.Geometry.GetComponentsInChildren<Collider>().Any(c=>c.enabled) && !scene.Geometry.GetComponentsInChildren<Rigidbody>().Any(b=>!b.isKinematic),"Build scene cannot move, collide, shoot or damage players");
        var active=scene.Geometry.GetComponentsInChildren<MonoBehaviour>().Where(b=>b.enabled && (b.GetType().Namespace=="TreasureRunner" || b is Enemy || b is EnemyPatrol)).ToArray();
        Check(active.Length==0,"Gameplay behaviours are frozen before activation: "+string.Join(",",active.Select(b=>b.GetType().Name)));
        Check(scene.Geometry.GetComponentsInChildren<Renderer>().All(r=>Mathf.Abs(r.bounds.center.z-20000)<100),"Imported geometry stays in the isolated scene rather than at world origin");
        string before=LevelCodec.Encode(LevelEditorScreen.Working);Capture("Level-bauen");
        Button("Abschnitt >").onClick.Invoke();yield return null;Check(scene.CellAt(ScreenPoint(scene,new Vector2Int(20,0)),null,out mapped) && mapped.x==20,"Next section paints absolute coordinates without a second page offset");Capture("Naechster-Abschnitt");
        for(int world=0;world<8;world++){example.world=world;editor.LoadDocument(example);yield return null;yield return null;Check(scene.SceneCamera.backgroundColor==LibraryWorldArt.Sky(world,0),"World backdrop changes in actual builder scene "+world);}
        example.world=0;editor.LoadDocument(example);yield return null;
        Check(LevelCodec.Encode(LevelEditorScreen.Working)==before,"Navigation and camera changes preserve exact saved level document");
        Check(LevelCodec.Validate(LevelEditorScreen.Working,out string reason),"Built scene remains a playable level: "+reason);
        // No file writes: test the exact in-memory draft through the real level factory.
        EditorTestSession.Begin(LevelEditorScreen.Working);LevelRun.StartCustom(LevelEditorScreen.Working);yield return null;yield return null;yield return null;
        Check(LevelRun.IsCustom && LevelRun.Instance?.Player!=null && FindFirstObjectByType<EditorLevelScene>()==null,"Testplay opens real level and disposes isolated builder geometry");
        Check(FindObjectsByType<Collider>(FindObjectsSortMode.None).Any(c=>c.enabled && !c.isTrigger && c.gameObject.layer!=28),"Playable terrain has real collisions after frozen preview");
        LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
        Check(FindFirstObjectByType<EditorLevelScene>()!=null && LevelCodec.Encode(LevelEditorScreen.Working)==before,"Returning from testplay restores the exact draft and scene builder");
    }
    static Vector2 ScreenPoint(EditorLevelScene scene,Vector2Int cell)
    {
        Vector3 uv=scene.SceneCamera.WorldToViewportPoint(new Vector3(cell.x,cell.y-.25f,20000));var r=(RectTransform)scene.transform;
        Vector3 local=new Vector3(Mathf.Lerp(r.rect.xMin,r.rect.xMax,uv.x),Mathf.Lerp(r.rect.yMin,r.rect.yMax,uv.y),0);return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));
    }
    void Capture(string name)
    {
        var camera=new GameObject("Test screenshot camera").AddComponent<Camera>();camera.cullingMask=~(1<<28);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
        foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Builds/LiveEditor-Preview/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;
        for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}camera.targetTexture=null;target.Release();Destroy(target);Destroy(image);Destroy(camera.gameObject);
    }
}
#endif
