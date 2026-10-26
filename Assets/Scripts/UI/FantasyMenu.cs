using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
namespace TreasureRunner
{
    // The artwork is scenery; every control, value, preview and frame is a separate UI element.
    public static class FantasyMenu
    {
        public static readonly Color Border=new Color(.29f,.51f,.57f,.8f),Green=new Color(.13f,.66f,.28f),Surface=new Color(.025f,.07f,.10f,.96f);
        static Font titleFont;
        public static Font TitleFont=>titleFont!=null ? titleFont : (titleFont=Resources.Load<Font>("Fonts/Cinzel")??GameUI.Font);
        public static RectTransform Region(Transform parent,string name,float x,float y,float w,float h)
        {var r=GameUI.Rect(name,parent);r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        public static RectTransform Card(Transform parent,string name,float x,float y,float w,float h)
        {var r=Region(parent,name,x,y,w,h);var image=r.gameObject.AddComponent<RoundedImage>();image.color=Surface;Frame(image);return r;}
        public static void Frame(Graphic image,bool gold=false)
        {var o=image.gameObject.GetComponent<Outline>()??image.gameObject.AddComponent<Outline>();o.effectColor=gold ? GameUI.Gold : Border;o.effectDistance=new Vector2(1,-1);o.useGraphicAlpha=false;}
        public static Text Label(Transform parent,string value,int size,float x,float y,float w,float h,Color? color=null,bool title=false)
        {var t=GameUI.Text(value,parent,size,color);var r=t.rectTransform;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;t.alignment=TextAnchor.MiddleLeft;t.verticalOverflow=VerticalWrapMode.Truncate;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Min(size,12);t.resizeTextMaxSize=size;if(title){t.font=TitleFont;t.fontStyle=FontStyle.Bold;}return t;}
        public static void Art(Transform parent,string path,float x,float y,float w,float h,Color? tint=null)
        {var r=Region(parent,"Illustration",x,y,w,h);var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>(path)??Resources.Load<Texture2D>("MenuArt/FantasyValley");image.color=tint??Color.white;image.raycastTarget=false;var fit=r.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=image.texture!=null ? image.texture.width/(float)image.texture.height : 16f/9;}
        public static void Background(Transform parent)
        {var clip=Region(parent,"Fantasy-Landschaft",0,0,1,1);clip.gameObject.AddComponent<RectMask2D>();Art(clip,"MenuArt/FantasyValley",0,0,1,1);var shade=GameUI.Panel("Lesekontrast",parent,new Color(.008f,.024f,.04f,.43f));GameUI.Stretch(shade);shade.GetComponent<Image>().raycastTarget=false;}
        public static Button Action(Transform parent,string label,UnityAction action,float x,float y,float w,float h,bool enabled=true,bool primary=false)
        {var b=GameUI.Button(label,parent,action,enabled);var r=(RectTransform)b.transform;r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;b.image.color=primary ? Green : new Color(.075f,.23f,.28f);Frame(b.image,primary);b.GetComponentInChildren<Text>().fontSize=20;return b;}
        public static void Bar(Transform parent,float fraction,float x,float y,float w,float h)
        {var r=Card(parent,"Fortschritt",x,y,w,h);var fill=Region(r,"Erreicht",.01f,.15f,.98f*Mathf.Clamp01(fraction),.7f);fill.gameObject.AddComponent<RoundedImage>().color=Green;}
        public static void Icon(Transform parent,string key,float x,float y,float w,float h)
        {var r=Region(parent,"Symbol "+key,x,y,w,h);r.gameObject.AddComponent<CanvasRenderer>();r.gameObject.AddComponent<MenuSymbol>().key=key;}
        public static void Logo(Transform parent,float x,float y,float w,float h)
        {var holder=Region(parent,"TreasureRunner Logo",x,y,w,h);var r=GameUI.Rect("Logo-Grafik",holder);var image=r.gameObject.AddComponent<RawImage>();image.texture=Resources.Load<Texture2D>("MenuArt/TreasureRunnerLogo");image.raycastTarget=false;var fit=r.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=2.2f;}
        public static void TileImage(Transform parent,LevelTile kind,string asset,int world,float x,float y,float w,float h)
        {var holder=Region(parent,"Objektvorschau",x,y,w,h);var r=GameUI.Rect("Objekt-Grafik",holder);var image=r.gameObject.AddComponent<RawImage>();image.texture=EditorTilePreview.Get(new LevelBlock{kind=kind,asset=asset},world);image.raycastTarget=false;var fit=r.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1;}
        public static RectTransform PageCard(Transform parent,string title,string subtitle,string symbol,int height=150)
        {var card=GameUI.Panel(title,parent,Surface);card.gameObject.AddComponent<LayoutElement>().preferredHeight=height;Frame(card.GetComponent<Image>());Icon(card,symbol,.025f,.2f,.095f,.6f);Label(card,title,24,.14f,.57f,.59f,.31f,GameUI.Gold);Label(card,subtitle,20,.14f,.1f,.59f,.46f,GameUI.Muted);return card;}
    }
    public class MenuButtonMotion:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler
    {
        Text label;float previousWidth=-1,previousHeight=-1;int baseSize;
        float target=1;public void OnPointerEnter(PointerEventData e){if(GetComponent<Button>().IsInteractable())target=1.015f;}
        public void OnPointerExit(PointerEventData e){target=1;}public void OnPointerDown(PointerEventData e){if(GetComponent<Button>().IsInteractable())target=.975f;}public void OnPointerUp(PointerEventData e){target=1;}
        void Update(){if(Mathf.Abs(transform.localScale.x-target)>.0002f)transform.localScale=Vector3.one*Mathf.Lerp(transform.localScale.x,target,Time.unscaledDeltaTime*18);}
        void LateUpdate()
        {
            if(label==null)label=GetComponentInChildren<Text>();if(label==null || label.text.Length>26 || label.text.Contains("\n"))return;
            var r=label.rectTransform.rect;if(Mathf.Abs(r.width-previousWidth)<.1f && Mathf.Abs(r.height-previousHeight)<.1f)return;previousWidth=r.width;previousHeight=r.height;
            if(baseSize==0)baseSize=label.fontSize;label.fontSize=baseSize;float natural=label.preferredWidth;
            if(natural>r.width-2)label.fontSize=Mathf.Max(12,Mathf.FloorToInt(baseSize*(r.width-2)/natural));
        }
        void OnDisable(){transform.localScale=Vector3.one;target=1;}
    }
    public class MenuFade:MonoBehaviour
    {
        CanvasGroup group;float elapsed;void Awake(){group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();Replay();}public void Replay(){elapsed=0;enabled=true;if(group!=null)group.alpha=0;}void Update(){if(group==null){enabled=false;return;}elapsed+=Time.unscaledDeltaTime;group.alpha=Mathf.Clamp01(elapsed/.16f);if(elapsed>=.16f)enabled=false;}
    }
    public class MenuSymbol:MaskableGraphic
    {
        public string key="world";
        void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){int n=v.currentVertCount;v.AddVert(a,tint,Vector2.zero);v.AddVert(b,tint,Vector2.zero);v.AddVert(c,tint,Vector2.zero);v.AddVert(d,tint,Vector2.zero);v.AddTriangle(n,n+1,n+2);v.AddTriangle(n,n+2,n+3);}
        void Line(VertexHelper v,Vector2 a,Vector2 b,float width,Color tint){Vector2 n=(b-a).normalized;var p=new Vector2(-n.y,n.x)*width*.5f;Quad(v,a-p,a+p,b+p,b-p,tint);}
        void Circle(VertexHelper v,Vector2 p,float r,Color tint){int n=v.currentVertCount;v.AddVert(p,tint,Vector2.zero);for(int i=0;i<=24;i++){float a=i*Mathf.PI/12;v.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,tint,Vector2.zero);if(i>0)v.AddTriangle(n,n+i,n+i+1);}}
        protected override void OnPopulateMesh(VertexHelper v)
        {v.Clear();raycastTarget=false;var rect=GetPixelAdjustedRect();float s=Mathf.Min(rect.width,rect.height);Vector2 c=rect.center;Color gold=GameUI.Gold,blue=new Color(.4f,.84f,1);float r=s*.33f;
            if(key=="play"){int n=v.currentVertCount;v.AddVert(c+new Vector2(-r,-r),gold,Vector2.zero);v.AddVert(c+new Vector2(-r,r),gold,Vector2.zero);v.AddVert(c+new Vector2(r,0),gold,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
            else if(key=="coin" || key=="shop"){Circle(v,c,r,gold);Circle(v,c,r*.75f,new Color(.88f,.47f,.04f));Circle(v,c,r*.55f,gold);}
            else if(key=="weapons" || key=="editor"){Line(v,c-new Vector2(r,r),c+new Vector2(r,r),s*.14f,blue);Line(v,c+new Vector2(-r,r),c+new Vector2(r,-r),s*.14f,gold);Line(v,c-new Vector2(r*.6f,0),c-new Vector2(0,r*.6f),s*.12f,gold);}
            else if(key=="profile" || key=="friends"){Circle(v,c+Vector2.up*r*.65f,r*.52f,blue);Quad(v,c+new Vector2(-r,-r),c+new Vector2(-r,-r*.25f),c+new Vector2(r,-r*.25f),c+new Vector2(r,-r),blue);if(key=="friends")Circle(v,c+Vector2.right*r, r*.4f,gold);}
            else if(key=="records"){Quad(v,c+new Vector2(-r,r),c+new Vector2(-r*.55f,-r*.4f),c+new Vector2(r*.55f,-r*.4f),c+new Vector2(r,r),gold);Line(v,c-Vector2.up*r*.4f,c-Vector2.up*r,s*.13f,gold);Line(v,c+new Vector2(-r*.7f,-r),c+new Vector2(r*.7f,-r),s*.12f,gold);}
            else {for(int i=0;i<24;i++){float a=i*Mathf.PI/12,b=(i+1)*Mathf.PI/12;Line(v,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,s*.065f,blue);}Line(v,c-Vector2.right*r,c+Vector2.right*r,s*.06f,blue);Line(v,c-Vector2.up*r,c+Vector2.up*r,s*.06f,blue);}
        }
    }
}
