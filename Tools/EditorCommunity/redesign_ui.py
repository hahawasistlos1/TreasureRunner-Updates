from pathlib import Path
p=Path('Assets/Scripts/UI/LevelEditorScreen.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public static void Open(){if(FindFirstObjectByType<LevelEditorScreen>()!=null)return;new GameObject("Level Editor").AddComponent<LevelEditorScreen>().Build();}', '''public static void Open()=>OpenDocument(null);
        public static void OpenDocument(CommunityLevel value)
        {
            if(FindFirstObjectByType<LevelEditorScreen>()!=null)return;
            Working=value==null ? LevelCodec.Empty() : LevelCodec.Decode(LevelCodec.Encode(value));
            new GameObject("Level Editor").AddComponent<LevelEditorScreen>().Build();
        }''')
a=s.index('        void Build()');b=s.index('        void LevelSettings()',a)
s=s[:a]+'''        void Build()
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
''' + s[b:]
s=s.replace('Working.width+" Felder · "+Working.tiles.Count','Working.width+" × "+Working.height+" Felder · "+Working.tiles.Count')
s=s.replace('GameUI.Text("Abschnitt direkt öffnen",c,22);','''var heightRow=GameUI.Row(c,42);GameUI.Button("Höhe +4",heightRow,()=>{if(Working.height>=LevelCodec.MaxHeight)return;Snapshot();Working.height=Mathf.Min(LevelCodec.MaxHeight,Working.height+4);Working.format=LevelCodec.RequiredFormat(Working);Refresh();LevelSettings();},Working.height<LevelCodec.MaxHeight);
            GameUI.Text("Abschnitt direkt öffnen",c,22);''')
s=s.replace('if(root!=null)Refresh();','if(root!=null){Refresh();scene.ResetView();}')
s=s.replace('titleInput.SetTextWithoutNotify(Working.title);authorInput.SetTextWithoutNotify(Working.author);undoButton.interactable=undo.Count>0;redoButton.interactable=redo.Count>0;', 'if(titleInput!=null)titleInput.SetTextWithoutNotify(Working.title);if(authorInput!=null)authorInput.SetTextWithoutNotify(Working.author);if(undoButton!=null)undoButton.interactable=undo.Count>0;if(redoButton!=null)redoButton.interactable=redo.Count>0;')
old='toolText.text=(tool==14 ? "Start" : tool==15 ? "Radierer" : selectedAsset.title)+(EditorTestSession.Passed(Working) ? " · Test bestanden ✓" : " · Test optional");pageText.text="Bereich "+(page+1)+" / "+Mathf.CeilToInt(Working.width/16f);'
new='toolText.text=(scene.PanMode ? "Hand · " : "")+(tool==14 ? "Start" : tool==15 ? "Radierer" : selectedAsset.title)+" · Simulation "+(scene.Simulation ? "an" : "aus")+(EditorTestSession.Passed(Working) ? " · Test bestanden ✓" : "");if(pageText!=null)pageText.text="Bereich "+(page+1)+" / "+Mathf.CeilToInt(Working.width/16f);'
assert old in s;s=s.replace(old,new)
s=s.replace('scene?.CancelInteraction();if(modal!=null)Destroy(modal.gameObject);modal=','CloseModal();modal=')
s=s.replace('selectedAsset=item;tool=(int)item.kind;Destroy(modal.gameObject);Refresh();','selectedAsset=item;tool=(int)item.kind;scene.PanMode=false;CloseModal();Refresh();')
s=s.replace('selectedAsset=item;tool=(int)item.kind;Refresh();','selectedAsset=item;tool=(int)item.kind;scene.PanMode=false;Refresh();')
a=s.index('        void Slots()');b=s.index('        void Say(',a)
s=s[:a]+'''        void Slots()=>ShowSlots(false);
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
''' + s[b:]
p.write_text(s,encoding='utf-8')
