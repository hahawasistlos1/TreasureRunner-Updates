using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public class EditorPaintSurface:MonoBehaviour,IPointerDownHandler,IDragHandler
    {
        public Action<int,int> paint;public int rows=12;
        public void OnPointerDown(PointerEventData e)=>Draw(e);
        public void OnDrag(PointerEventData e)=>Draw(e);
        void Draw(PointerEventData e)
        {
            var rect=(RectTransform)transform;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var p))
            {int x=Mathf.FloorToInt((p.x-rect.rect.xMin)/rect.rect.width*16),y=Mathf.FloorToInt((p.y-rect.rect.yMin)/rect.rect.height*rows);if(x>=0 && x<16 && y>=0 && y<rows)paint?.Invoke(x,y);}
        }
    }
    public class LevelEditorScreen:MonoBehaviour
    {
        public static CommunityLevel Working;
        GameObject canvasOwner;
        readonly LinkedList<string> undo=new LinkedList<string>(),redo=new LinkedList<string>();Button undoButton,redoButton;InputField titleInput,authorInput;RectTransform root,grid,modal;Text notice,toolText,pageText;Image[,] cells=new Image[16,16];Text[,] names=new Text[16,16];RawImage[,] previews=new RawImage[16,16];
        int tool,page,slot,paletteCategory,palettePage;string paletteQuery="";BuilderAsset selectedAsset;Text properties;RawImage brushPreview;
        public static void Open(){if(FindFirstObjectByType<LevelEditorScreen>()!=null)return;new GameObject("Level Editor").AddComponent<LevelEditorScreen>().Build();}
        public static InputField Input(Transform parent,string value,int limit,int height=55)
        {
            var r=GameUI.Panel("Eingabe",parent,new Color(.13f,.24f,.27f));r.gameObject.AddComponent<LayoutElement>().preferredHeight=height;
            var field=r.gameObject.AddComponent<InputField>();var text=GameUI.Text(value,r,22);GameUI.Stretch(text.rectTransform,12,6,12,6);text.supportRichText=false;text.verticalOverflow=VerticalWrapMode.Truncate;
            field.textComponent=text;field.characterLimit=limit;field.text=value;return field;
        }
        void Build()
        {
            selectedAsset=BuilderPalette.Basic(LevelTile.Ground);
            if(Working==null)Working=LocalLevels.LoadDraft()??LocalLevels.Load(0)??LevelCodec.Starter();
            root=GameUI.Canvas("Levelbaukasten");canvasOwner=root.parent.gameObject;canvasOwner.GetComponent<Canvas>().sortingOrder=45;
            root=ToolWindowTheme.Workspace(root,"Level-Editor","Bauen · Testspielen · Einreichen","editor");
            var background=GameUI.Panel("Editor",root,new Color(.035f,.075f,.1f,1));GameUI.Stretch(background);
            var header=Region(.9f,.99f);var row=GameUI.Row(header,55);
            GameUI.Button(DesktopTools.Active ? "Beenden" : "Menü",row,Close);titleInput=Input(row,Working.title,40);titleInput.onEndEdit.AddListener(v=>{if(v==Working.title)return;Snapshot();Working.title=v;Refresh();});
            authorInput=Input(row,Working.author,24);authorInput.onEndEdit.AddListener(v=>{if(v==Working.author)return;Snapshot();Working.author=v;Refresh();});
            var tools=Region(.8f,.89f);row=GameUI.Row(tools,55);
            GameUI.Button("Bauteil wählen",row,Palette);toolText=GameUI.Text("",row,21,GameUI.Gold);
            GameUI.Button("Start setzen",row,()=>{tool=14;Refresh();});GameUI.Button("Radierer",row,()=>{tool=15;Refresh();});undoButton=GameUI.Button("Rückgängig",row,Undo);redoButton=GameUI.Button("Wiederholen",row,Redo);
            FitToolbar(row);
            grid=GameUI.Panel("Baufeld",root,new Color(.12f,.19f,.25f));grid.anchorMin=new Vector2(.025f,.26f);grid.anchorMax=new Vector2(.975f,.79f);grid.offsetMin=grid.offsetMax=Vector2.zero;
            grid.gameObject.AddComponent<EditorPaintSurface>().paint=Paint;
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                var c=GameUI.Panel("Feld "+x+","+y,grid,Color.clear);c.anchorMin=new Vector2(x/16f,y/12f);c.anchorMax=new Vector2((x+1)/16f,(y+1)/12f);c.offsetMin=Vector2.one;c.offsetMax=-Vector2.one;
                cells[x,y]=c.GetComponent<Image>();cells[x,y].raycastTarget=false;
                var art=GameUI.Rect("Echtes Bauteil",c);art.anchorMin=Vector2.zero;art.anchorMax=Vector2.one;art.offsetMin=art.offsetMax=Vector2.zero;
                var aspect=art.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectRatio=1;aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                previews[x,y]=art.gameObject.AddComponent<RawImage>();previews[x,y].raycastTarget=false;previews[x,y].enabled=false;
                names[x,y]=GameUI.Text("",c,14);GameUI.Stretch(names[x,y].rectTransform);names[x,y].alignment=TextAnchor.MiddleCenter;
            }
            row=GameUI.Row(Region(.17f,.25f),50);
            GameUI.Button("< Abschnitt",row,()=>{page=Mathf.Max(0,page-1);Refresh();});pageText=GameUI.Text("",row,20);
            GameUI.Button("Abschnitt >",row,()=>{page=Mathf.Min(Mathf.CeilToInt(Working.width/16f)-1,page+1);Refresh();});
            GameUI.Button("Grundkulisse",row,()=>{Snapshot();Working.world=(Working.world+1)%8;Refresh();});
            GameUI.Button("Breite +16",row,()=>{if(Working.width>=96)return;Snapshot();Working.width=Mathf.Min(96,Working.width+16);Refresh();});
            GameUI.Button("Version +1",row,()=>{if(Working.revision>=10000)return;Snapshot();Working.revision++;Refresh();});
            FitToolbar(row);
            row=GameUI.Row(Region(.085f,.165f),50);
            GameUI.Button("Speichern",row,()=>{try{LocalLevels.Save(Working,slot);Say("In Bauplatz "+(slot+1)+" gespeichert.");}catch(Exception){Say("Speichern fehlgeschlagen.");}});
            GameUI.Button("Bauplatz",row,Slots);GameUI.Button("Testspielen",row,Play,true,true);
            if(DesktopTools.Active)GameUI.Button("Dateiordner",row,DesktopTools.OpenFolder);
            GameUI.Button("Teilen",row,Share);GameUI.Button("Importieren",row,()=>ImportDialog(root,l=>LoadDocument(l)));GameUI.Button("Einreichen",row,Submit);
            notice=GameUI.Text("Tippen oder ziehen zum Bauen. Bauteile mit Gegnern oder Fallen enthalten bereits Boden.",root,19,GameUI.Muted);
            notice.rectTransform.anchorMin=new Vector2(.03f,.01f);notice.rectTransform.anchorMax=new Vector2(.97f,.08f);notice.rectTransform.offsetMin=notice.rectTransform.offsetMax=Vector2.zero;Refresh();
            if(DesktopTools.Active)foreach(var button in root.GetComponentsInChildren<Button>())button.GetComponentInChildren<Text>().fontSize=20;
            ArrangeFantasyEditor();Refresh();
        }
        void ArrangeFantasyEditor()
        {
            Button Find(string label)=>root.GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>().text==label);
            var header=(RectTransform)titleInput.transform.parent;
            Find("Speichern").transform.SetParent(header,false);Find("Testspielen").transform.SetParent(header,false);
            header.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;
            foreach(Transform child in header){var l=child.GetComponent<LayoutElement>();l.preferredWidth=child==titleInput.transform || child==authorInput.transform ? 0 : 125;l.flexibleWidth=child==titleInput.transform || child==authorInput.transform ? 1 : 0;var b=child.GetComponent<Button>();if(b!=null)b.GetComponentInChildren<Text>().fontSize=18;}
            var left=FantasyMenu.Card(root,"Bauteilkategorien",.02f,.28f,.13f,.60f);var list=GameUI.Column(left,5,7);GameUI.Stretch(list);
            var choose=Find("Bauteil wählen");choose.transform.SetParent(list,false);choose.GetComponent<LayoutElement>().preferredHeight=43;choose.GetComponentInChildren<Text>().fontSize=18;
            for(int i=0;i<BuilderPalette.Categories.Length;i++){int category=i;var b=GameUI.Button(BuilderPalette.Categories[i],list,()=>{paletteCategory=category;palettePage=0;paletteQuery="";Palette();});b.GetComponent<LayoutElement>().preferredHeight=37;b.GetComponentInChildren<Text>().fontSize=17;}
            var right=FantasyMenu.Card(root,"Bauteileigenschaften",.835f,.28f,.145f,.60f);
            FantasyMenu.Label(right,"BAUTEIL",18,.06f,.88f,.88f,.10f,GameUI.Gold);
            var previewHolder=FantasyMenu.Region(right,"Aktuelles Bauteil",.08f,.58f,.84f,.29f);var preview=GameUI.Rect("Pinsel-Grafik",previewHolder);brushPreview=preview.gameObject.AddComponent<RawImage>();brushPreview.raycastTarget=false;var ratio=preview.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.FitInParent;ratio.aspectRatio=1;
            properties=FantasyMenu.Label(right,"",17,.06f,.39f,.88f,.19f,GameUI.Muted);
            foreach(string name in new[]{"Start setzen","Radierer"}){var b=Find(name);b.transform.SetParent(right,false);var r=(RectTransform)b.transform;r.anchorMin=new Vector2(.06f,name=="Start setzen" ? .23f : .07f);r.anchorMax=new Vector2(.94f,name=="Start setzen" ? .36f : .20f);r.offsetMin=r.offsetMax=Vector2.zero;b.GetComponentInChildren<Text>().fontSize=18;}
            var actions=Find("Bauplatz").transform.parent;undoButton.transform.SetParent(actions,false);redoButton.transform.SetParent(actions,false);
            var tools=(RectTransform)toolText.transform.parent;tools.SetParent(root,false);tools.anchorMin=new Vector2(.165f,.82f);tools.anchorMax=new Vector2(.82f,.89f);tools.offsetMin=tools.offsetMax=Vector2.zero;
            toolText.fontSize=18;toolText.GetComponent<LayoutElement>().preferredHeight=44;
            grid.anchorMin=new Vector2(.165f,.28f);grid.anchorMax=new Vector2(.82f,.815f);
        }
        static void FitToolbar(RectTransform row)
        {
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;
            foreach(Transform child in row)
            {
                var layout=child.GetComponent<LayoutElement>()??child.gameObject.AddComponent<LayoutElement>();
                bool button=child.GetComponent<Button>()!=null;layout.preferredWidth=button ? 150 : 0;layout.flexibleWidth=button ? 0 : 1;
                if(button)child.GetComponentInChildren<Text>().fontSize=20;
            }
        }
        RectTransform Region(float min,float max){var r=GameUI.Column(root,0);r.anchorMin=new Vector2(.025f,min);r.anchorMax=new Vector2(.975f,max);r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        static void Remember(LinkedList<string> history,string value){history.AddLast(value);if(history.Count>60)history.RemoveFirst();}
        void Snapshot(){Remember(undo,LevelCodec.Encode(Working));redo.Clear();}
        void Undo(){if(undo.Count==0)return;Remember(redo,LevelCodec.Encode(Working));Working=LevelCodec.Decode(undo.Last.Value);undo.RemoveLast();Refresh();}
        void Redo(){if(redo.Count==0)return;Remember(undo,LevelCodec.Encode(Working));Working=LevelCodec.Decode(redo.Last.Value);redo.RemoveLast();Refresh();}
        public void LoadDocument(CommunityLevel value)
        {
            Working=LevelCodec.Decode(LevelCodec.Encode(value));undo.Clear();redo.Clear();page=0;if(root!=null)Refresh();
        }
        void Paint(int x,int y)
        {
            x+=page*16;if(x<0 || x>=Working.width || y<0 || y>=Working.height)return;
            var old=Working.tiles.Find(t=>t.x==x && t.y==y);
            string asset=selectedAsset.id.StartsWith("base.") ? "" : selectedAsset.id;
            if(tool==15 && old==null)return;
            if(tool==14 && Working.spawnX==x && Working.spawnY==y && old!=null && LevelCodec.Floor(old.kind))return;
            if(tool<14 && old!=null && old.kind==selectedAsset.kind && (old.asset??"")==asset)return;
            bool needsNewCell=tool!=15 && old==null;
            int removesGoal=tool==(int)LevelTile.Goal ? Working.tiles.Count(t=>t.kind==LevelTile.Goal) : 0;
            if(needsNewCell && Working.tiles.Count-removesGoal>=LevelCodec.MaxTiles){Say("Maximal 650 Bauteile.");return;}
            Snapshot();
            if(tool==14)
            {
                Working.spawnX=x;Working.spawnY=y;
                if(old==null || !LevelCodec.Floor(old.kind)){Working.tiles.RemoveAll(t=>t.x==x && t.y==y);Working.tiles.Add(new LevelBlock{x=x,y=y,kind=LevelTile.Ground});}
            }
            else
            {
                Working.tiles.RemoveAll(t=>t.x==x && t.y==y);
                if(tool<14)
                {
                    if(tool==(int)LevelTile.Goal)Working.tiles.RemoveAll(t=>t.kind==LevelTile.Goal);
                    Working.tiles.Add(new LevelBlock{x=x,y=y,kind=selectedAsset.kind,asset=asset});

                }
            }
            Working.format=Working.tiles.Where(t=>!string.IsNullOrEmpty(t.asset)).Select(t=>BuilderPalette.Get(t.asset)?.format??3).DefaultIfEmpty(1).Max();
            Refresh();
        }
        void Refresh()
        {
            page=Mathf.Clamp(page,0,Mathf.CeilToInt(Working.width/16f)-1);grid.GetComponent<EditorPaintSurface>().rows=Working.height;
            titleInput.SetTextWithoutNotify(Working.title);authorInput.SetTextWithoutNotify(Working.author);undoButton.interactable=undo.Count>0;redoButton.interactable=redo.Count>0;
            toolText.text=(tool==14 ? "Start" : tool==15 ? "Radierer" : selectedAsset.title)+(EditorTestSession.Passed(Working) ? " · Test bestanden ✓" : " · Test optional");pageText.text="Bereich "+(page+1)+" / "+Mathf.CeilToInt(Working.width/16f)+" · "+GameConfig.WorldNames[Working.world]+" · Platz "+(slot+1)+" · v"+Working.revision;
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                var cell=(RectTransform)cells[x,y].transform;cell.gameObject.SetActive(y<Working.height && x+page*16<Working.width);
                cell.anchorMin=new Vector2(x/16f,y/(float)Working.height);cell.anchorMax=new Vector2((x+1)/16f,(y+1)/(float)Working.height);cell.offsetMin=Vector2.one;cell.offsetMax=-Vector2.one;
                cells[x,y].color=(x+y)%2==0 ? new Color(.10f,.18f,.25f) : new Color(.14f,.23f,.30f);names[x,y].text="";previews[x,y].enabled=false;
            }
            foreach(var t in Working.tiles)
            {
                int x=t.x-page*16;if(x<0 || x>=16 || t.y>=Working.height)continue;var preview=previews[x,t.y];
                preview.texture=EditorTilePreview.GetGrid(t,Working.world,out var uv,out bool fill);preview.uvRect=uv;
                var fit=preview.GetComponent<AspectRatioFitter>();fit.enabled=!fill;if(fill)GameUI.Stretch(preview.rectTransform);
                preview.enabled=true;
            }
            int sx=Working.spawnX-page*16;if(sx>=0 && sx<16 && Working.spawnY<Working.height)names[sx,Working.spawnY].text="START";
            if(properties!=null){properties.text=selectedAsset.title+(selectedAsset.title==selectedAsset.category ? "" : "\n"+selectedAsset.category);brushPreview.texture=EditorTilePreview.Get(new LevelBlock{kind=selectedAsset.kind,asset=selectedAsset.id.StartsWith("base.") ? "" : selectedAsset.id},Working.world);}
        }
        RectTransform Dialog(string title)
        {
            if(modal!=null)Destroy(modal.gameObject);modal=GameUI.Panel("Dialog",root,new Color(.035f,.075f,.1f,1));GameUI.Stretch(modal,60,30,60,30);
            var column=GameUI.Column(modal,8,18);GameUI.Stretch(column);GameUI.Text(title,column,28,GameUI.Gold);return column;
        }
        void Palette()
        {
            var c=Dialog("BAUTEILE · alle Welten frei kombinieren");
            for(int line=0;line<2;line++)
            {
                var tabs=GameUI.Row(c,44);
                for(int n=0;n<4;n++){int category=line*4+n;GameUI.Button(BuilderPalette.Categories[category],tabs,()=>{paletteCategory=category;palettePage=0;Palette();},true,category==paletteCategory).GetComponentInChildren<Text>().fontSize=20;}
            }
            var search=Input(c,paletteQuery,40,40);var hint=GameUI.Text("Bauteile suchen …",search.transform,20,GameUI.Muted);GameUI.Stretch(hint.rectTransform,12,6,12,6);search.placeholder=hint;search.onEndEdit.AddListener(q=>{paletteQuery=q;palettePage=0;Palette();});
            var items=BuilderPalette.Entries.Where(a=>a.category==BuilderPalette.Categories[paletteCategory] && (string.IsNullOrWhiteSpace(paletteQuery) || a.title.IndexOf(paletteQuery,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/8f));palettePage=Mathf.Clamp(palettePage,0,pages-1);
            for(int line=0;line<2;line++)
            {
                var row=GameUI.Row(c,105);
                for(int col=0;col<4;col++)
                {
                    int i=palettePage*8+line*4+col;if(i>=items.Length)break;var item=items[i];
                    var button=GameUI.Button(item.title,row,()=>{selectedAsset=item;tool=(int)item.kind;Destroy(modal.gameObject);Refresh();});
                    var holder=GameUI.Rect("Bildrahmen",button.transform);holder.anchorMin=new Vector2(.02f,.1f);holder.anchorMax=new Vector2(.31f,.9f);holder.offsetMin=holder.offsetMax=Vector2.zero;
                    var art=GameUI.Rect("Bauteilvorschau",holder);GameUI.Stretch(art);
                    var aspect=art.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=1;
                    var image=art.gameObject.AddComponent<RawImage>();image.texture=EditorTilePreview.Get(new LevelBlock{kind=item.kind,asset=item.id.StartsWith("base.") ? "" : item.id},Working.world);image.raycastTarget=false;
                    var label=button.GetComponentInChildren<Text>();label.rectTransform.anchorMin=new Vector2(.34f,0);label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(2,5);label.rectTransform.offsetMax=new Vector2(-5,-5);label.fontSize=18;label.supportRichText=false;
                }
            }
            var navigation=GameUI.Row(c,45);GameUI.Button("< Zurück",navigation,()=>{palettePage--;Palette();},palettePage>0);
            GameUI.Text(items.Length+" Bauteile · "+(palettePage+1)+" / "+pages,navigation,20,GameUI.Gold);
            GameUI.Button("Weitere >",navigation,()=>{palettePage++;Palette();},palettePage+1<pages);
            GameUI.Text("Alle Kategorien sind in jeder Welt verfügbar. Suche: Namen eingeben und Enter drücken. Deko steht im Hintergrund.",c,18,GameUI.Muted);
            GameUI.Button("Schließen",c,()=>Destroy(modal.gameObject));
        }
        void Slots()
        {
            var c=Dialog("Acht eigene Bauplätze");
            for(int row=0;row<4;row++)
            {var r=GameUI.Row(c,75);for(int col=0;col<2;col++){int index=row*2+col;var l=LocalLevels.Load(index);GameUI.Button((index+1)+" · "+(l==null ? "Leer – neues Level" : l.title),r,()=>{slot=index;LoadDocument(LocalLevels.Load(slot)??LevelCodec.Starter());Destroy(modal.gameObject);});}}
            GameUI.Button("Zurück",c,()=>Destroy(modal.gameObject));
        }
        void Say(string s)=>notice.text=s;
        bool Valid(){if(LevelCodec.Validate(Working,out string why))return true;Say(why);return false;}
        void Play(){if(!Valid())return;try{LocalLevels.SaveDraft(Working);}catch(Exception){Say("Testentwurf konnte nicht gesichert werden.");return;}EditorTestSession.Begin(Working);LevelRun.StartCustom(Working);}
        void Share(){if(!Valid())return;LocalLevels.Share(Working);Say(Working.format>=3 ? "Level-Code kopiert. Neue Bauteile benötigen Spiel oder Werkzeuge ab 0.8.0." : "Level-Code kopiert. Andere wählen Community → Level-Code importieren.");}
        void Submit()
        {
            if(!Valid())return;
            if(!LevelModeration.CanPublish(Working)){Say("Diese neuen Bauteile sind noch ein lokaler Entwicklungsstand. Öffentliche Freigabe erst mit der nächsten ausdrücklich veröffentlichten Spielversion.");return;}
            var c=Dialog("Level zur Freigabe einreichen");
            GameUI.Text("Dein Level wird mit Titel und Autorenname öffentlich auf GitHub eingereicht. Ein GitHub-Konto ist dafür nötig. Nach Prüfung kann es in der Community-Auswahl erscheinen. Zum direkten Teilen genügt der Level-Code.",c,24).GetComponent<LayoutElement>().preferredHeight=180;
            GameUI.Button("GitHub-Einreichung öffnen",c,()=>{
                string body="Bitte dieses TreasureRunner-Level prüfen und für den Community-Katalog freigeben.\n\n"+LevelCodec.Encode(Working);
                if(body.Length>5500){Say("Für die Einreichung bitte den kopierten Level-Code in das GitHub-Formular einfügen.");GUIUtility.systemCopyBuffer=body;body="Mein Level-Code ist in der Zwischenablage. Bitte hier einfügen.";}
                Application.OpenURL("https://github.com/hahawasistlos1/TreasureRunner-Updates/issues/new?title="+Uri.EscapeDataString("[Level] "+Working.title)+"&body="+Uri.EscapeDataString(body));Destroy(modal.gameObject);
            },true,true);GameUI.Button("Zurück",c,()=>Destroy(modal.gameObject));
        }
        public static void ImportDialog(Transform parent,Action<CommunityLevel> accept)
        {
            var r=GameUI.Panel("Level importieren",parent,GameUI.Ink);GameUI.Stretch(r,40,30,40,30);var c=GameUI.Column(r,12,24);GameUI.Stretch(c);
            GameUI.Text("Level-Code oder GitHub-Einreichungslink",c,28,GameUI.Gold);var field=Input(c,"",100000,220);field.lineType=InputField.LineType.MultiLineNewline;
            GameUI.Button("Aus Zwischenablage",c,()=>field.text=GUIUtility.systemCopyBuffer);var status=GameUI.Text("Es werden ausschließlich Level-Bauteile importiert.",c,22);
            var importer=r.gameObject.AddComponent<CommunityIssueImporter>();
            GameUI.Button("Importieren",c,()=>{
                if(CommunityIssueImporter.IsReference(field.text,out int number)){importer.Fetch(number,l=>{accept(l);Destroy(r.gameObject);},s=>status.text=s);return;}
                try{var l=LevelCodec.Decode(field.text);if(!LevelCodec.Validate(l,out var why)){status.text=why;return;}accept(l);Destroy(r.gameObject);}catch(Exception){status.text="Ungültiger Level-Code. GitHub-Link: Repository → Issues → deine Einreichung.";}
            },true,true);
            GameUI.Button("Abbrechen",c,()=>Destroy(r.gameObject));
        }
        void Close(){if(DesktopTools.Active){Application.Quit();return;}Destroy(canvasOwner);Destroy(gameObject);}
        void OnDestroy(){EditorTilePreview.Clear();}
    }
}
