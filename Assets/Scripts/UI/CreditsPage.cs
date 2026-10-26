using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class CreditsPage
    {
        public static void Open()
        {
            var root=GameUI.Canvas("Credits");root.parent.GetComponent<Canvas>().sortingOrder=85;
            var bg=GameUI.Panel("Credits Hintergrund",root,new Color(.035f,.075f,.10f,1));GameUI.Stretch(bg);
            var frame=GameUI.Rect("Urheber",root);GameUI.Stretch(frame,55,100,55,25);
            Build(GameUI.Scroll(frame));
            var close=GameUI.Button("Zurück",root,()=>Object.Destroy(root.parent.gameObject));
            var r=(RectTransform)close.transform;r.anchorMin=new Vector2(.35f,.025f);r.anchorMax=new Vector2(.65f,.11f);r.offsetMin=r.offsetMax=Vector2.zero;
        }
        public static void Build(Transform parent)
        {
            GameUI.Text("DANKE AN DIE ASSET-KÜNSTLER",parent,30,GameUI.Gold);
            Entry(parent,"Kay Lousberg · KayKit","Skeletons Character Pack und Dungeon Remastered: spielbare Skelettfiguren, Gegner, Ausrüstung und Requisiten. CC0 1.0.","https://kaylousberg.com","OpenArt/Skeletons/LICENSE");
            Entry(parent,"Kenney","Nature Kit: Bäume, Felsen, Pflanzen und Kulissen. Particle Pack: Treffer-, Rauch-, Feuer- und Magietexturen. CC0 1.0.","https://kenney.nl/assets","OpenArt/Nature/License");
            Entry(parent,"Kenney · Platformer Kit","Baublöcke, bewegliche Plattform, Münzen, Säge, Stacheln, Sprungfeder und weitere Bau-Requisiten. CC0 1.0.","https://kenney.nl/assets/platformer-kit","OpenArt/Platformer/License");
            Entry(parent,"KayKit · Dungeon Remastered","Kay Lousberg · Dungeon-Bauteile und Requisiten. CC0 1.0. Vollständiger Lizenztext dieses Pakets.","https://kaylousberg.itch.io/kaykit-dungeon-remastered","OpenArt/Dungeon/LICENSE");
            Entry(parent,"Kenney · Particle Pack","Treffer-, Rauch-, Feuer- und Magietexturen. CC0 1.0. Vollständiger Lizenztext dieses Pakets.","https://kenney.nl/assets/particle-pack","OpenArt/Particles/License");
            Entry(parent,"Cinzel · The Cinzel Project Authors","Fantasy-Überschriften. SIL Open Font License 1.1, Copyright 2020. Unveränderte Schriftdatei.","https://github.com/google/fonts/tree/main/ofl/cinzel","Fonts/Cinzel-License");
            Entry(parent,"Lato · Łukasz Dziedzic / tyPoland","Leseschrift. SIL Open Font License 1.1, Copyright 2010–2014. Unveränderte Schriftdatei; reservierter Schriftname Lato.","https://github.com/google/fonts/tree/main/ofl/lato","Fonts/Lato-License");
            Entry(parent,"Standout 7","LOWPO · Low-Poly Goblin Pack (Free Sample). Spieler- und Goblinmodell. Standard Unity Asset Store EULA.","https://assetstore.unity.com/packages/3d/characters/creatures/lowpo-low-poly-goblin-pack-free-sample-332200");
            Entry(parent,"DM Dungeon Mason","RPG Monster Buddy PBR Polyart, vorhandenes Monsterpaket des Projekts. Standard Unity Asset Store EULA.","https://assetstore.unity.com/packages/3d/characters/creatures/rpg-monster-buddy-pbr-polyart-253961");
            Entry(parent,"AngeloMaN87","Natural Environment (Mobile), vorhandene Naturbauteile des Projekts. Standard Unity Asset Store EULA.","https://assetstore.unity.com/packages/3d/environments/landscapes/natural-environment-mobile-324098");
            GameUI.Text("TreasureRunner",parent,28,GameUI.Gold);
            GameUI.Text("Spielkonzept und Projekt: hahawasistlos1. Programmierung, eigene Comic-Bauteile und prozedurale Musik: mit Codex entwickelt. Menüvorlage, Logo, Symbole und Kartenbilder: vom Nutzer bereitgestellt und freigegeben. Abenteuerillustration aus der Vorlage mit OpenAI-Bildgenerierung aufbereitet; App-Icon und weitere Menülandschaft ebenfalls damit erstellt. Weitere Weltvorschauen stammen aus dem Spiel. Community-Level nennen ihre jeweiligen Autoren.",parent,23,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=190;
            GameUI.Text("CC0-Pakete werden freiwillig genannt. Fremde Modelle werden nicht als eigene Arbeiten ausgegeben. Die vollständigen Lizenztexte liegen den Windows-Werkzeugen unter Lizenzen bei.",parent,22,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=100;
        }
        static void Entry(Transform parent,string name,string description,string url,string license=null)
        {
            var card=GameUI.Panel(name,parent,FantasyMenu.Surface);card.gameObject.AddComponent<LayoutElement>().preferredHeight=215;FantasyMenu.Frame(card.GetComponent<Image>());var column=GameUI.Column(card,7,14);GameUI.Stretch(column);parent=column;
            GameUI.Text(name,parent,27,GameUI.Gold);
            GameUI.Text(description,parent,21,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=85;
            var row=GameUI.Row(parent,55);GameUI.Button("Originalquelle",row,()=>Application.OpenURL(url));
            if(license!=null)GameUI.Button("Lizenz lesen",row,()=>{
                var text=Resources.Load<TextAsset>(license);if(text==null)return;
                var r=GameUI.Canvas("Lizenztext");r.parent.GetComponent<Canvas>().sortingOrder=90;
                var b=GameUI.Panel("Lizenz",r,GameUI.Ink);GameUI.Stretch(b);var frame=GameUI.Rect("Text",r);GameUI.Stretch(frame,45,110,45,25);
                var content=GameUI.Scroll(frame);var t=GameUI.Text(text.text,content,23);t.GetComponent<LayoutElement>().preferredHeight=Mathf.Max(800,text.text.Split('\n').Length*34);
                var close=GameUI.Button("Zurück",r,()=>Object.Destroy(r.parent.gameObject));var rect=(RectTransform)close.transform;rect.anchorMin=new Vector2(.35f,.025f);rect.anchorMax=new Vector2(.65f,.11f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            });
            else GameUI.Button("Lizenz (Unity EULA)",row,()=>Application.OpenURL("https://unity.com/legal/as-terms"));
        }
    }
}
