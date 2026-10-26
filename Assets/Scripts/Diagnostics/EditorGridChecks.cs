#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
namespace TreasureRunner
{
    public class EditorGridChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        Button Button(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==label);
        LevelBlock Tile(BuilderAsset a)=>new LevelBlock{kind=a.kind,asset=a.id.StartsWith("base.") ? "" : a.id};
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/EditorGridPreview");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;var sequence=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Log;if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real save changed");}else report.Add("PASS: Real player save and backup unchanged");failed|=errors;report.Add(failed ? "EDITOR GRID TESTS FAILED" : "ALL EDITOR GRID TESTS PASSED");File.WriteAllLines("Logs/EditorGridTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var solid=BuilderPalette.Entries.Where(a=>EditorTilePreview.FillsGridCell(Tile(a))).ToArray();Check(solid.Length>=10,"Multiple existing solid floor/block variants use filled raster presentation");
            Check(EditorTilePreview.FillsGridCell(new LevelBlock{kind=LevelTile.Ground,asset="base.0"}),"Imported explicit basic-ground ID also fills its tile");Check(EditorTilePreview.FillsGridCell(new LevelBlock{kind=LevelTile.Crumble,asset="base.6"}),"Imported explicit crumbling-floor ID also fills its tile");
            foreach(var asset in solid)for(int world=0;world<8;world++)
            {
                var texture=EditorTilePreview.GetGrid(Tile(asset),world,out var uv,out bool fill);Check(fill && uv.width>0 && uv.height>0 && uv.xMin>=0 && uv.yMin>=0 && uv.xMax<=1.001f && uv.yMax<=1.001f && texture.GetPixels32().Count(p=>p.a>10)>50,"Real visible filled block preview: "+asset.id+" / world "+world);yield return null;
            }
            foreach(var asset in BuilderPalette.Entries.Where(a=>a.role==BuilderRole.Enemy || a.role==BuilderRole.Decoration || a.role==BuilderRole.Spikes || a.role==BuilderRole.Saw || a.role==BuilderRole.Health || a.role==BuilderRole.Shield || a.kind==LevelTile.Lift))Check(!EditorTilePreview.FillsGridCell(Tile(asset)),"Non-block object keeps its shape: "+asset.id);
            foreach(string id in new[]{"art.Platformer_platform","art.Platformer_platform-fortified"})Check(!EditorTilePreview.FillsGridCell(Tile(BuilderPalette.Get(id))),"Thin platform remains recognizable: "+id);
            var source=EditorTilePreview.Get(LevelTile.Ground,0);var filled=EditorTilePreview.GetGrid(new LevelBlock{kind=LevelTile.Ground},0,out var groundUV,out _);Check(source!=filled && groundUV.height<.9f,"Grid removes transparent framing while palette keeps original 3D preview");
            var camera=new GameObject("Editor test camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;
            var level=LevelCodec.Starter();level.title="Gefüllte Rasterblöcke";
            void Put(int x,int y,LevelTile kind,string asset=""){level.tiles.RemoveAll(t=>t.x==x && t.y==y);level.tiles.Add(new LevelBlock{x=x,y=y,kind=kind,asset=asset});}
            for(int x=3;x<=6;x++)for(int y=1;y<=3;y++)Put(x,y,LevelTile.Ground,x<5 ? "art.Platformer_block-grass" : "art.floor_tile_small");Put(8,3,LevelTile.Lift);Put(9,4,LevelTile.Coin);Put(12,0,LevelTile.Enemy);
            string before=LevelCodec.Encode(level);LevelEditorScreen.Working=level;LevelEditorScreen.Open();yield return new WaitForSeconds(.2f);Canvas.ForceUpdateCanvases();var grid=FindFirstObjectByType<EditorPaintSurface>();
            RawImage Preview(int x,int y)=>grid.transform.Find("Feld "+x+","+y).GetComponentInChildren<RawImage>();
            var block=Preview(3,1);var cell=(RectTransform)block.transform.parent;Check(!block.GetComponent<AspectRatioFitter>().enabled && Mathf.Abs(block.rectTransform.rect.width-cell.rect.width)<.1f && Mathf.Abs(block.rectTransform.rect.height-cell.rect.height)<.1f,"Placed solid block occupies full rectangular raster cell instead of a small square icon");
            Check(Preview(8,3).GetComponent<AspectRatioFitter>().enabled && Preview(9,4).GetComponent<AspectRatioFitter>().enabled && Preview(12,0).GetComponent<AspectRatioFitter>().enabled,"Lift, coin and enemy previews keep their proportions in the actual grid");Check(LevelCodec.Encode(LevelEditorScreen.Working)==before,"Redrawing the grid leaves all saved level data unchanged");Capture(camera,"Raster-Gefuellt",block);
            var rect=(RectTransform)grid.transform;Vector3 point=rect.TransformPoint(new Vector3(rect.rect.xMin+rect.rect.width*11.5f/16,rect.rect.yMin+rect.rect.height*4.5f/level.height));grid.OnPointerDown(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,point),pointerId=1});Canvas.ForceUpdateCanvases();Check(LevelEditorScreen.Working.tiles.Any(t=>t.x==11 && t.y==4 && t.kind==LevelTile.Ground) && !Preview(11,4).GetComponent<AspectRatioFitter>().enabled,"Pointer painting still addresses the correct tile and immediately fills it");
            Button("Radierer").onClick.Invoke();grid.paint(11,4);Check(!Preview(11,4).enabled,"Erasing removes the entire filled preview");Button("Rückgängig").onClick.Invoke();Check(Preview(11,4).enabled && !Preview(11,4).GetComponent<AspectRatioFitter>().enabled,"Undo restores a fully filled block");Button("Wiederholen").onClick.Invoke();Check(!Preview(11,4).enabled,"Redo clears the filled preview again");
            var edit=FindFirstObjectByType<LevelEditorScreen>();edit.LoadDocument(LevelCodec.Starter());Check(Preview(3,0).enabled && !Preview(3,0).GetComponent<AspectRatioFitter>().enabled,"Loading another draft uses the filled block presentation");
            var physical=new GameObject("Unchanged gameplay surface");physical.AddComponent<LevelFactory>().BuildPreview(LevelTile.Ground,0);Physics.SyncTransforms();var collider=physical.GetComponentsInChildren<BoxCollider>().First();Check(Mathf.Abs(collider.bounds.size.y-.65f)<.01f && Mathf.Abs(collider.bounds.max.y)<.01f,"Actual game block height and standing surface remain unchanged");Destroy(physical);
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Grid improvement leaves app and Android versions unchanged");
        }
        void Capture(Camera camera,string name,RawImage proof)
        {
            var canvas=proof.GetComponentInParent<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Builds/EditorGridPreview/"+name+".png",image.EncodeToPNG());
            var corners=new Vector3[4];proof.rectTransform.GetWorldCorners(corners);var a=camera.WorldToScreenPoint(corners[0]);var b=camera.WorldToScreenPoint(corners[2]);int x=Mathf.Clamp(Mathf.CeilToInt(a.x)+1,0,1599),y=Mathf.Clamp(Mathf.CeilToInt(a.y)+1,0,899),w=Mathf.Clamp(Mathf.FloorToInt(b.x)-x-1,1,1600-x),h=Mathf.Clamp(Mathf.FloorToInt(b.y)-y-1,1,900-y);var pixels=image.GetPixels(x,y,w,h);float coverage=pixels.Count(p=>p.r>.25f || p.g>.35f || p.b>.4f)/(float)pixels.Length;Check(coverage>.80f,"Rendered solid block visibly covers its cell rather than leaving half empty: "+coverage.ToString("P1"));RenderTexture.active=active;camera.targetTexture=oldTarget;canvas.renderMode=mode;canvas.worldCamera=previousCamera;rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
