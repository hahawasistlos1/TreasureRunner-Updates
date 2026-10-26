using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace TreasureRunner
{
    // User-supplied artwork is sampled as a UI atlas. No screenshot is used as a flattened menu.
    public static class ReferenceMenuSkin
    {
        public const float Width=1672,Height=941;
        public static readonly Color Surface=new Color(.025f,.064f,.085f,.985f),Edge=new Color(.16f,.30f,.35f),TextColor=new Color(.91f,.95f,.97f),Gold=new Color(1,.81f,.32f);
        static Texture2D atlas;
        public static Texture2D Atlas=>atlas!=null ? atlas : (atlas=Resources.Load<Texture2D>("MenuArt/Reference/UserMenu"));
        static readonly Dictionary<string,Rect> icons=new Dictionary<string,Rect>{
            {"home",new Rect(477,124,38,36)},{"world",new Rect(48,229,41,38)},{"levels",new Rect(660,122,42,39)},
            {"shop",new Rect(48,285,43,37)},{"weapons",new Rect(49,338,40,39)},{"inventory",new Rect(49,393,39,39)},
            {"character",new Rect(49,450,40,39)},{"editor",new Rect(48,505,43,39)},{"community",new Rect(49,560,42,40)},
            {"friends",new Rect(49,616,42,39)},{"records",new Rect(49,670,43,40)},{"settings",new Rect(49,727,42,39)},
            {"profile",new Rect(50,783,39,40)},{"updates",new Rect(49,837,42,40)},{"credits",new Rect(49,892,42,38)},
            {"play",new Rect(59,177,29,31)},{"coin",new Rect(1164,121,40,41)},{"gem",new Rect(1368,122,41,38)},
            {"life",new Rect(1361,336,29,28)},{"speed",new Rect(1361,371,30,30)},{"jump",new Rect(1361,406,30,29)},
            {"attack",new Rect(1361,440,30,30)},{"defense",new Rect(1361,475,30,30)},
            {"skull",new Rect(1054,388,26,26)},{"clock",new Rect(1054,423,26,26)}
        };
        public static RectTransform Place(Transform parent,string name,float x,float y,float w,float h)
        {var r=GameUI.Rect(name,parent);r.anchorMin=new Vector2(x/Width,1-(y+h)/Height);r.anchorMax=new Vector2((x+w)/Width,1-y/Height);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        public static RectTransform Local(Transform parent,string name,float x,float y,float w,float h,float designWidth,float designHeight)
        {return FantasyMenu.Region(parent,name,x/designWidth,1-(y+h)/designHeight,w/designWidth,h/designHeight);}
        public static RawImage Crop(Transform parent,Rect source,float x,float y,float w,float h,string name="Vorlagengrafik")
        {var r=FantasyMenu.Region(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.texture=Atlas;image.uvRect=new Rect(source.x/Width,1-source.yMax/Height,source.width/Width,source.height/Height);image.raycastTarget=false;return image;}
        public static void Icon(Transform parent,string key,float x,float y,float w,float h)
        {if(!icons.TryGetValue(key,out var source))return;var r=FantasyMenu.Region(parent,"Symbol "+key,x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.texture=Atlas;image.uvRect=new Rect(source.x/Width,1-source.yMax/Height,source.width/Width,source.height/Height);image.raycastTarget=false;}
        public static RectTransform Panel(Transform parent,string name,float x,float y,float w,float h,Color? color=null)
        {var r=FantasyMenu.Region(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<ReferencePanel>();image.color=color??Surface;return r;}
        public static Text Label(Transform parent,string value,int size,float x,float y,float w,float h,Color? color=null,bool bold=false)
        {var t=FantasyMenu.Label(parent,value,size,x,y,w,h,color??TextColor);t.fontStyle=bold ? FontStyle.Bold : FontStyle.Normal;return t;}
        public static Button Button(Transform parent,string text,UnityAction action,float x,float y,float w,float h,bool green=false,int size=21)
        {
            var r=Panel(parent,"Button "+text,x,y,w,h,green ? new Color(.08f,.65f,.22f) : new Color(.07f,.22f,.27f));
            var button=r.gameObject.AddComponent<Button>();button.onClick.AddListener(action);r.gameObject.AddComponent<MenuButtonMotion>();
            r.GetComponent<ReferencePanel>().border=green ? new Color(.44f,.99f,.49f) : new Color(.28f,.57f,.64f);
            var colors=button.colors;colors.highlightedColor=new Color(1,1,.85f);colors.pressedColor=new Color(.65f,.85f,.72f);button.colors=colors;
            var label=Label(r,text,size,.035f,.05f,.93f,.9f,null,true);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        public static void Selected(Button button,bool selected)
        {bool nav=button.name.StartsWith("Navigation ");button.image.color=selected ? nav ? new Color(1f,.68f,.15f) : new Color(.35f,.28f,.10f) : Surface;var panel=button.GetComponent<ReferencePanel>();if(panel!=null)panel.border=selected ? Gold : Edge;foreach(var text in button.GetComponentsInChildren<Text>())text.color=selected && nav ? new Color(.15f,.10f,.025f) : text.fontSize<=15 ? new Color(.72f,.82f,.88f) : TextColor;}
        public static void Background(Transform root)
        {
            var basePanel=GameUI.Panel("Menühintergrund",root,new Color(.018f,.045f,.055f));GameUI.Stretch(basePanel);basePanel.GetComponent<Image>().raycastTarget=false;
            var top=Place(root,"Wald und Burg",442,0,1230,109);Crop(top,new Rect(442,0,1230,109),0,0,1,1);
            var logo=Place(root,"Originales TreasureRunner-Logo",0,0,442,165);Crop(logo,new Rect(0,0,442,165),0,0,1,1);
        }
        public static void Progress(Transform parent,float value,float x,float y,float w,float h)
        {
            var track=Panel(parent,"Fortschrittsbalken",x,y,w,h,new Color(.07f,.15f,.18f));
            var fill=FantasyMenu.Region(track,"Fortschritt gefüllt",0,0,Mathf.Clamp01(value),1);fill.gameObject.AddComponent<ReferencePanel>().color=new Color(.08f,.78f,.35f);fill.GetComponent<Image>().raycastTarget=false;
        }
    }
    public class ReferencePanel:RoundedImage
    {
        public Color border=new Color(.16f,.30f,.35f);public float borderWidth=1;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            base.OnPopulateMesh(mesh);var rect=GetPixelAdjustedRect();var vertex=new UIVertex();
            for(int i=0;i<mesh.currentVertCount;i++){mesh.PopulateUIVertex(ref vertex,i);float y=Mathf.InverseLerp(rect.yMin,rect.yMax,vertex.position.y);float light=Mathf.Lerp(.72f,1.17f,y)+Mathf.Pow(y,14)*.15f;vertex.color=new Color(color.r*light,color.g*light,color.b*light,color.a);mesh.SetUIVertex(vertex,i);}
            if(borderWidth<=0)return;
            // A true border ring, so translucent surfaces do not receive an opaque duplicate fill.
            int start=mesh.currentVertCount;float radius=Mathf.Min(13,Mathf.Min(rect.width,rect.height)*.18f),inner=Mathf.Max(0,radius-borderWidth);
            for(int corner=0;corner<4;corner++)for(int step=0;step<=5;step++){
                Vector2 center=new Vector2(corner==0 || corner==3 ? rect.xMax-radius : rect.xMin+radius,corner<2 ? rect.yMax-radius : rect.yMin+radius);
                float angle=(corner*90+step*18)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                mesh.AddVert(center+direction*radius,border,Vector2.zero);mesh.AddVert(center+direction*inner,border,Vector2.zero);
            }
            for(int i=0;i<24;i++){int a=start+i*2,b=start+((i+1)%24)*2;mesh.AddTriangle(a,b,a+1);mesh.AddTriangle(a+1,b,b+1);}
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public class ReferencePlayGlyph:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {mesh.Clear();var r=GetPixelAdjustedRect();mesh.AddVert(new Vector2(r.xMin,r.yMin),color,Vector2.zero);mesh.AddVert(new Vector2(r.xMin,r.yMax),color,Vector2.zero);mesh.AddVert(new Vector2(r.xMax,r.center.y),color,Vector2.zero);mesh.AddTriangle(0,1,2);raycastTarget=false;}
    }
}
