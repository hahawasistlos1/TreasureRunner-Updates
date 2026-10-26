using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class MainMenuController:MonoBehaviour
    {
        RectTransform menuRoot,page,body,sidebar;Text heading,wallet,badges,message,updateStatus;Button homeButton;string currentPage="Start";float noticeUntil;
        public static int SelectedLevel=-1;
        public static int ContinueLevel { get {if(SelectedLevel>=0 && CampaignCatalog.Unlocked(SelectedLevel))return SelectedLevel;return Mathf.Clamp(ProgressStore.Data.highestUnlockedLevel,0,23);} }
        readonly System.Collections.Generic.Dictionary<string,Button> navigation=new System.Collections.Generic.Dictionary<string,Button>();
        public void Build()
        {
            menuRoot=GameUI.Canvas("Main Menu");var scaler=menuRoot.parent.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1672,941);scaler.matchWidthOrHeight=1;
            ReferenceMenuSkin.Background(menuRoot);
            var top=ReferenceMenuSkin.Place(menuRoot,"Hauptleiste",453,109,891,66);ReferenceMenuSkin.Panel(top,"Navigationsfläche",0,0,1,1);
            homeButton=ReferenceMenuSkin.Button(top,"Home",()=>Show("Start"),.01f,.1f,.192f,.80f,false,22);ReferenceMenuSkin.Icon(homeButton.transform,"home",.065f,.18f,.22f,.63f);var homeText=homeButton.GetComponentInChildren<Text>();homeText.rectTransform.anchorMin=new Vector2(.31f,.05f);homeText.rectTransform.anchorMax=new Vector2(.96f,.95f);
            var levels=ReferenceMenuSkin.Button(top,"Levelauswahl",()=>Show("Levelauswahl"),.216f,.1f,.248f,.80f,false,21);ReferenceMenuSkin.Icon(levels.transform,"levels",.025f,.18f,.19f,.63f);levels.GetComponentInChildren<Text>().rectTransform.anchorMin=new Vector2(.23f,.05f);
            var map=ReferenceMenuSkin.Button(top,"Weltkarte",()=>Show("Weltkarte"),.472f,.1f,.265f,.80f,false,21);ReferenceMenuSkin.Icon(map.transform,"world",.025f,.18f,.16f,.63f);map.GetComponentInChildren<Text>().rectTransform.anchorMin=new Vector2(.19f,.05f);
            ReferenceMenuSkin.Icon(top,"coin",.795f,.19f,.048f,.62f);wallet=ReferenceMenuSkin.Label(top,"",23,.85f,.17f,.076f,.66f,null,true);wallet.alignment=TextAnchor.MiddleCenter;
            ReferenceMenuSkin.Button(top,"+",()=>Show("Shop"),.941f,.19f,.045f,.60f,true,28);
            var achievements=ReferenceMenuSkin.Place(menuRoot,"Schatzabzeichen",1353,109,218,66);ReferenceMenuSkin.Panel(achievements,"Erfolge",0,0,1,1);ReferenceMenuSkin.Icon(achievements,"gem",.055f,.20f,.19f,.58f);
            badges=ReferenceMenuSkin.Label(achievements,"",23,.35f,.03f,.38f,.50f,null,true);badges.alignment=TextAnchor.MiddleCenter;ReferenceMenuSkin.Label(achievements,"Erfolge",13,.34f,.52f,.41f,.29f,GameUI.Muted).alignment=TextAnchor.MiddleCenter;
            ReferenceMenuSkin.Button(achievements,"+",()=>Show("Erfolge"),.745f,.19f,.18f,.60f,true,28);
            var settings=ReferenceMenuSkin.Place(menuRoot,"Einstellungen öffnen",1582,109,73,66);var settingsButton=ReferenceMenuSkin.Button(settings,"",()=>Show("Einstellungen"),0,0,1,1);ReferenceMenuSkin.Icon(settingsButton.transform,"settings",.20f,.16f,.60f,.68f);
            sidebar=ReferenceMenuSkin.Place(menuRoot,"Navigation",29,166,277,768);
            string[] names={"Start","Welten","Shop","Waffen","Inventar","Charakter","Level-Editor","Community","Freunde","Rekorde","Einstellungen","Profil","Updates","Credits"};
            string[] targets={"Start","Levelauswahl","Shop","Waffen","Inventar","Charakter","Level-Editor","Community","Freunde","Rangliste","Einstellungen","Profil","Updates","Credits"};
            string[] hints={"Abenteuer starten","Levelübersicht","Upgrades & Items","Ausrüstung","Deine Gegenstände","Aussehen & Skins","Eigene Level bauen","Level von Spielern","Freundesliste","Highscores","Audio, Grafik, Steuerung","Deine Statistiken","Nach Updates suchen","Mitwirkende & Lizenzen"};
            string[] icons={"play","world","shop","weapons","inventory","character","editor","community","friends","records","settings","profile","updates","credits"};
            for(int i=0;i<names.Length;i++){
                string target=targets[i];float height=1/14f,y=1-(i+1)*height;
                var b=ReferenceMenuSkin.Button(sidebar,names[i],()=>Show(target),0,y+.002f,1,height-.004f,false,19);b.name="Navigation "+names[i];
                var label=b.GetComponentInChildren<Text>();label.rectTransform.anchorMin=new Vector2(.31f,.39f);label.rectTransform.anchorMax=new Vector2(.97f,.96f);label.alignment=TextAnchor.MiddleLeft;
                ReferenceMenuSkin.Label(b.transform,hints[i],15,.31f,.04f,.67f,.38f,new Color(.72f,.82f,.88f));ReferenceMenuSkin.Icon(b.transform,icons[i],.065f,.18f,.155f,.66f);navigation[target]=b;
            }
            body=ReferenceMenuSkin.Place(menuRoot,"Seiteninhalt",328,187,1326,746);
            var toast=ReferenceMenuSkin.Place(menuRoot,"Menühinweis",350,887,920,45);ReferenceMenuSkin.Panel(toast,"Hinweisfläche",0,0,1,1).GetComponent<Image>().raycastTarget=false;message=ReferenceMenuSkin.Label(toast,"",19,.018f,.02f,.964f,.96f,ReferenceMenuSkin.Gold);toast.gameObject.SetActive(false);
            _=FriendInvites.OwnId;
            if(GameUpdateClient.Instance==null)new GameObject("Game Updates").AddComponent<GameUpdateClient>();
            Show("Start");if(!string.IsNullOrEmpty(ProgressStore.LoadNotice))Notify(ProgressStore.LoadNotice);
            ProgressStore.Changed+=Changed;
            PatchRuntime.Changed+=PatchChanged;
        }
        void Changed(){if(wallet!=null)wallet.text=ProgressStore.Data.coins.ToString();if(badges!=null)badges.text=ProgressStore.Data.achievements.Count.ToString();}
        void OnDestroy(){ProgressStore.Changed-=Changed;PatchRuntime.Changed-=PatchChanged;}
        void PatchChanged(){Refresh();}
        string Group(string name)=>new[]{"Shop","Inventar","Charakter"}.Contains(name) ? "Charakter" : new[]{"Community","Level-Editor","Freunde"}.Contains(name) ? "Community" : new[]{"Profil","Rangliste","Erfolge"}.Contains(name) ? "Profil" : new[]{"Einstellungen","Updates","Credits"}.Contains(name) ? "Einstellungen" : "Spielen";
        public void Show(string name)
        {
            if(name=="Level-Editor"){LevelEditorScreen.Open();return;}
            currentPage=name;Changed();updateStatus=null;GameUI.Clear(body);var fade=body.GetComponent<MenuFade>();if(fade==null)body.gameObject.AddComponent<MenuFade>();else fade.Replay();message.text="";message.transform.parent.gameObject.SetActive(false);
            foreach(var pair in navigation)ReferenceMenuSkin.Selected(pair.Value,pair.Key==name || name=="Weltkarte" && pair.Key=="Levelauswahl");ReferenceMenuSkin.Selected(homeButton,name=="Start");
            if(name=="Start"){MenuDashboard.Build(FantasyMenu.Region(body,"Dashboard",0,0,1,1),this);return;}
            var surface=ReferenceMenuSkin.Panel(body,"Menüseite",0,0,1,1);
            heading=ReferenceMenuSkin.Label(surface,name=="Levelauswahl" ? "Deine Welten" : name=="Inventar" || name=="Waffen" ? "Waffen & Inventar" : name,32,.025f,.86f,.95f,.12f,ReferenceMenuSkin.Gold,true);
            var holder=FantasyMenu.Region(surface,"Seitenansicht",.018f,.025f,.964f,.82f);page=GameUI.Scroll(holder);
            switch(name)
            {
                case "Levelauswahl":MenuDashboard.Worlds(page,this);break;
                case "Weltkarte":MenuDashboard.WorldMap(page,this);break;
                case "Shop":MenuShopPages.Shop(page,this);break;
                case "Inventar":MenuShopPages.Inventory(page,this);break;
                case "Waffen":MenuShopPages.Inventory(page,this);break;
                case "Charakter":MenuProfilePages.Character(page,this);break;
                case "Profil":MenuProfilePages.Profile(page,this);break;
                case "Einstellungen":MenuProfilePages.Settings(page,this);break;
                case "Community":CommunityLevelsPage.Build(page,this);break;
                case "Freunde":FriendsPage.Build(page,this);break;
                case "Rangliste":Records();break;
                case "Erfolge":Achievements();break;
                case "Credits":CreditsPage.Build(page);break;
                case "Updates":Updates();break;
            }
        }
        public void ShowWorld(int world){Show("Levelauswahl");GameUI.Clear(page);heading.text=GameConfig.WorldNames[world];MenuDashboard.Stages(page,this,world);}
        public void SelectLevel(int id){if(!CampaignCatalog.Unlocked(id))return;SelectedLevel=id;Show("Start");}
        public void Refresh()=>Show(currentPage);
        public void Notify(string value){message.text=value;message.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value));noticeUntil=Time.unscaledTime+5;Changed();}
        void Updates()
        {
            var client=GameUpdateClient.Instance;
            FantasyMenu.PageCard(page,"TreasureRunner · "+Application.version,"Installierte Version","world",135);
            updateStatus=GameUI.WrappedText(UpdateDescription(),page,23,GameUI.Muted);
            GameUI.Button("Nach Updates suchen",page,()=>{client?.CheckNow();PatchClient.Instance?.CheckNow();Notify("Update-Prüfung gestartet.");},true,true);
            GameUI.Button("Download / Update-Status öffnen",page,()=>client?.OpenStatus());
            BackgroundUpdatePanel.Build(page,this);
        }
        string UpdateDescription(){var c=GameUpdateClient.Instance;return (c!=null ? c.LastStatus : "Noch nicht geprüft")+(c!=null && !string.IsNullOrEmpty(c.AvailableVersion) ? "\nVerfügbar: "+c.AvailableVersion : "");}
        void Update(){if(message!=null && message.transform.parent.gameObject.activeSelf && Time.unscaledTime>noticeUntil)message.transform.parent.gameObject.SetActive(false);if(updateStatus!=null){string description=UpdateDescription();if(updateStatus.text!=description)updateStatus.text=description;}}
        void Records()
        {
            int total=0;foreach(var r in ProgressStore.Data.levels.OrderBy(r=>CampaignCatalog.World(r.id)).ThenBy(r=>CampaignCatalog.Stage(r.id))){total+=r.bestScore;FantasyMenu.PageCard(page,GameConfig.Current.LevelName(r.id),r.bestScore+" Punkte · Beste Zeit "+MenuDashboard.Time(r.bestTime)+" · "+r.completions+" Abschlüsse","records",115);}
            GameUI.Text("Gesamtbestwert: "+total,page,28,GameUI.Gold);
            GameUI.WrappedText("Persönliche Rekorde auf diesem Gerät. Noch kein Online-Dienst für weltweite Ranglisten verbunden.",page,21,GameUI.Muted);
        }
        private void Achievements()
        {
            GameUI.Clear(page); heading.text = "Erfolge";
            string[] names = { "Erster Schatz", "Monsterjäger", "Goldrausch" };
            string[] descriptions = { "Schließe ein Level ab.", "Besiege zehn Gegner.", "Sammle insgesamt 1.000 Münzen." };
            for (int i = 0; i < names.Length; i++)
            {
                GameUI.Text((ProgressStore.Data.achievements.Contains(names[i]) ? "ERREICHT · " : "OFFEN · ") + names[i],page,27,GameUI.Gold);
                GameUI.Text(descriptions[i],page,23,GameUI.Muted);
            }
            GameUI.Text("SCHATZSAMMLUNG",page,28,GameUI.Gold);int count=0;
            foreach(string badge in ProgressStore.Data.achievements)
            {
                if(!badge.StartsWith("Schatz · "))continue;count++;string key=badge.Substring(9);int dot=key.IndexOf('.');
                string title=badge;
                if(dot>0 && int.TryParse(key.Substring(dot+1),out int level) && level>=0 && level<GameConfig.Current.TotalLevels)
                    title=(key.StartsWith("boss.") ? "Wächterschatz · " : "Geheimraumschatz · ")+GameConfig.Current.LevelName(level);
                GameUI.Text(title,page,23,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=70;
            }
            if(count==0)GameUI.Text("Finde goldene Schlüssel und besiege Weltwächter, um seltene Schatzabzeichen zu sammeln.",page,23,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=90;
        }
    }
}
