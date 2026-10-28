using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TreasureRunner
{
    public static class GameUI
    {
        public static readonly Color Ink = new Color(.018f,.045f,.065f,.98f);
        public static readonly Color Card = new Color(.035f,.10f,.14f,.96f);
        public static readonly Color Gold = new Color(1f,.76f,.32f);
        public static readonly Color Muted = new Color(.62f,.75f,.75f);
        private static Font font;
        public static Font Font => font != null ? font : (font = Resources.Load<Font>("Fonts/Lato")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }
        public static void Stretch(RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left,bottom); rect.offsetMax = new Vector2(-right,-top);
        }
        public static RectTransform Canvas(string name)
        {
            RectTransform root = Rect(name, null);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720);
            scaler.matchWidthOrHeight = .5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var safe = Rect("SafeArea", root);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaLayout>();
            return safe;
        }
        public static RectTransform Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<ReferencePanel>(); image.borderWidth=0;image.color = color;
            return rect;
        }
        public static Text Text(string value, Transform parent, int size = 24, Color? color = null)
        {
            var rect = Rect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Font; text.fontSize = size; text.color = color ?? Color.white;
            text.text = value; text.supportRichText = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = size * 1.55f;
            return text;
        }
        public static Text WrappedText(string value,Transform parent,int size=24,Color? color=null)
        {
            var text=Text(value,parent,size,color);text.GetComponent<LayoutElement>().preferredHeight=-1;return text;
        }
        public static Button Button(string label, Transform parent, UnityAction action, bool enabled = true, bool accent = false)
        {
            var rect = Panel("Button " + label, parent, accent ? FantasyMenu.Green : new Color(.075f,.23f,.28f));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
            var button = rect.gameObject.AddComponent<Button>();
            button.interactable = enabled;
            button.onClick.AddListener(action);
            button.onClick.AddListener(()=>{GameAudio.Ensure();GameAudio.Instance?.Play("click");});
            rect.gameObject.AddComponent<MenuButtonMotion>();
            FantasyMenu.Frame(button.image,accent);
            var colors = button.colors;
            colors.highlightedColor = new Color(.8f,.96f,.85f);
            colors.pressedColor = new Color(.55f,.8f,.7f);
            colors.disabledColor = new Color(.3f,.35f,.35f,.6f);
            button.colors = colors;
            var text = Text(label, rect, 22, Color.white);
            Stretch(text.rectTransform, 12,5,12,5);
            text.alignment = TextAnchor.MiddleCenter;
            LibraryTheme.Button(button);
            return button;
        }
        public static RectTransform Column(Transform parent, float spacing = 14, int padding = 0)
        {
            var rect = Rect("Column", parent);
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing; group.padding = new RectOffset(padding,padding,padding,padding);
            group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = false;
            return rect;
        }
        public static RectTransform Row(Transform parent, float height = 80, float spacing = 16)
        {
            var rect = Rect("Row", parent);
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing; group.childControlWidth = true; group.childControlHeight = true;
            group.childForceExpandWidth = true; group.childForceExpandHeight = true;
            return rect;
        }
        public static RectTransform Scroll(Transform parent)
        {
            var root = Panel("Scroll", parent, new Color(0,0,0,.01f)); Stretch(root);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            var view = Rect("Viewport",root); Stretch(view);
            view.gameObject.AddComponent<RectMask2D>();
            var content = Column(view, 16, 8);
            content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f,1); content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = view; scroll.content = content;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }
        public static void Clear(Transform parent)
        {
            foreach (Transform child in parent) { child.gameObject.SetActive(false); Object.Destroy(child.gameObject); }
        }
    }
}
