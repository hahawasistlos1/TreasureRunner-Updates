using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class LibraryTheme
    {
        public static void Control(Transform parent,string name)
        {
            var entry=AssetLibrary.Data.images.FirstOrDefault(i=>i.pack=="kenney-controls" && i.title.Replace(' ','_')==name);
            if(entry!=null)Overlay(parent,entry.resource,"Kenney-Steuerung",false);
        }
        public static void Button(Button button)
        {if(button!=null && !string.IsNullOrEmpty(ProgressStore.Data.libraryButton))Overlay(button.transform,ProgressStore.Data.libraryButton,"Gewähltes Button-Asset",true);}
        public static void Panel(Image panel)
        {if(panel!=null && !string.IsNullOrEmpty(ProgressStore.Data.libraryPanel))Overlay(panel.transform,ProgressStore.Data.libraryPanel,"Gewählter Asset-Rahmen",true);}
        static void Overlay(Transform parent,string resource,string title,bool sliced)
        {
            if(parent.Find(title)!=null)return;var texture=Resources.Load<Texture2D>(resource);if(texture==null)return;
            var rect=GameUI.Rect(title,parent);GameUI.Stretch(rect);rect.SetAsFirstSibling();var image=rect.gameObject.AddComponent<Image>();image.raycastTarget=false;
            float border=sliced ? Mathf.Min(16,Mathf.Min(texture.width,texture.height)/4f) : 0;
            var sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*border);
            image.sprite=sprite;image.type=sliced ? Image.Type.Sliced : Image.Type.Simple;rect.gameObject.AddComponent<LibrarySpriteOwner>().sprite=sprite;
        }
    }
    public class LibrarySpriteOwner:MonoBehaviour
    {
        public Sprite sprite;void OnDestroy(){if(sprite!=null)Destroy(sprite);}
    }
}
