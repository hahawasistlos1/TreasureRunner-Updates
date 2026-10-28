using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class LibraryMediaBrowser:MonoBehaviour
    {
        RectTransform root,content;GameObject owner;AudioSource preview;
        int mode,page;string query="";Action<BuilderAsset> choose;
        public static void Open(Action<BuilderAsset> select=null)
        {var browser=new GameObject("Assetbibliothek").AddComponent<LibraryMediaBrowser>();browser.choose=select;browser.Build();}
        void Build()
        {
            root=GameUI.Canvas("Gemeinsame Assetbibliothek");owner=root.parent.gameObject;owner.GetComponent<Canvas>().sortingOrder=88;
            var bg=GameUI.Panel("Bibliothek",root,GameUI.Ink);GameUI.Stretch(bg);
            var header=GameUI.Column(FantasyMenu.Region(bg,"Kopf",.03f,.77f,.94f,.21f),6);
            GameUI.Stretch(header);
            GameUI.Text("ASSETBIBLIOTHEK · "+AssetLibrary.Data.packs.Length+" Pakete",header,27,GameUI.Gold);
            var row=GameUI.Row(header,48);for(int i=0;i<3;i++){int tab=i;GameUI.Button(new[]{"Bauteile","Bilder & Menüdesign","Audio"}[i],row,()=>{mode=tab;page=0;Draw();});}
            var search=LevelEditorScreen.Input(header,"",80,42);search.onEndEdit.AddListener(value=>{query=value.Trim();page=0;Draw();});
            content=GameUI.Scroll(FantasyMenu.Region(bg,"Inhalte",.03f,.14f,.94f,.61f));
            var nav=GameUI.Row(FantasyMenu.Region(bg,"Navigation",.03f,.025f,.94f,.095f),52);
            GameUI.Stretch(nav);
            GameUI.Button("<",nav,()=>{page=Mathf.Max(0,page-1);Draw();});GameUI.Button(">",nav,()=>{page++;Draw();});
            GameUI.Button("Menüdesign zurücksetzen",nav,()=>{ProgressStore.Data.libraryButton="";ProgressStore.Data.libraryPanel="";ProgressStore.Save();});
            GameUI.Button("Schließen",nav,()=>{Destroy(owner);Destroy(gameObject);});
            preview=gameObject.AddComponent<AudioSource>();Draw();
        }
        bool Match(string value)=>string.IsNullOrEmpty(query) || value.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0;
        void Draw()
        {
            preview?.Stop();foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            if(mode==0)
            {
                var all=BuilderPalette.Entries.Where(a=>Match(a.title+" "+a.id+" "+a.category)).ToArray();page=Mathf.Clamp(page,0,Mathf.Max(0,(all.Length-1)/12));
                GameUI.Text(all.Length+" Bauteile · Seite "+(page+1)+" · unabhängig von der Welt",content,21,GameUI.Gold);
                foreach(var item in all.Skip(page*12).Take(12))
                {
                    var row=PreviewRow(90);Picture(row,EditorTilePreview.Get(new LevelBlock{kind=item.kind,asset=item.id},0),90);
                    Description(row,item.title+"\n"+item.category);if(choose!=null)FixedButton("Bauen",row,()=>{choose(item);Destroy(owner);Destroy(gameObject);});
                }
            }
            else if(mode==1)
            {
                var all=AssetLibrary.Data.images.Where(i=>Match(i.title+" "+i.pack)).ToArray();page=Mathf.Clamp(page,0,Mathf.Max(0,(all.Length-1)/12));
                GameUI.Text(all.Length+" Bilder · Seite "+(page+1)+" · Suche auch nach Paketnamen",content,21,GameUI.Gold);
                foreach(var item in all.Skip(page*12).Take(12))
                {
                    var row=PreviewRow(100);Picture(row,Resources.Load<Texture2D>(item.resource),120);
                    Description(row,item.title+"\n"+item.pack);
                    if(item.use=="ui" && (item.title.Contains("button") || item.title.Contains("Button")))FixedButton("Als Button",row,()=>{ProgressStore.Data.libraryButton=item.resource;ProgressStore.Save();});
                    if(item.use=="ui" && (item.title.Contains("panel") || item.title.Contains("Panel")))FixedButton("Als Rahmen",row,()=>{ProgressStore.Data.libraryPanel=item.resource;ProgressStore.Save();});
                }
            }
            else
            {
                var all=AssetLibrary.Data.audio.Where(a=>Match(a.title+" "+a.pack)).ToArray();page=Mathf.Clamp(page,0,Mathf.Max(0,(all.Length-1)/12));
                GameUI.Text(all.Length+" Audioclips · Seite "+(page+1),content,21,GameUI.Gold);
                foreach(var item in all.Skip(page*12).Take(12))
                {var row=PreviewRow(65);Description(row,item.title+" · "+item.pack);FixedButton("Anhören",row,()=>{preview.Stop();preview.volume=ProgressStore.Data.muted ? 0 : ProgressStore.Data.effectsVolume;preview.clip=Resources.Load<AudioClip>(item.resource);preview.Play();});}
            }
        }
        RectTransform PreviewRow(float height){var row=GameUI.Row(content,height);row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;return row;}
        void Description(Transform row,string text){var label=GameUI.Text(text,row,19);var layout=label.GetComponent<LayoutElement>();layout.preferredWidth=200;layout.flexibleWidth=1;}
        void FixedButton(string label,Transform row,UnityEngine.Events.UnityAction action){var layout=GameUI.Button(label,row,action).GetComponent<LayoutElement>();layout.minWidth=145;layout.preferredWidth=145;layout.flexibleWidth=0;}
        void Picture(Transform row,Texture texture,float width)
        {
            var holder=GameUI.Rect("Vorschau",row);var layout=holder.gameObject.AddComponent<LayoutElement>();layout.minWidth=width;layout.preferredWidth=width;layout.flexibleWidth=0;
            var rect=GameUI.Rect("Originalgrafik",holder);var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;
            var fit=rect.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=texture!=null ? texture.width/(float)Mathf.Max(1,texture.height) : 1;
        }
        void OnDestroy(){if(preview!=null)preview.Stop();if(owner!=null)Destroy(owner);}
    }
}
