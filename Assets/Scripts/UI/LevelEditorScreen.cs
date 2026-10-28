using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace TreasureRunner
{
    public class LevelEditorScreen:MonoBehaviour
    {
        public static CommunityLevel Working;
        GameObject canvasOwner;
        readonly LinkedList<string> undo=new LinkedList<string>(),redo=new LinkedList<string>();Button undoButton,redoButton;InputField titleInput,authorInput;RectTransform root,grid,modal;Text notice,toolText,pageText;EditorLevelScene scene;Button panButton;
        int tool,page,slot,paletteCategory,palettePage;string paletteQuery="";BuilderAsset selectedAsset;Text properties;RawImage brushPreview;
        public static void Open()=>OpenDocument(null);
        public static void OpenDocument(CommunityLevel value)
        {
            if(FindFirstObjectByType<LevelEditorScreen>()!=null)return;
            Working=value==null ? LevelCodec.Empty() : LevelCodec.Decode(LevelCodec.Encode(value));
            new GameObject("Level Editor").AddComponent<LevelEditorScreen>().Build();
        }
        public static InputField Input(Transform parent,string value,int limit,int height=55)
        {
            var r=GameUI.Panel("Eingabe",parent,new Color(.13f,.24f,.27f));r.gameObject.AddComponent<LayoutElement>().preferredHeight=height;
            var field=r.gameObject.AddComponent<InputField>();var text=GameUI.Text(value,r,22);GameUI.Stretch(text.rectTransform,12,6,12,6);text.supportRichText=false;text.verticalOverflow=VerticalWrapMode.Truncate;
            field.textComponent=text;field.characterLimit=limit;field.text=value;return field;
        }
        void Build()
        {
            selectedAsset=BuilderPalette.Basic(LevelTile.Ground);slot=-1;
            root=GameUI.Canvas("Levelbaukasten");canvasOwner=root.parent.gameObject;canvasOwner.GetComponent<Canvas>().sortingOrder=45;
            var scaler=canvasOwner.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            grid=GameUI.Rect("Leere Welt · Bauansicht",root);GameUI.Stretch(grid);
            scene=grid.gameObject.AddComponent<EditorLevelScene>();scene.Initialize();scene.paint=Paint;
            var gear=FloatingButton("Bauteile",18,Palette);ReferenceMenuSkin.Icon(gear.transform,"settings",.15f,.15f,.7f,.7f);
            var functions=FloatingButton("Funktionen",92,()=>ToolMenu(0));
            for(int line=0;line<3;line++){var bar=FantasyMenu.Region(functions.transform,"Menülinie",.24f,.27f+line*.19f,.52f,.06f);bar.gameObject.AddComponent<Image>().color=GameUI.Gold;bar.GetComponent<Image>().raycastTarget=false;}
            toolText=FantasyMenu.Label(root,"",17,.14f,.925f,.8f,.055f,GameUI.Gold);
            notice=FantasyMenu.Label(root,"Tippen/ziehen: Bauen · Zwei Finger: Ansicht bewegen/zoomen · Zum Weiterbauen an den Rand ziehen",16,.02f,.015f,.96f,.045f,Color.white);
            foreach(var text in new[]{toolText,notice}){var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);}
            Refresh();
        }
        Button FloatingButton(string name,float x,UnityEngine.Events.UnityAction action)
        {
            var button=GameUI.Button("",root,action);button.name=name;var rect=(RectTransform)button.transform;
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-18);rect.sizeDelta=new Vector2(62,62);return button;
        }
        public void ToolMenu(int tab)
        {
            var c=Dialog("FUNKTIONEN");var tabs=GameUI.Row(c,44);
            string[] titles={"BAUEN","ANSICHT","LEVEL"};for(int i=0;i<3;i++){int current=i;GameUI.Button(titles[i],tabs,()=>ToolMenu(current),true,tab==i);}
            if(tab==0)
            {
                GameUI.Text("Bauteil: "+selectedAsset.title,c,23,GameUI.Gold);
                var row=GameUI.Row(c,55);GameUI.Button("Bauteil wählen",row,Palette);
                GameUI.Button("Start setzen",row,()=>SelectTool(14));GameUI.Button("Radierer",row,()=>SelectTool(15));
                row=GameUI.Row(c,55);GameUI.Button("Bauen",row,()=>{scene.PanMode=false;CloseModal();Refresh();});
                panButton=GameUI.Button("Hand",row,()=>{scene.PanMode=true;CloseModal();Refresh();});
                row=GameUI.Row(c,55);undoButton=GameUI.Button("Rückgängig",row,()=>{Undo();ToolMenu(0);},undo.Count>0);redoButton=GameUI.Button("Wiederholen",row,()=>{Redo();ToolMenu(0);},redo.Count>0);
                row=GameUI.Row(c,55);GameUI.Button(scene.Simulation ? "Simulation: an" : "Simulation: aus",row,()=>{scene.SetSimulation(!scene.Simulation);scene.PanMode=false;CloseModal();Refresh();Say(scene.Simulation ? "Simulation läuft. Du kannst weiterhin Bauteile setzen und löschen." : "Simulation angehalten. Bauteile stehen wieder an ihren Baupositionen.");});
                GameUI.Button("Neu starten",row,()=>{scene.ResetSimulation();CloseModal();});
                GameUI.WrappedText("Simulation und Bauen sind unabhängig. Bauteile bleiben auf ihrem Rasterplatz; Bewegung ist eine Vorschau. Neue Auswahl beendet den Handmodus.",c,20,GameUI.Muted);
            }
            else if(tab==1)
            {
                GameUI.WrappedText("Mit zwei Fingern in alle Richtungen schieben und zoomen. Rechts-/Mittelklick am PC verschiebt die Ansicht. Beim Bauen am Rand bleibt die Ansicht in Bewegung.",c,20,GameUI.Muted);
                var row=GameUI.Row(c,50);
                GameUI.Button("← Links",row,()=>MoveView(-4,0));GameUI.Button("↑ Hoch",row,()=>MoveView(0,4));GameUI.Button("↓ Runter",row,()=>MoveView(0,-4));GameUI.Button("Rechts →",row,()=>MoveView(4,0));
                row=GameUI.Row(c,50);GameUI.Button("− Zoom",row,()=>{scene.Zoom(1.2f);CloseModal();});GameUI.Button("+ Zoom",row,()=>{scene.Zoom(1/1.2f);CloseModal();});GameUI.Button("Ansicht",row,()=>{scene.ResetView();CloseModal();});
                row=GameUI.Row(c,50);GameUI.Button("< Abschnitt",row,()=>{page=Mathf.Max(0,page-1);Refresh();CloseModal();});pageText=GameUI.Text("Bereich "+(page+1)+" / "+Mathf.CeilToInt(Working.width/16f),row,20);
                GameUI.Button("Abschnitt >",row,()=>{page=Mathf.Min(Mathf.CeilToInt(Working.width/16f)-1,page+1);Refresh();CloseModal();});GameUI.Button("Level einrichten",c,LevelSettings);
            }
            else
            {
                var row=GameUI.Row(c,50);titleInput=Input(row,Working.title,40);authorInput=Input(row,Working.author,24);
                titleInput.onEndEdit.AddListener(v=>{if(v==Working.title)return;Snapshot();Working.title=v;Refresh();});authorInput.onEndEdit.AddListener(v=>{if(v==Working.author)return;Snapshot();Working.author=v;Refresh();});
                row=GameUI.Row(c,50);GameUI.Button("Speichern",row,SaveDocument);GameUI.Button("Bauplatz",row,Slots);GameUI.Button("Testspielen",row,Play,true,true);
                row=GameUI.Row(c,50);GameUI.Button("Teilen",row,Share);GameUI.Button("Importieren",row,()=>{CloseModal();ImportDialog(root,LoadDocument);});GameUI.Button("Einreichen",row,Submit);
                row=GameUI.Row(c,50);GameUI.Button("Level einrichten",row,LevelSettings);GameUI.Button("Neues leeres Level",row,()=>{var confirm=Dialog("Neues Level beginnen?");GameUI.WrappedText("Ungespeicherte Änderungen werden verworfen. Gespeicherte Bauplätze bleiben erhalten.",confirm,22);GameUI.Button("Leere Welt öffnen",confirm,()=>{LoadDocument(LevelCodec.Empty(Working.world));slot=-1;CloseModal();});GameUI.Button("Abbrechen",confirm,()=>ToolMenu(2));});
                row=GameUI.Row(c,50);GameUI.Button(DesktopTools.Active ? "Beenden" : "Menü",row,Close);GameUI.Button("Credits",row,CreditsPage.Open);if(DesktopTools.Active)GameUI.Button("Dateiordner",row,DesktopTools.OpenFolder);
                GameUI.WrappedText("Beim neuen Öffnen startest du in einer leeren Welt. Speichere deine eigenen Welten in einem Bauplatz; vorhandene Level lädst du dort gezielt.",c,19,GameUI.Muted);
            }
            GameUI.Button("Zurück zum Bauen",c,CloseModal).GetComponent<LayoutElement>().preferredHeight=45;
        }
        void SelectTool(int value){tool=value;scene.PanMode=false;CloseModal();Refresh();}
        void MoveView(float x,float y){scene.PanBy(new Vector2(x,y));CloseModal();}
        void CloseModal(){scene?.CancelInteraction();if(modal!=null){modal.gameObject.SetActive(false);Destroy(modal.gameObject);modal=null;}}
        void LevelSettings()
        {
            var c=Dialog("LEVEL EINRICHTEN · bis zu 24 Abschnitte");
            GameUI.Text(Working.width+" × "+Working.height+" Felder · "+Working.tiles.Count+" / "+LevelCodec.MaxTiles+" Bauteile · "+GameConfig.WorldNames[Working.world]+" · Fassung "+Working.revision,c,22,GameUI.Gold);
            var row=GameUI.Row(c,50);
            GameUI.Button("Breite +16",row,()=>{if(Working.width>=LevelCodec.MaxWidth)return;Snapshot();Working.width=Mathf.Min(LevelCodec.MaxWidth,Working.width+16);Working.format=LevelCodec.RequiredFormat(Working);Refresh();LevelSettings();},Working.width<LevelCodec.MaxWidth);
            GameUI.Button("Grundkulisse",row,()=>{Snapshot();Working.world=(Working.world+1)%8;Refresh();LevelSettings();});
            GameUI.Button("Version +1",row,()=>{if(Working.revision>=10000)return;Snapshot();Working.revision++;Refresh();LevelSettings();});
            var heightRow=GameUI.Row(c,42);GameUI.Button("Höhe +4",heightRow,()=>{if(Working.height>=LevelCodec.MaxHeight)return;Snapshot();Working.height=Mathf.Min(LevelCodec.MaxHeight,Working.height+4);Working.format=LevelCodec.RequiredFormat(Working);Refresh();LevelSettings();},Working.height<LevelCodec.MaxHeight);
            GameUI.Text("Abschnitt direkt öffnen",c,22);
            int pages=Mathf.CeilToInt(Working.width/16f);
            for(int n=0;n<pages;n+=8){var r=GameUI.Row(c,42);for(int j=n;j<Mathf.Min(n+8,pages);j++){int target=j;GameUI.Button("Abschnitt "+(j+1),r,()=>{page=target;Destroy(modal.gameObject);Refresh();});}}
            GameUI.Text("Mehr als 6 Abschnitte benötigen die nächste passende Spielausgabe. Bestehende Level und alle Bauteile bleiben erhalten.",c,18,GameUI.Muted);
            GameUI.Button("Zurück zum Bauen",c,()=>Destroy(modal.gameObject)).GetComponent<LayoutElement>().preferredHeight=45;
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
            Working=LevelCodec.Decode(LevelCodec.Encode(value));undo.Clear();redo.Clear();page=0;if(root!=null){Refresh();scene.ResetView();}
        }
        void Paint(int x,int y)
        {
            if(x<0 || x>=Working.width || y<0 || y>=Working.height)return;
            var old=Working.tiles.Find(t=>t.x==x && t.y==y);
            string asset=selectedAsset.id.StartsWith("base.") ? "" : selectedAsset.id;
            if(tool==15 && old==null)return;
            if(tool==14 && Working.spawnX==x && Working.spawnY==y && old!=null && LevelCodec.Floor(old.kind))return;
            if(tool<14 && old!=null && old.kind==selectedAsset.kind && (old.asset??"")==asset)return;
            bool needsNewCell=tool!=15 && old==null;
            int removesGoal=tool==(int)LevelTile.Goal ? Working.tiles.Count(t=>t.kind==LevelTile.Goal) : 0;
            if(needsNewCell && Working.tiles.Count-removesGoal>=LevelCodec.MaxTiles){Say("Maximal "+LevelCodec.MaxTiles+" Bauteile.");return;}
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
            Working.format=LevelCodec.RequiredFormat(Working);
            Refresh();
        }
        void Refresh()
        {
            page=Mathf.Clamp(page,0,Mathf.CeilToInt(Working.width/16f)-1);
            if(titleInput!=null)titleInput.SetTextWithoutNotify(Working.title);if(authorInput!=null)authorInput.SetTextWithoutNotify(Working.author);if(undoButton!=null)undoButton.interactable=undo.Count>0;if(redoButton!=null)redoButton.interactable=redo.Count>0;
            toolText.text=(scene.PanMode ? "Hand · " : "")+(tool==14 ? "Start" : tool==15 ? "Radierer" : selectedAsset.title)+" · Simulation "+(scene.Simulation ? "an" : "aus")+(EditorTestSession.Passed(Working) ? " · Test bestanden ✓" : "");if(pageText!=null)pageText.text="Bereich "+(page+1)+" / "+Mathf.CeilToInt(Working.width/16f);
            scene.Synchronize(Working,page,new LevelBlock{kind=selectedAsset.kind,asset=selectedAsset.id.StartsWith("base.") ? "" : selectedAsset.id},tool);
            if(properties!=null){properties.text=selectedAsset.title+(selectedAsset.title==selectedAsset.category ? "" : "\n"+selectedAsset.category);brushPreview.texture=EditorTilePreview.Get(new LevelBlock{kind=selectedAsset.kind,asset=selectedAsset.id.StartsWith("base.") ? "" : selectedAsset.id},Working.world);}
        }
        RectTransform Dialog(string title)
        {
            CloseModal();modal=GameUI.Panel("Dialog",root,new Color(.035f,.075f,.1f,1));GameUI.Stretch(modal,60,30,60,30);
            var column=GameUI.Column(modal,8,18);GameUI.Stretch(column);GameUI.Text(title,column,28,GameUI.Gold);return column;
        }
        void Palette()
        {
            CloseModal();modal=GameUI.Panel("Dialog",root,new Color(.025f,.065f,.085f,1));GameUI.Stretch(modal,22,18,22,18);
            FantasyMenu.Label(modal,"BAUTEILE · alle Welten frei kombinieren",25,.02f,.91f,.72f,.07f,GameUI.Gold);
            FantasyMenu.Action(modal,"Schließen",CloseModal,.80f,.91f,.18f,.07f);
            var categories=GameUI.Column(FantasyMenu.Region(modal,"Kategorien",.015f,.06f,.19f,.82f),5,6);GameUI.Stretch(categories);
            GameUI.Text("ALLE WELTEN",categories,18,GameUI.Gold);
            for(int i=0;i<BuilderPalette.Categories.Length;i++){int category=i;GameUI.Button(BuilderPalette.Categories[i],categories,()=>{paletteCategory=category;palettePage=0;paletteQuery="";Palette();},true,i==paletteCategory).GetComponent<LayoutElement>().preferredHeight=43;}
            var c=GameUI.Column(FantasyMenu.Region(modal,"Bauteilliste",.225f,.055f,.755f,.83f),7,4);GameUI.Stretch(c);
            var search=Input(c,paletteQuery,40,40);var hint=GameUI.Text("Bauteile suchen …",search.transform,18,GameUI.Muted);GameUI.Stretch(hint.rectTransform,12,6,12,6);search.placeholder=hint;search.onEndEdit.AddListener(q=>{paletteQuery=q;palettePage=0;Palette();});
            var items=BuilderPalette.Entries.Where(a=>a.category==BuilderPalette.Categories[paletteCategory] && (string.IsNullOrWhiteSpace(paletteQuery) || a.title.IndexOf(paletteQuery,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            int pages=Mathf.Max(1,Mathf.CeilToInt(items.Length/8f));palettePage=Mathf.Clamp(palettePage,0,pages-1);
            for(int line=0;line<2;line++)
            {
                var row=GameUI.Row(c,120);
                for(int col=0;col<4;col++)
                {
                    int i=palettePage*8+line*4+col;if(i>=items.Length)break;var item=items[i];
                    var button=GameUI.Button(item.title,row,()=>{selectedAsset=item;tool=(int)item.kind;scene.PanMode=false;CloseModal();Refresh();});
                    var holder=GameUI.Rect("Bildrahmen",button.transform);holder.anchorMin=new Vector2(.05f,.32f);holder.anchorMax=new Vector2(.95f,.98f);holder.offsetMin=holder.offsetMax=Vector2.zero;
                    var art=GameUI.Rect("Bauteilvorschau",holder);GameUI.Stretch(art);var ratio=art.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.FitInParent;ratio.aspectRatio=1;
                    var image=art.gameObject.AddComponent<RawImage>();image.texture=EditorTilePreview.Get(new LevelBlock{kind=item.kind,asset=item.id.StartsWith("base.") ? "" : item.id},Working.world);image.raycastTarget=false;
                    var label=button.GetComponentInChildren<Text>();label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=new Vector2(1,.31f);label.rectTransform.offsetMin=new Vector2(4,2);label.rectTransform.offsetMax=new Vector2(-4,-2);label.fontSize=16;label.supportRichText=false;label.resizeTextForBestFit=true;label.resizeTextMinSize=12;label.resizeTextMaxSize=16;label.verticalOverflow=VerticalWrapMode.Truncate;
                }
            }
            var navigation=GameUI.Row(c,42);GameUI.Button("< Zurück",navigation,()=>{palettePage--;Palette();},palettePage>0);
            GameUI.Text(items.Length+" Bauteile · "+(palettePage+1)+" / "+pages,navigation,18,GameUI.Gold);GameUI.Button("Weitere >",navigation,()=>{palettePage++;Palette();},palettePage+1<pages);
            GameUI.Button("Gesamte Assetbibliothek · Bilder und Audio",c,()=>{CloseModal();LibraryMediaBrowser.Open(item=>{selectedAsset=item;tool=(int)item.kind;scene.PanMode=false;Refresh();});}).GetComponent<LayoutElement>().preferredHeight=43;
        }
        void Slots()=>ShowSlots(false);
        void SaveDocument()=>ShowSlots(true);
        void ShowSlots(bool saving)
        {
            var c=Dialog(saving ? "SPEICHERN · Bauplatz wählen" : "BAUPLATZ · gespeichertes Level laden");
            for(int row=0;row<4;row++)
            {
                var r=GameUI.Row(c,65);for(int col=0;col<2;col++)
                {
                    int index=row*2+col;var level=LocalLevels.Load(index);
                    GameUI.Button((index+1)+" · "+(level==null ? "Leer" : level.title),r,()=>{
                        if(!saving){if(level==null){Say("Dieser Bauplatz ist leer.");CloseModal();return;}slot=index;LoadDocument(level);CloseModal();return;}
                        if(level!=null && level.id!=Working.id){var confirm=Dialog("Bauplatz "+(index+1)+" überschreiben?");GameUI.WrappedText("Gespeichertes Level: "+level.title+". Die vorherige Fassung bleibt als lokale Sicherung erhalten.",confirm,23);GameUI.Button("Überschreiben",confirm,()=>SaveTo(index));GameUI.Button("Abbrechen",confirm,()=>ShowSlots(true));}
                        else SaveTo(index);
                    });
                }
            }
            if(!saving)GameUI.Button("Letzten Testentwurf laden",c,()=>{var draft=LocalLevels.LoadDraft();if(draft==null){Say("Kein Testentwurf vorhanden.");CloseModal();return;}slot=-1;LoadDocument(draft);CloseModal();});
            GameUI.Button("Zurück zum Bauen",c,CloseModal).GetComponent<LayoutElement>().preferredHeight=45;
        }
        void SaveTo(int index)
        {
            try{LocalLevels.Save(Working,index);slot=index;CloseModal();Say("In Bauplatz "+(slot+1)+" gespeichert.");}catch(Exception){Say("Speichern fehlgeschlagen.");}
        }
        void Say(string s)=>notice.text=s;
        bool Valid(){if(LevelCodec.Validate(Working,out string why))return true;Say(why);return false;}
        void Play(){if(!Valid())return;try{LocalLevels.SaveDraft(Working);}catch(Exception){Say("Testentwurf konnte nicht gesichert werden.");return;}EditorTestSession.Begin(Working);LevelRun.StartCustom(Working);}
        void Share(){if(!Valid())return;LocalLevels.Share(Working);Say(Working.format>=4 ? "Level-Code kopiert. Für diese Bauteile benötigen andere TreasureRunner ab "+ReleaseInfo.Version+"." : Working.format>=3 ? "Level-Code kopiert. Neue Bauteile benötigen Spiel oder Werkzeuge ab 0.8.0." : "Level-Code kopiert. Andere wählen Community → Level-Code importieren.");}
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
            GameUI.Text("Level-Code oder GitHub-Einreichungslink",c,28,GameUI.Gold);var field=Input(c,"",LevelCodec.MaxCodeLength,220);field.lineType=InputField.LineType.MultiLineNewline;
            var status=GameUI.Text("Es werden ausschließlich Level-Bauteile importiert.",c,22);GameUI.Button("Aus Zwischenablage",c,()=>{SharedLinkInput.Paste(field,out var message);status.text=message;});
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
