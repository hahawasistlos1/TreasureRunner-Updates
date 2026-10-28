using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TreasureRunner
{
    // The builder renders the same LevelFactory geometry as a playable level. Its isolated
    // Only preview-safe behaviours run; documents and gameplay progress stay separate.
    public sealed class EditorLevelScene:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,IScrollHandler,IPointerMoveHandler,IPointerExitHandler
    {
        const int Layer=28;
        static readonly Vector3 Origin=new Vector3(0,0,20000);
        readonly Dictionary<int,GameObject> nodes=new Dictionary<int,GameObject>();
        readonly Dictionary<int,string> keys=new Dictionary<int,string>();
        Transform stage,backdrop,ghost,startMarker;Camera view;RenderTexture target;RawImage image;
        CommunityLevel document;LevelBlock brush;int lastWorld=-1,lastWidth=-1,lastPage=-1,lastTool;
        Vector2 center=new Vector2(7.5f,4.5f);float distance=16;bool dragging,dragPan;Vector2 lastPointer,pressPointer;
        Camera pointerCamera;float pressedAt,suppressPaintUntil;int pointerId;Vector2 gestureMid;float gestureSpan;bool gesture;
        int backdropStart=-1;
        Vector2Int previousCell=new Vector2Int(-1,-1);string ghostKey;
        public Action<int,int> paint;public bool PanMode {get;set;}
        public Camera SceneCamera=>view;public Transform Geometry=>stage;
        public int VisibleTileCount=>nodes.Count;
        public bool Simulation {get;private set;}
        public Vector2 ViewCenter=>center;
        public void SetSimulation(bool enabled){if(Simulation==enabled)return;Simulation=enabled;ResetSimulation();}
        public void ResetSimulation(){foreach(var node in nodes.Values)Retire(node);nodes.Clear();keys.Clear();SyncTiles();}
        public void Initialize()
        {
            image=gameObject.AddComponent<RawImage>();image.color=Color.white;
            stage=new GameObject("Live-Levelbau (isoliert)").transform;stage.position=Origin;
            view=new GameObject("Baukamera").AddComponent<Camera>();view.enabled=false;view.fieldOfView=42;
            view.nearClipPlane=.1f;view.farClipPlane=100;view.clearFlags=CameraClearFlags.SolidColor;view.cullingMask=1<<Layer;
            var data=view.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();data.renderPostProcessing=false;view.allowHDR=false;view.allowMSAA=false;
            var sun=new GameObject("Bausonne").AddComponent<Light>();sun.transform.SetParent(stage);sun.type=LightType.Directional;sun.intensity=1.35f;sun.cullingMask=1<<Layer;sun.transform.rotation=Quaternion.Euler(45,-30,0);sun.shadows=LightShadows.None;
        }
        public void Synchronize(CommunityLevel level,int page,LevelBlock selected,int tool)
        {
            document=level;brush=selected;lastTool=tool;
            if(lastWorld!=level.world || lastWidth!=level.width)
            {
                foreach(var node in nodes.Values)Retire(node);nodes.Clear();keys.Clear();
                Retire(backdrop==null ? null : backdrop.gameObject);backdrop=null;lastWorld=level.world;lastWidth=level.width;ghostKey=null;
                view.backgroundColor=LibraryWorldArt.Sky(level.world,0);
            }
            if(lastPage!=page){lastPage=page;center.x=Mathf.Min(level.width-1,page*16+7.5f);}
            UpdateCamera();SyncTiles();UpdateStart();
            if(ghost!=null){Retire(ghost.gameObject);ghost=null;ghostKey=null;}
        }
        public void FocusPage(int page){lastPage=page;center.x=Mathf.Min(document.width-1,page*16+7.5f);UpdateCamera();SyncTiles();}
        public void ResetView(){distance=16;center=new Vector2(Mathf.Min(document.width-1,lastPage*16+7.5f),4.5f);UpdateCamera();SyncTiles();}
        public void PanBy(Vector2 delta){center+=delta;UpdateCamera();SyncTiles();}
        public void Zoom(float factor){distance=Mathf.Clamp(distance*factor,9,40);UpdateCamera();SyncTiles();}
        void UpdateCamera()
        {
            center.x=Mathf.Clamp(center.x,0,Mathf.Max(0,(document?.width??64)-1));center.y=Mathf.Clamp(center.y,0,(document?.height??12)-1);
            view.transform.position=Origin+new Vector3(center.x,center.y+distance*.14f,-distance);view.transform.rotation=Quaternion.Euler(8,0,0);
        }
        static void Retire(GameObject node){if(node==null)return;node.SetActive(false);Destroy(node);}
        static void Freeze(GameObject node)
        {
            foreach(var b in node.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
            foreach(var a in node.GetComponentsInChildren<Animation>(true))a.enabled=false;
            foreach(var a in node.GetComponentsInChildren<Animator>(true))a.enabled=false;
            foreach(var c in node.GetComponentsInChildren<Collider>(true))c.enabled=false;
            foreach(var body in node.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;body.detectCollisions=false;}
            foreach(var t in node.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        }
        GameObject Tile(LevelBlock tile,Transform parent,bool simulate=true)
        {
            var node=new GameObject("Bauteil "+tile.x+","+tile.y+" · "+tile.kind);node.SetActive(false);
            node.AddComponent<LevelFactory>().BuildPreview(tile,document.world);
            if(simulate && Simulation)PrepareSimulation(node,tile);else Freeze(node);
            node.transform.SetParent(parent,false);node.transform.localPosition=new Vector3(tile.x,tile.y,0);node.SetActive(true);return node;
        }
        void PrepareSimulation(GameObject node,LevelBlock tile)
        {
            foreach(var b in node.GetComponentsInChildren<MonoBehaviour>(true))
                b.enabled=b is EnemyPatrol || b is LibraryMotion || b is LibraryEnemyEquipment || b is SkeletonMotion || b is CharacterPresentation || b is EnemyPresentation || b is WorldTrap || b is MovingRide || b is EnemyRangedAttack || b is EnemyTactics || b is CoinSpin || b is AssetHazard;
            foreach(var t in node.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            foreach(var c in node.GetComponentsInChildren<Collider>(true))if(c.isTrigger || c.GetComponentInParent<Enemy>(true)!=null)c.enabled=false;
            foreach(var body in node.GetComponentsInChildren<Rigidbody>(true)){body.isKinematic=true;body.useGravity=false;body.detectCollisions=true;}
            foreach(var ride in node.GetComponentsInChildren<MovingRide>(true))ride.EditorPreview=true;
            foreach(var enemy in node.GetComponentsInChildren<Enemy>(true)){enemy.EditorPreview=true;enemy.GetComponent<EnemyPatrol>().EditorPreview=true;enemy.GetComponent<Rigidbody>().detectCollisions=false;}
            foreach(var trap in node.GetComponentsInChildren<WorldTrap>(true))
            {
                trap.EditorPreview=true;trap.floorY+=tile.y;
                if(trap.kind==TrapKind.FallingRock)
                    foreach(var floor in document.tiles)if(floor.x==tile.x && floor.y<tile.y && LevelCodec.Floor(floor.kind))trap.floorY=Mathf.Max(trap.floorY,floor.y);
            }
            foreach(var ranged in node.GetComponentsInChildren<EnemyRangedAttack>(true))ranged.EditorPreview=true;
            foreach(var tactic in node.GetComponentsInChildren<EnemyTactics>(true))tactic.EditorPreview=true;
            foreach(var hazard in node.GetComponentsInChildren<AssetHazard>(true))hazard.EditorPreview=true;
        }
        void SyncTiles()
        {
            if(document==null || stage==null)return;
            int desired=Mathf.Max(0,Mathf.FloorToInt((center.x-32)/10)*10);
            if(backdrop==null || desired!=backdropStart)BuildBackdrop();
            var keep=new HashSet<int>();float half=distance*view.aspect*.42f+5;
            foreach(var tile in document.tiles)
            {
                if(Mathf.Abs(tile.x-center.x)>half)continue;int id=LevelCodec.Cell(tile.x,tile.y);keep.Add(id);string key=tile.kind+":"+tile.asset;
                if(keys.TryGetValue(id,out var old) && old==key)continue;
                if(nodes.TryGetValue(id,out var existing))Retire(existing);
                nodes[id]=Tile(tile,stage);keys[id]=key;
            }
            var removed=new List<int>();foreach(var id in nodes.Keys)if(!keep.Contains(id))removed.Add(id);
            foreach(int id in removed){Retire(nodes[id]);nodes.Remove(id);keys.Remove(id);}
        }
        void BuildBackdrop()
        {
            Retire(backdrop==null ? null : backdrop.gameObject);
            backdropStart=Mathf.Max(0,Mathf.FloorToInt((center.x-32)/10)*10);
            var node=new GameObject("Echte Levelkulisse");node.SetActive(false);
            var factory=node.AddComponent<LevelFactory>();factory.BuildEditorBackdrop(document.world,Mathf.Min(80,document.width-backdropStart));Freeze(node);node.transform.SetParent(stage,false);node.transform.localPosition=Vector3.right*backdropStart;node.SetActive(true);backdrop=node.transform;
        }
        void UpdateStart()
        {
            if(document.tiles.Count==0){if(startMarker!=null)startMarker.gameObject.SetActive(false);return;}
            if(startMarker==null)
            {
                var node=new GameObject("Startfigur (Vorschau)");node.SetActive(false);node.transform.SetParent(stage,false);
                CharacterCatalog.Build(node.transform,1.8f,0);Freeze(node);node.SetActive(true);startMarker=node.transform;
            }
            startMarker.gameObject.SetActive(true);startMarker.localPosition=new Vector3(document.spawnX,document.spawnY,0);
        }
        public bool CellAt(Vector2 screen,Camera eventCamera,out Vector2Int cell)
        {
            cell=default;var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,screen,eventCamera,out var p))return false;
            var uv=new Vector2((p.x-rect.rect.xMin)/rect.rect.width,(p.y-rect.rect.yMin)/rect.rect.height);
            if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1)return false;
            var ray=view.ViewportPointToRay(uv);if(!new Plane(Vector3.forward,Origin).Raycast(ray,out float enter))return false;
            Vector3 world=ray.GetPoint(enter)-Origin;cell=new Vector2Int(Mathf.FloorToInt(world.x+.5f),Mathf.FloorToInt(world.y+.65f));
            return document!=null && cell.x>=0 && cell.x<document.width && cell.y>=0 && cell.y<document.height;
        }
        void ShowGhost(Vector2Int cell)
        {
            if(lastTool>=14){if(ghost!=null)ghost.gameObject.SetActive(false);return;}
            string key=brush.kind+":"+brush.asset;
            if(ghost==null || key!=ghostKey){if(ghost!=null)Retire(ghost.gameObject);ghost=Tile(brush,stage,false).transform;ghost.name="Platzierungsvorschau";ghostKey=key;
                foreach(var r in ghost.GetComponentsInChildren<Renderer>())for(int i=0;i<r.sharedMaterials.Length;i++){var block=new MaterialPropertyBlock();r.GetPropertyBlock(block,i);var color=r.sharedMaterials[i].HasProperty("_BaseColor") ? r.sharedMaterials[i].GetColor("_BaseColor") : Color.white;block.SetColor("_BaseColor",Color.Lerp(color,new Color(.25f,1,.8f),.55f));r.SetPropertyBlock(block,i);}}
            ghost.gameObject.SetActive(true);ghost.localPosition=new Vector3(cell.x,cell.y,-.03f);
        }
        void Draw(PointerEventData e)
        {
            if(!CellAt(e.position,e.pressEventCamera,out var cell)){if(ghost!=null)ghost.gameObject.SetActive(false);return;}
            ShowGhost(cell);if(previousCell==cell)return;previousCell=cell;paint?.Invoke(cell.x,cell.y);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(dragging)return;dragging=true;pointerId=e.pointerId;pointerCamera=e.pressEventCamera;pressedAt=Time.unscaledTime;
            previousCell=new Vector2Int(-1,-1);dragPan=PanMode || e.button!=PointerEventData.InputButton.Left;lastPointer=pressPointer=e.position;
        }
        public void OnDrag(PointerEventData e)
        {
            if(!dragging || e.pointerId!=pointerId)return;
            if(gesture || TouchCount()>1 || Time.unscaledTime<suppressPaintUntil){lastPointer=e.position;return;}
            if(dragPan)PanBy(-(e.position-lastPointer)*WorldPerPixel());
            else if((e.position-pressPointer).sqrMagnitude>64 || Time.unscaledTime-pressedAt>.1f)Draw(e);
            lastPointer=e.position;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(!dragging || e.pointerId!=pointerId)return;
            if(!dragPan && !gesture && TouchCount()<2 && Time.unscaledTime>=suppressPaintUntil)Draw(e);
            dragging=false;previousCell=new Vector2Int(-1,-1);
        }
        public void CancelInteraction(){dragging=false;gesture=false;previousCell=new Vector2Int(-1,-1);if(ghost!=null)ghost.gameObject.SetActive(false);}
        float WorldPerPixel(){var rect=(RectTransform)transform;return distance*2*Mathf.Tan(view.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Max(1,rect.rect.height*rect.lossyScale.y);}
        static int TouchCount()=>UnityEngine.InputSystem.Touchscreen.current==null ? 0 : UnityEngine.InputSystem.Touchscreen.current.touches.Count(t=>t.press.isPressed);
        public void Gesture(Vector2 midpoint,float span)
        {
            suppressPaintUntil=Time.unscaledTime+.15f;
            if(gesture){PanBy(-(midpoint-gestureMid)*WorldPerPixel());if(span>20 && gestureSpan>20)Zoom(gestureSpan/span);}
            gesture=true;gestureMid=midpoint;gestureSpan=span;if(ghost!=null)ghost.gameObject.SetActive(false);
        }
        void Update()
        {
            if(!dragging){gesture=false;return;}
            var screen=UnityEngine.InputSystem.Touchscreen.current;
            if(screen==null)return;var touches=screen.touches.Where(t=>t.press.isPressed).Take(2).ToArray();
            if(touches.Length==2)
            {
                var a=touches[0].position.ReadValue();var b=touches[1].position.ReadValue();
                if(RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,(a+b)*.5f,pointerCamera))Gesture((a+b)*.5f,Vector2.Distance(a,b));
            }
            else if(touches.Length==0)gesture=false;
        }
        public void OnPointerMove(PointerEventData e){if(!dragging && !PanMode && CellAt(e.position,e.enterEventCamera,out var cell))ShowGhost(cell);}
        public void OnPointerExit(PointerEventData e){if(ghost!=null)ghost.gameObject.SetActive(false);}
        public void OnScroll(PointerEventData e){Zoom(Mathf.Pow(.87f,e.scrollDelta.y));}
        void LateUpdate()
        {
            if(document==null || view==null)return;var rect=(RectTransform)transform;
            if(dragging && !dragPan && !gesture && TouchCount()<2 && Time.unscaledTime>=suppressPaintUntil && Time.unscaledTime-pressedAt>.12f)
            {
                if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,lastPointer,pointerCamera,out var local))
                {
                    var uv=new Vector2((local.x-rect.rect.xMin)/rect.rect.width,(local.y-rect.rect.yMin)/rect.rect.height);
                    if(uv.x>=0 && uv.x<=1 && uv.y>=0 && uv.y<=1)
                    {
                        var delta=new Vector2(uv.x<.035f ? -1 : uv.x>.965f ? 1 : 0,uv.y<.055f ? -1 : uv.y>.945f ? 1 : 0)*4*Time.unscaledDeltaTime;
                        if(delta.sqrMagnitude>0)PanBy(delta);
                        Draw(new PointerEventData(EventSystem.current){position=lastPointer});
                    }
                }
            }
            // Animation and ranged Start methods attach weapons/warnings after tile construction.
            foreach(var renderer in stage.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=Layer;
            int width=Mathf.Clamp(Mathf.RoundToInt(rect.rect.width*rect.lossyScale.x),320,1600),height=Mathf.Clamp(Mathf.RoundToInt(rect.rect.height*rect.lossyScale.y),180,900);
            if(target==null || target.width!=width || target.height!=height){if(target!=null){view.targetTexture=null;target.Release();Destroy(target);}target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);view.targetTexture=target;view.aspect=width/(float)height;image.texture=target;SyncTiles();}
            view.Render();
        }
        void OnDestroy(){if(view!=null){view.targetTexture=null;Destroy(view.gameObject);}if(target!=null){target.Release();Destroy(target);}if(stage!=null)Retire(stage.gameObject);}
    }
}
