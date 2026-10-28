from pathlib import Path
p=Path('Assets/Scripts/UI/LevelEditorScreen.cs');s=p.read_text(encoding='utf-8-sig')
a=s.index('        void Palette()');b=s.index('        void Slots()=>',a)
s=s[:a]+'''        void Palette()
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
                    var label=button.GetComponentInChildren<Text>();label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=new Vector2(1,.31f);label.rectTransform.offsetMin=new Vector2(4,2);label.rectTransform.offsetMax=new Vector2(-4,-2);label.fontSize=16;label.supportRichText=false;
                }
            }
            var navigation=GameUI.Row(c,42);GameUI.Button("< Zurück",navigation,()=>{palettePage--;Palette();},palettePage>0);
            GameUI.Text(items.Length+" Bauteile · "+(palettePage+1)+" / "+pages,navigation,18,GameUI.Gold);GameUI.Button("Weitere >",navigation,()=>{palettePage++;Palette();},palettePage+1<pages);
            GameUI.Button("Gesamte Assetbibliothek · Bilder und Audio",c,()=>{CloseModal();LibraryMediaBrowser.Open(item=>{selectedAsset=item;tool=(int)item.kind;scene.PanMode=false;Refresh();});}).GetComponent<LayoutElement>().preferredHeight=43;
        }
''' + s[b:]
p.write_text(s,encoding='utf-8')
