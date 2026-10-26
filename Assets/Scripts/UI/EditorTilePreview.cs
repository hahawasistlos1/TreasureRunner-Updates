using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public static class EditorTilePreview
    {
        static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        static readonly Dictionary<Texture2D,Rect> gridUV=new Dictionary<Texture2D,Rect>();
        public static bool FillsGridCell(LevelBlock tile)
        {
            if(string.IsNullOrEmpty(tile.asset))return tile.kind==LevelTile.Ground || tile.kind==LevelTile.Crumble;
            var asset=BuilderPalette.Get(tile.asset);
            if(asset!=null && asset.id.StartsWith("base.") && asset.kind==tile.kind)return tile.kind==LevelTile.Ground || tile.kind==LevelTile.Crumble;
            return asset!=null && asset.role==BuilderRole.Block && asset.kind==LevelTile.Ground && asset.id!="art.Platformer_platform" && asset.id!="art.Platformer_platform-fortified";
        }
        public static Texture2D GetGrid(LevelBlock tile,int world,out Rect uv,out bool fill)
        {fill=FillsGridCell(tile);var image=Render(tile,world,fill);uv=fill && gridUV.TryGetValue(image,out var cropped) ? cropped : new Rect(0,0,1,1);return image;}
        public static Texture2D Get(LevelTile kind,int world)=>Get(new LevelBlock{kind=kind},world);
        public static Texture2D Get(LevelBlock tile,int world)=>Render(tile,world,false);
        static Texture2D Render(LevelBlock tile,int world,bool flat)
        {
            string key=(flat ? "grid:" : "object:")+world+":"+tile.kind+":"+tile.asset;if(cache.TryGetValue(key,out var image) && image!=null)return image;
            var root=new GameObject("Bauteilvorschau (isoliert)");root.SetActive(false);
            root.AddComponent<LevelFactory>().BuildPreview(tile,world);
            foreach(var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
            root.transform.position=new Vector3(0,0,10000);root.SetActive(true);
            var renderers=root.GetComponentsInChildren<Renderer>();Bounds bounds=new Bounds(root.transform.position,Vector3.one);
            bool found=false;foreach(var renderer in renderers)if(renderer.enabled){if(!found){bounds=AssetBlockSurface.GeometryBounds(renderer);found=true;}else bounds.Encapsulate(AssetBlockSurface.GeometryBounds(renderer));}
            var camera=new GameObject("Vorschaukamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=flat ? Mathf.Max(.05f,Mathf.Max(bounds.size.y,bounds.size.x)*.51f) : Mathf.Max(.25f,Mathf.Max(bounds.size.y+bounds.size.z*.22f,bounds.size.x)*.58f);
            camera.transform.position=bounds.center+new Vector3(0,flat ? 0 : 1.7f,-8);camera.transform.LookAt(bounds.center);camera.cullingMask=1<<29;
            var additional=camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();additional.renderPostProcessing=false;camera.allowHDR=false;camera.allowMSAA=false;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.nearClipPlane=.1f;camera.farClipPlane=20;
            var light=new GameObject("Vorschaulicht").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<29;light.transform.rotation=Quaternion.Euler(35,-25,0);
            var target=new RenderTexture(96,96,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.aspect=1;camera.Render();
            var before=RenderTexture.active;RenderTexture.active=target;
            image=new Texture2D(96,96,TextureFormat.RGBA32,false){name="Spielbauteil "+world+" "+tile.kind+" "+tile.asset};
            image.ReadPixels(new Rect(0,0,96,96),0,0);image.Apply();
            bool visible=false;foreach(var pixel in image.GetPixels32())if(pixel.a>10){visible=true;break;}
            // The first render can initialize URP/mesh resources without producing pixels.
            // Retry that render once, before caching a thumbnail for the UI.
            if(!visible){camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,96,96),0,0);image.Apply();}
            if(flat)
            {
                // Frame the actual front face rather than the transparent square around a 3D icon.
                int minX=96,minY=96,maxX=-1,maxY=-1;var pixels=image.GetPixels32();
                for(int y=0;y<96;y++)for(int x=0;x<96;x++)if(pixels[y*96+x].a>10){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
                gridUV[image]=maxX>=minX ? new Rect(minX/96f,minY/96f,(maxX-minX+1)/96f,(maxY-minY+1)/96f) : new Rect(0,0,1,1);
            }
            RenderTexture.active=before;
            // Destroy is deferred: hide this tile now so the next thumbnail cannot include it.
            root.SetActive(false);light.enabled=false;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(camera.gameObject);Object.Destroy(light.gameObject);Object.Destroy(root);
            cache[key]=image;return image;
        }
        public static void Clear(){foreach(var image in cache.Values)if(image!=null)Object.Destroy(image);cache.Clear();gridUV.Clear();}
    }
}
