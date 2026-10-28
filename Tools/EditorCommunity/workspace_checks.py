from pathlib import Path
p=Path('Assets/Editor/TreasureRunnerEditorCommunityTests.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('using UnityEngine.UI;','using UnityEngine.UI;\nusing UnityEngine.EventSystems;\nusing UnityEngine.InputSystem;\nusing UnityEngine.InputSystem.LowLevel;')
s=s.replace('readonly List<string> report=new List<string>();bool failed;','readonly List<string> report=new List<string>();bool failed;Touchscreen simulatedTouch;')
s=s.replace('Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();','if(simulatedTouch!=null)InputSystem.RemoveDevice(simulatedTouch);Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();')
s=s.replace('Logs/EditorCommunity','Logs/EditorWorkspace-R2')
needle='        var longLevel=LevelCodec.Starter();'
insert='''        LevelEditorScreen.Working=LevelCodec.Starter();LevelEditorScreen.Open();yield return null;yield return null;Canvas.ForceUpdateCanvases();
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
        Check(workspaceView.ViewCenter.y>centerBefore+.5f && LevelEditorScreen.Working.tiles.Count==0,"Real Input System two-finger drag pans vertically without painting");
        float cameraBefore=workspaceView.SceneCamera.transform.position.z;SendTouch(1,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.left*110);SendTouch(2,UnityEngine.InputSystem.TouchPhase.Moved,mid+shift+Vector2.right*110);yield return null;yield return null;
        Check(workspaceView.SceneCamera.transform.position.z>cameraBefore && LevelEditorScreen.Working.tiles.Count==0,"Two-finger pinch changes zoom without adding blocks");
        SendTouch(1,UnityEngine.InputSystem.TouchPhase.Ended,mid+shift+Vector2.left*110);SendTouch(2,UnityEngine.InputSystem.TouchPhase.Ended,mid+shift+Vector2.right*110);yield return null;yield return new WaitForSeconds(.2f);
        Check(LevelEditorScreen.Working.tiles.Count==0,"Lifting fingers after a gesture does not create phantom blocks");InputSystem.RemoveDevice(simulatedTouch);simulatedTouch=null;workspaceView.CancelInteraction();
        var tall=LevelCodec.Starter();tall.height=32;tall.format=5;tall.tiles.Add(new LevelBlock{x=5,y=31,kind=LevelTile.Ground});Check(LevelCodec.Validate(tall,out _),"New format supports tall worlds and uppermost build row");tall.format=4;Check(!LevelCodec.Validate(tall,out _),"Legacy level formats keep their previous height limit");
        BuilderTestNavigation.Button("Menü").onClick.Invoke();yield return null;yield return null;
'''
assert needle in s;s=s.replace(needle,insert+needle)
needle='    void Capture(string name)'
insert='''    void SendTouch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 point)=>InputSystem.QueueStateEvent(simulatedTouch,new TouchState{touchId=id,phase=phase,position=point});
    static Vector2 ScreenPoint(EditorLevelScene scene,Vector2Int cell)
    {
        var uv=scene.SceneCamera.WorldToViewportPoint(new Vector3(cell.x,cell.y-.25f,20000));return ViewportPoint(scene,new Vector2(uv.x,uv.y));
    }
    static Vector2 ViewportPoint(EditorLevelScene scene,Vector2 uv)
    {
        var r=(RectTransform)scene.transform;var local=new Vector3(Mathf.Lerp(r.rect.xMin,r.rect.xMax,uv.x),Mathf.Lerp(r.rect.yMin,r.rect.yMax,uv.y),0);return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(local));
    }
'''
s=s.replace(needle,insert+needle);p.write_text(s,encoding='utf-8')
