using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class ResponsiveMenuGrid:MonoBehaviour
    {
        private void Start()=>Resize();
        private void OnRectTransformDimensionsChange()=>Resize();
        private void Resize()
        {
            var grid=GetComponent<GridLayoutGroup>();if(grid==null)return;
            var rect=((RectTransform)transform).rect;
            grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.spacing=new Vector2(8,8);grid.childAlignment=TextAnchor.UpperCenter;
            int rows=Mathf.Max(1,Mathf.CeilToInt(transform.childCount/2f));
            grid.cellSize=new Vector2(Mathf.Max(40,(rect.width-8)*.5f),Mathf.Max(34,Mathf.Min(86,(rect.height-8*(rows-1))/rows)));
        }
    }
}
