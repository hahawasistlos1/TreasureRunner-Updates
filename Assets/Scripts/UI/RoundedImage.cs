using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class RoundedImage:Image
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var r=GetPixelAdjustedRect();float radius=Mathf.Min(13,Mathf.Min(r.width,r.height)*.18f);
            mesh.AddVert(r.center,color,Vector2.zero);
            for(int corner=0;corner<4;corner++)
            {
                var center=new Vector2(corner==0 || corner==3 ? r.xMax-radius : r.xMin+radius,corner<2 ? r.yMax-radius : r.yMin+radius);
                for(int step=0;step<=5;step++)
                {
                    float a=(corner*90+step*18)*Mathf.Deg2Rad;
                    mesh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,color,Vector2.zero);
                }
            }
            for(int i=1;i<=24;i++)mesh.AddTriangle(0,i,i==24 ? 1 : i+1);
        }
    }
}
