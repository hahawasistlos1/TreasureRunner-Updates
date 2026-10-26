using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class MenuWeaponPreview
    {
        static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
        public static void Show(Transform parent,string id,float x=.025f,float y=.15f,float w=.11f,float h=.70f)
        {foreach(var icon in parent.GetComponentsInChildren<MenuSymbol>())icon.enabled=false;var holder=FantasyMenu.Region(parent,"Waffenvorschau "+id,x,y,w,h);var image=GameUI.Rect("Waffen-Grafik",holder).gameObject.AddComponent<RawImage>();image.texture=Get(id);image.raycastTarget=false;var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;}
        public static Texture2D Get(string id)
        {
            if(cache.TryGetValue(id,out var result) && result!=null)return result;
            var root=new GameObject("Waffen-Vorschau");root.transform.position=new Vector3(0,0,15000);WeaponModel.Build(root.transform,id);
            foreach(var t in root.GetComponentsInChildren<Transform>())t.gameObject.layer=27;
            var renderers=root.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            var camera=new GameObject("Waffen-Kamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.size.y,bounds.size.x)*.65f;camera.cullingMask=1<<27;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.transform.position=bounds.center+new Vector3(0,0,-4);camera.transform.LookAt(bounds.center);
            camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
            var light=new GameObject("Waffen-Licht").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<27;light.transform.rotation=Quaternion.Euler(25,-35,0);
            var rt=new RenderTexture(192,192,16,RenderTextureFormat.ARGB32);camera.targetTexture=rt;var before=RenderTexture.active;camera.Render();camera.Render();RenderTexture.active=rt;result=new Texture2D(192,192,TextureFormat.RGBA32,false);result.ReadPixels(new Rect(0,0,192,192),0,0);result.Apply();cache[id]=result;RenderTexture.active=before;camera.targetTexture=null;root.SetActive(false);light.enabled=false;rt.Release();Object.Destroy(rt);Object.Destroy(root);Object.Destroy(camera.gameObject);Object.Destroy(light.gameObject);return result;
        }
    }
}
