using UnityEngine;
namespace TreasureRunner
{
    public class SafeAreaLayout : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int previousSize;
        private void Update()
        {
            Rect safe = Screen.safeArea;
            var size = new Vector2Int(Screen.width,Screen.height);
            if (safe == previous && size == previousSize) return;
            previous = safe; previousSize = size;
            if (Screen.width == 0 || Screen.height == 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);
            rect.anchorMax = new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
