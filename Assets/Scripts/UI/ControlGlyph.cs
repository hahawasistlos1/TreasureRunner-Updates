using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class ControlGlyph:Image
    {
        public enum Shape { Disc, Ring, Jump, Sword, Swap }
        public Shape shape;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Rect r=GetPixelAdjustedRect();float radius=Mathf.Min(r.width,r.height)*.46f;Vector2 center=r.center;
            if(shape==Shape.Disc || shape==Shape.Ring)
            {
                for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64,b=(i+1)*Mathf.PI*2/64;Vector2 u=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                    if(shape==Shape.Disc)Triangle(vh,center,center+u*radius,center+v*radius);
                    else Quad(vh,center+u*radius,center+v*radius,center+v*radius*.88f,center+u*radius*.88f);}
                return;
            }
            if(shape==Shape.Jump){Quad(vh,P(-.13f,-.55f),P(.13f,-.55f),P(.13f,.15f),P(-.13f,.15f));Triangle(vh,P(-.55f,.08f),P(.55f,.08f),P(0,.65f));}
            if(shape==Shape.Sword){Quad(vh,P(-.4f,-.42f),P(-.25f,-.57f),P(.48f,.25f),P(.3f,.4f));Triangle(vh,P(.3f,.4f),P(.48f,.25f),P(.57f,.62f));Quad(vh,P(-.62f,-.14f),P(-.48f,.02f),P(.02f,-.48f),P(-.12f,-.62f));}
            if(shape==Shape.Swap){Quad(vh,P(-.5f,.16f),P(.32f,.16f),P(.32f,.34f),P(-.5f,.34f));Triangle(vh,P(.25f,.06f),P(.25f,.46f),P(.62f,.26f));Quad(vh,P(-.32f,-.34f),P(.5f,-.34f),P(.5f,-.16f),P(-.32f,-.16f));Triangle(vh,P(-.25f,-.46f),P(-.25f,-.06f),P(-.62f,-.26f));}
            Vector2 P(float x,float y)=>center+new Vector2(x,y)*radius;
        }
        void Triangle(VertexHelper v,Vector2 a,Vector2 b,Vector2 c){int n=v.currentVertCount;v.AddVert(a,color,Vector2.zero);v.AddVert(b,color,Vector2.zero);v.AddVert(c,color,Vector2.zero);v.AddTriangle(n,n+1,n+2);}
        void Quad(VertexHelper v,Vector2 a,Vector2 b,Vector2 c,Vector2 d){Triangle(v,a,b,c);Triangle(v,a,c,d);}
        public static ControlGlyph Add(Transform parent,string title,Shape shape,Color color,float inset=0)
        {var rect=GameUI.Rect(title,parent);GameUI.Stretch(rect,inset,inset,inset,inset);var g=rect.gameObject.AddComponent<ControlGlyph>();g.shape=shape;g.color=color;g.raycastTarget=false;return g;}
    }
}
