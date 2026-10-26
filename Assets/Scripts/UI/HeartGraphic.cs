using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    // Vector hearts render reliably on Android without depending on a font glyph.
    [RequireComponent(typeof(CanvasRenderer))]
    public class HeartGraphic:Image
    {
        private int units=2;
        public int Units { get=>units;set{int next=Mathf.Clamp(value,0,2);if(next==units)return;units=next;SetVerticesDirty();} }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();float size=Mathf.Min(rect.width,rect.height);
            DrawSide(mesh,rect.center,size,true,new Color(.03f,.025f,.06f),1);
            DrawSide(mesh,rect.center,size,false,new Color(.03f,.025f,.06f),1);
            DrawSide(mesh,rect.center,size,true,units>=1 ? new Color(1,.22f,.35f) : new Color(.25f,.29f,.35f),.85f);
            DrawSide(mesh,rect.center,size,false,units>=2 ? new Color(.93f,.10f,.23f) : new Color(.25f,.29f,.35f),.85f);
        }
        static void DrawSide(VertexHelper mesh,Vector2 center,float size,bool left,Color tint,float scale)
        {
            int first=mesh.currentVertCount;mesh.AddVert(center,tint,Vector2.zero);
            for(int i=0;i<=24;i++)
            {
                float t=(left ? Mathf.PI : 0)+i/24f*Mathf.PI;
                float x=16*Mathf.Pow(Mathf.Sin(t),3);
                float y=13*Mathf.Cos(t)-5*Mathf.Cos(2*t)-2*Mathf.Cos(3*t)-Mathf.Cos(4*t);
                mesh.AddVert(center+new Vector2(x/32,(y+2.5f)/32)*size*scale,tint,Vector2.zero);
            }
            for(int i=0;i<24;i++)mesh.AddTriangle(first,first+i+1,first+i+2);
        }
    }
}
