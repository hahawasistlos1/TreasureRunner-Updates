using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class MenuDashboard
    {
        public static string Time(float seconds)=>seconds>0 ? System.TimeSpan.FromSeconds(seconds).ToString(@"m\:ss\.f") : "—";
        public static int Completed(int world)=>Enumerable.Range(0,10).Count(s=>ProgressStore.Data.FindLevel(CampaignCatalog.Id(world,s))?.completions>0);
        public static int Unlocked(int world)=>Enumerable.Range(0,10).Count(s=>CampaignCatalog.Unlocked(CampaignCatalog.Id(world,s)));
        static void WorldArt(Transform parent,int world,float x,float y,float w,float h)
        {var clip=FantasyMenu.Region(parent,"Weltvorschau",x,y,w,h);clip.gameObject.AddComponent<RectMask2D>();FantasyMenu.Art(clip,"MenuArt/World-"+world,0,0,1,1);}
        public static void Build(RectTransform parent,MainMenuController menu)
        {
            var data=ProgressStore.Data;int id=MainMenuController.ContinueLevel,world=CampaignCatalog.World(id),stage=CampaignCatalog.Stage(id);var record=data.FindLevel(id);
            var hero=ReferenceMenuSkin.Panel(parent,"Aktuelles Abenteuer",0,369f/746,1004f/1326,377f/746);
            var clip=FantasyMenu.Region(hero,"Abenteuerillustration",.002f,.006f,.996f,.988f);clip.gameObject.AddComponent<RectMask2D>();
            if(world==0){var texture=Resources.Load<Texture2D>("MenuArt/Reference/Adventure");if(texture!=null){var image=clip.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;}else ReferenceMenuSkin.Crop(clip,new Rect(332,296,700,264),0,0,1,1,"Abenteuerillustration aus Vorlage");}
            else WorldArt(clip,world,0,0,1,1);
            // Preserve the exact visible UI-free scene from the user's reference; reconstructed art fills only the covered surroundings.
            if(world==0)ReferenceMenuSkin.Crop(hero,new Rect(332,295,700,266),.002f,.006f,.700f,.707f,"Originale Waldszene");
            var title=ReferenceMenuSkin.Panel(hero,"Abenteuername",.002f,.715f,.996f,.28f,new Color(.015f,.055f,.075f,.83f));
            ReferenceMenuSkin.Label(title,"Weiter spielen",44,.025f,.46f,.79f,.50f,ReferenceMenuSkin.Gold,true);
            ReferenceMenuSkin.Label(title,GameConfig.Current.LevelName(id),28,.025f,.055f,.82f,.42f,null,true);
            var level=ReferenceMenuSkin.Panel(hero,"Aktueller Level",.855f,.85f,.128f,.095f,new Color(.09f,.15f,.18f,.95f));ReferenceMenuSkin.Label(level,"Level "+(stage+1)+" / 10",19,.06f,.04f,.88f,.92f).alignment=TextAnchor.MiddleCenter;
            var status=ReferenceMenuSkin.Panel(hero,"Abenteuerfortschritt",.704f,.035f,.279f,.695f,new Color(.018f,.05f,.065f,.97f));
            ReferenceMenuSkin.Label(status,"Fortschritt",22,.065f,.865f,.87f,.12f);
            ReferenceMenuSkin.Progress(status,Completed(world)/10f,.065f,.785f,.69f,.064f);ReferenceMenuSkin.Label(status,Completed(world)+" / 10",19,.76f,.76f,.20f,.11f).alignment=TextAnchor.MiddleRight;
            string[] keys={"coin","skull","clock"};string[] labels={"Gesammelte Münzen","Besiegte Gegner","Beste Zeit"};
            string[] values={record?.hasDetailedStats==true ? record.mostCoins.ToString() : "—",record?.hasDetailedStats==true ? record.mostEnemies.ToString() : "—",Time(record?.bestTime??0)};
            for(int i=0;i<3;i++){float y=.59f-i*.13f;ReferenceMenuSkin.Icon(status,keys[i],.075f,y,.09f,.10f);ReferenceMenuSkin.Label(status,labels[i],14,.195f,y,.59f,.11f);var value=ReferenceMenuSkin.Label(status,values[i],17,.76f,y,.19f,.11f);value.alignment=TextAnchor.MiddleRight;}
            var play=ReferenceMenuSkin.Button(status,"Level spielen",()=>LevelRun.StartLevel(id),.026f,.035f,.95f,.28f,true,29);FantasyMenu.Region(play.transform,"Startsymbol",.045f,.25f,.10f,.48f).gameObject.AddComponent<ReferencePlayGlyph>();play.GetComponentInChildren<Text>().rectTransform.anchorMin=new Vector2(.15f,.05f);
            var player=ReferenceMenuSkin.Panel(parent,"Charakterübersicht",1024f/1326,369f/746,302f/1326,377f/746);
            var portrait=ReferenceMenuSkin.Panel(player,"Porträtrahmen",.025f,.657f,.325f,.32f,new Color(.005f,.017f,.026f));var preview=FantasyMenu.Region(portrait,"Charaktervorschau",.04f,.03f,.92f,.94f);preview.gameObject.AddComponent<RawImage>();preview.gameObject.AddComponent<CharacterPreview>().zoom=1.28f;
            ReferenceMenuSkin.Label(player,data.playerName,21,.39f,.88f,.57f,.09f,null,true);ReferenceMenuSkin.Label(player,CharacterCatalog.Current.title,18,.39f,.79f,.57f,.09f);ReferenceMenuSkin.Label(player,GameConfig.Current.EquippedWeapon.title,17,.39f,.705f,.57f,.09f);
            int finished=data.levels.Count(r=>r.completions>0);ReferenceMenuSkin.Progress(player,finished/80f,.39f,.665f,.57f,.045f);ReferenceMenuSkin.Label(player,finished+" / 80 Level",14,.40f,.665f,.54f,.05f).alignment=TextAnchor.MiddleCenter;
            var config=GameConfig.Current;string[] stats={"Leben","Tempo","Sprungkraft","Angriff","Verteidigung"};string[] statIcons={"life","speed","jump","attack","defense"};
            for(int i=0;i<5;i++){float y=.523f-i*.092f;ReferenceMenuSkin.Icon(player,statIcons[i],.038f,y,.095f,.077f);ReferenceMenuSkin.Label(player,stats[i],20,.177f,y,.58f,.085f);ReferenceMenuSkin.Label(player,data.upgrades[i]+" / "+config.upgrades[i].maxLevel,20,.77f,y,.18f,.085f).alignment=TextAnchor.MiddleRight;}
            ReferenceMenuSkin.Button(player,"Charakter ändern",()=>menu.Show("Charakter"),.025f,.026f,.95f,.10f,false,19);
            var editName=ReferenceMenuSkin.Button(player,"",()=>menu.Show("Profil"),.88f,.895f,.085f,.065f);ReferenceMenuSkin.Icon(editName.transform,"editor",0,0,1,1);
            string[] names={"Welten","Shop","Waffen & Inventar","Level-Editor","Community"},targets={"Levelauswahl","Shop","Inventar","Level-Editor","Community"},symbols={"world","shop","weapons","editor","community"},buttons={"Welt wählen","Shop öffnen","Ausrüsten","Editor öffnen","Community"};
            string[] descriptions={"Acht bunte Welten\nmit 80 Leveln","Upgrades, Leben,\nTempo, Sprungkraft\nund mehr","Ausrüstung, Waffen\nund Gegenstände","Eigene Level bauen\nund einreichen","Level von anderen\nSchatzbauern spielen"};
            Rect[] art={new Rect(337,624,228,83),new Rect(588,624,226,83),new Rect(839,624,239,83),new Rect(1106,624,266,83),new Rect(1404,624,240,83)};
            float[] starts={0,252,504,771,1062},widths={242,242,256,281,264};
            for(int i=0;i<5;i++){
                string target=targets[i];var card=ReferenceMenuSkin.Panel(parent,names[i],starts[i]/1326,118f/746,widths[i]/1326,242f/746);
                ReferenceMenuSkin.Icon(card,symbols[i],.055f,.815f,.14f,.14f);ReferenceMenuSkin.Label(card,names[i],22,.245f,.805f,.73f,.18f,null,true);
                var artClip=FantasyMenu.Region(card,"Kartenillustration",.032f,.426f,.936f,.368f);artClip.gameObject.AddComponent<RectMask2D>();ReferenceMenuSkin.Crop(artClip,art[i],0,0,1,1,"Originales Kartenbild");
                ReferenceMenuSkin.Label(card,descriptions[i],17,.105f,.19f,.86f,.24f);ReferenceMenuSkin.Button(card,buttons[i],()=>menu.Show(target),.052f,.055f,.90f,.145f,false,18);
            }
            var records=ReferenceMenuSkin.Panel(parent,"Rekorde",0,0,522f/1326,107f/746);ReferenceMenuSkin.Icon(records,"records",.035f,.44f,.075f,.43f);ReferenceMenuSkin.Label(records,"Rekorde",20,.16f,.63f,.79f,.26f,ReferenceMenuSkin.Gold,true);
            var best=data.levels.Where(r=>r.completions>0).OrderBy(r=>r.id).Take(2).ToArray();for(int i=0;i<best.Length;i++){var r=best[i];ReferenceMenuSkin.Label(records,GameConfig.Current.LevelName(r.id),15,.16f,.37f-i*.20f,.45f,.22f);ReferenceMenuSkin.Label(records,r.bestScore.ToString("N0")+" Pkt. · "+Time(r.bestTime),15,.61f,.37f-i*.20f,.37f,.22f);}
            if(best.Length==0)ReferenceMenuSkin.Label(records,"Dein erstes Abenteuer wartet.",16,.16f,.28f,.78f,.25f,GameUI.Muted);
            ReferenceMenuSkin.Label(records,"Gesamtbestwert: "+data.levels.Sum(r=>(long)r.bestScore).ToString("N0")+" Punkte",16,.16f,.015f,.79f,.21f,ReferenceMenuSkin.Gold);records.gameObject.AddComponent<Button>().onClick.AddListener(()=>menu.Show("Rangliste"));
            var friends=ReferenceMenuSkin.Panel(parent,"Freunde",533f/1326,0,422f/1326,107f/746);ReferenceMenuSkin.Icon(friends,"friends",.035f,.48f,.11f,.36f);ReferenceMenuSkin.Label(friends,"Freunde",20,.19f,.63f,.75f,.26f,ReferenceMenuSkin.Gold,true);ReferenceMenuSkin.Label(friends,"Gemeinsam auf Schatzsuche",16,.19f,.33f,.77f,.25f);ReferenceMenuSkin.Label(friends,FriendInvites.Contacts.Count+" lokale Kontakte · kein Online-Status",15,.19f,.06f,.77f,.25f,GameUI.Muted);friends.gameObject.AddComponent<Button>().onClick.AddListener(()=>menu.Show("Freunde"));
            var updates=ReferenceMenuSkin.Panel(parent,"Updates",966f/1326,0,360f/1326,107f/746);ReferenceMenuSkin.Icon(updates,"updates",.04f,.51f,.11f,.33f);ReferenceMenuSkin.Label(updates,"Updates",20,.20f,.63f,.75f,.26f,ReferenceMenuSkin.Gold,true);ReferenceMenuSkin.Label(updates,"Spielversion "+Application.version,16,.20f,.36f,.77f,.25f);
            ReferenceMenuSkin.Button(updates,"Nach Updates suchen",()=>{menu.Show("Updates");GameUpdateClient.Instance?.CheckNow();PatchClient.Instance?.CheckNow();},.20f,.075f,.75f,.29f,true,17);
        }
        public static void WorldMap(Transform page,MainMenuController menu)
        {
            GameUI.WrappedText("Deine Reise durch acht Welten · wähle eine Welt für ihre zehn Level.",page,24,GameUI.Muted);
            for(int row=0;row<4;row++){
                var strip=GameUI.Row(page,180,18);for(int col=0;col<2;col++){
                    int world=row*2+col;var card=GameUI.Panel("Kartenwelt "+world,strip,ReferenceMenuSkin.Surface);card.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;WorldArt(card,world,.01f,.04f,.4f,.92f);
                    ReferenceMenuSkin.Label(card,(world+1)+" · "+GameConfig.WorldNames[world],24,.44f,.61f,.53f,.29f,ReferenceMenuSkin.Gold,true);ReferenceMenuSkin.Label(card,Completed(world)+" / 10 abgeschlossen",19,.44f,.36f,.53f,.22f);
                    ReferenceMenuSkin.Button(card,"Welt erkunden",()=>menu.ShowWorld(world),.44f,.06f,.53f,.25f);
                }
            }
        }
        public static void Worlds(Transform page,MainMenuController menu)
        {
            for(int row=0;row<4;row++)
            {var r=GameUI.Row(page,245,16);r.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;
                for(int col=0;col<2;col++)
                {int world=row*2+col;var c=GameUI.Panel(GameConfig.WorldNames[world],r,FantasyMenu.Surface);var layout=c.gameObject.AddComponent<LayoutElement>();layout.preferredWidth=0;layout.flexibleWidth=1;FantasyMenu.Frame(c.GetComponent<Image>());WorldArt(c,world,.01f,.36f,.98f,.63f);
                    var badge=FantasyMenu.Card(c,"Weltname",.025f,.76f,.95f,.20f);FantasyMenu.Label(badge,GameConfig.WorldNames[world],25,.04f,0,.92f,1,GameUI.Gold,true);
                    FantasyMenu.Label(c,Unlocked(world)+" / 10 freigeschaltet · "+Completed(world)+" / 10 abgeschlossen",18,.035f,.23f,.93f,.12f,GameUI.Muted);FantasyMenu.Bar(c,Completed(world)/10f,.035f,.18f,.93f,.035f);
                    FantasyMenu.Action(c,"Welt ansehen",()=>menu.ShowWorld(world),.035f,.035f,.93f,.12f);}
            }
        }
        public static void Stages(Transform page,MainMenuController menu,int world)
        {
            GameUI.Button("Alle Welten",page,()=>menu.Show("Levelauswahl"));
            for(int s=0;s<10;s++)
            {int id=CampaignCatalog.Id(world,s);var record=ProgressStore.Data.FindLevel(id);bool unlocked=CampaignCatalog.Unlocked(id);
                var c=GameUI.Panel("Level "+id,page,FantasyMenu.Surface);c.gameObject.AddComponent<LayoutElement>().preferredHeight=166;FantasyMenu.Frame(c.GetComponent<Image>(),record?.completions>0);WorldArt(c,world,.008f,.04f,.25f,.92f);
                FantasyMenu.Label(c,(s+1)+" · "+CampaignCatalog.Name(id),24,.28f,.61f,.49f,.29f,unlocked ? GameUI.Gold : GameUI.Muted);
                FantasyMenu.Label(c,CampaignCatalog.Style(s)+" · "+(unlocked ? record?.completions>0 ? "Abgeschlossen" : "Freigeschaltet" : "Gesperrt")+"\nBestscore "+(record?.bestScore??0)+" · Beste Zeit "+Time(record?.bestTime??0),19,.28f,.15f,.49f,.44f,GameUI.Muted);
                FantasyMenu.Action(c,unlocked ? "Auswählen" : "Gesperrt",()=>menu.SelectLevel(id),.79f,.54f,.19f,.32f,unlocked);
                FantasyMenu.Action(c,"Spielen",()=>{MainMenuController.SelectedLevel=id;LevelRun.StartLevel(id);},.79f,.12f,.19f,.32f,unlocked,true);
            }
        }
    }
}
