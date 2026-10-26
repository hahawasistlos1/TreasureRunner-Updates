using UnityEngine;
using UnityEngine.UI;

namespace TreasureRunner
{
    public static class MenuProfilePages
    {
        public static void Profile(Transform page,MainMenuController menu)
        {
            GoogleAccountPanel.Build(page,menu);
            if(!GoogleIdentity.Current.HasSession){
            GameUI.Text("Lokales Profil · ohne Anmeldung spielbar",page,28);
            GameUI.Text("Nickname",page,23,GameUI.Muted);
            var rect = GameUI.Panel("Name input",page,new Color(.13f,.24f,.27f));
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
            var input = rect.gameObject.AddComponent<InputField>();
            var text = GameUI.Text(ProgressStore.Data.playerName,rect,28);
            GameUI.Stretch(text.rectTransform,18,8,18,8);
            input.textComponent = text; input.text = ProgressStore.Data.playerName; input.characterLimit = 20;
            input.onEndEdit.AddListener(value =>
            {
                value=value.Trim();if(!NicknamePolicy.Valid(value)){input.text=ProgressStore.Data.playerName;menu.Notify(NicknamePolicy.Hint);return;}
                ProgressStore.Data.playerName = value;
                ProgressStore.Save(); menu.Notify("Profil gespeichert.");
            });}
            GameUI.Text("Freigeschaltet: " + System.Linq.Enumerable.Count(System.Linq.Enumerable.Range(0,CampaignCatalog.Count),CampaignCatalog.Unlocked) + " / " + GameConfig.Current.TotalLevels + " Level",page,25);
            GameUI.Text("Besiegte Gegner: " + ProgressStore.Data.totalEnemies,page,25);
            GameUI.Text("Gesammelte Münzen insgesamt: " + ProgressStore.Data.lifetimeCoins,page,25);
            int worlds=0,completed=0,total=0;for(int w=0;w<8;w++){int n=MenuDashboard.Completed(w);completed+=n;if(n==10)worlds++;}foreach(var r in ProgressStore.Data.levels)total+=r.bestScore;
            FantasyMenu.PageCard(page,"Dein Abenteuer",completed+" / 80 Level abgeschlossen · "+worlds+" / 8 Welten abgeschlossen\nGesamtbestwert "+total+" Punkte · "+ProgressStore.Data.achievements.Count+" Erfolge und Schatzabzeichen","records",150);
            GameUI.Button("Rekorde ansehen",page,()=>menu.Show("Rangliste"));GameUI.Button("Erfolge & Schätze",page,()=>menu.Show("Erfolge"));
            GameUI.WrappedText("Dein gewählter Nickname erscheint in Einladungen. Freunde, Ranglisten und Spielstände werden durch die Anmeldung noch nicht automatisch synchronisiert.",page,22,GameUI.Muted);
        }
        public static void Character(Transform page,MainMenuController menu)
        {
            GameUI.WrappedText("Vier kostenlose Figuren mit eigenen kaufbaren Fähigkeiten und Spezialwaffen. Herzen, Bewegung und Sprung bleiben gleich.",page,21,GameUI.Muted);
            var row = GameUI.Row(page,230);
            var preview = GameUI.Rect("Character preview",row);
            var previewLayout = preview.gameObject.AddComponent<LayoutElement>();
            previewLayout.minWidth = previewLayout.preferredWidth = 160;
            previewLayout.flexibleWidth = 0;
            preview.gameObject.AddComponent<RawImage>();
            preview.gameObject.AddComponent<CharacterPreview>();
            var info = GameUI.Column(row,10,10);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            GameUI.Text(CharacterCatalog.Current.title,info,28,GameUI.Gold);
            GameUI.Text("Farbvariante: "+ProgressStore.Data.skin+"\nAusgerüstet: " + GameConfig.Current.EquippedWeapon.title,info,21)
                .GetComponent<LayoutElement>().preferredHeight = 65;
            GameUI.WrappedText("Schwert, Axt, Bogen und Runenstab kann jede Figur nutzen. Ihre Spezialwaffe und Fähigkeit machen die Figuren unterschiedlich.",info,20,GameUI.Muted);
            MenuShopPages.Abilities(page,menu,CharacterCatalog.Current.id);
            GameUI.Text("FIGURAUSWAHL",page,26,GameUI.Gold);
            for(int line=0;line<2;line++)
            {
                var choices=GameUI.Row(page,210);choices.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;
                for(int column=0;column<2;column++)
                {
                    var character=CharacterCatalog.All[line*2+column];bool selected=CharacterCatalog.Current.id==character.id;
                    var card=GameUI.Panel(character.title,choices,FantasyMenu.Surface);var size=card.gameObject.AddComponent<LayoutElement>();size.preferredWidth=0;size.flexibleWidth=1;FantasyMenu.Frame(card.GetComponent<Image>(),selected);
                    var portrait=FantasyMenu.Region(card,"Figurenporträt",.025f,.13f,.20f,.76f);portrait.gameObject.AddComponent<RawImage>();var display=portrait.gameObject.AddComponent<CharacterPreview>();display.characterId=character.id;display.staticImage=true;
                    FantasyMenu.Label(card,character.title,22,.26f,.63f,.71f,.29f,GameUI.Gold);FantasyMenu.Label(card,CharacterSkills.Title(character.id)+" · Stufe "+CharacterSkills.StoredLevel(character.id)+"/3",18,.26f,.30f,.71f,.30f,GameUI.Muted);
                    FantasyMenu.Action(card,selected ? "Ausgewählt" : "Figur wählen",()=>{if(CharacterCatalog.Select(character.id)){menu.Refresh();menu.Notify(character.title+" ausgewählt.");}},.26f,.06f,.71f,.20f,character.Model!=null && !selected,selected);
                }
            }
            var config=GameConfig.Current;var values=ProgressStore.Data.upgrades;FantasyMenu.PageCard(page,"Deine Werte",string.Join(" · ",System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,config.upgrades.Length),i=>config.upgrades[i].title+" "+values[i]+"/"+config.upgrades[i].maxLevel)),"profile",135);
            GameUI.Text("FARBVARIANTEN",page,26,GameUI.Gold);
            string[] skins = { "Abenteurer", "Waldläufer", "Runenwächter" };
            for (int i = 0; i < skins.Length; i++)
            {
                string skin = skins[i]; int required = i * 3;
                bool unlocked = ProgressStore.Data.highestUnlockedLevel >= required;
                GameUI.Button(skin + (ProgressStore.Data.skin==skin ? " · Ausgewählt" : unlocked ? "" : " · ab Level " + (required + 1)),page,() =>
                {
                    ProgressStore.Data.skin = skin; ProgressStore.Save(); menu.Refresh();
                },unlocked,ProgressStore.Data.skin==skin);
            }
        }
        public static void Settings(Transform page,MainMenuController menu)
        {
            GameUI.Text("Spielversion "+Application.version,page,24,GameUI.Gold);
            GameUI.Button("Updates prüfen / Download anzeigen",page,()=>{GameUpdateClient.Instance?.OpenStatus();menu.Notify(GameUpdateClient.Instance!=null ? GameUpdateClient.Instance.LastStatus : "Updates werden vorbereitet");});
            BackgroundUpdatePanel.Build(page,menu);

            Slider(page,"Musik",ProgressStore.Data.musicVolume,value =>
            { ProgressStore.Data.musicVolume = value; GameAudio.Instance?.ApplyVolumes(); ProgressStore.Save(); });
            Slider(page,"Effekte",ProgressStore.Data.effectsVolume,value =>
            { ProgressStore.Data.effectsVolume = value; GameAudio.Instance?.ApplyVolumes(); ProgressStore.Save(); });
            GameUI.Button(ProgressStore.Data.muted ? "Ton einschalten" : "Stummschalten",page,()=>{ProgressStore.Data.muted=!ProgressStore.Data.muted;GameAudio.Instance?.ApplyVolumes();ProgressStore.Save();menu.Refresh();});
            GameUI.Text("Grafikqualität",page,27,GameUI.Gold);
            var row = GameUI.Row(page);
            string[] labels = { "Sparsam", "Ausgewogen", "Hoch" };
            for (int i = 0; i < labels.Length; i++)
            {
                int quality = i;
                GameUI.Button(labels[i],row,() =>
                {
                    ProgressStore.Data.graphicsQuality = quality;
                    GameGraphics.Apply(); ProgressStore.Save(); menu.Refresh();
                },true,ProgressStore.Data.graphicsQuality == i);
            }
            GameUI.Text("Sparsam reduziert Schatten und Dekoration. Änderungen gelten beim nächsten Levelstart.",page,22,GameUI.Muted)
                .GetComponent<LayoutElement>().preferredHeight = 90;
            GameUI.Button("Einstellungen speichern",page,() => { ProgressStore.Save(); menu.Notify("Einstellungen gespeichert."); },true,true);
            GameUI.Text("STEUERUNG",page,27,GameUI.Gold);
            GameUI.Text("Bewegung: "+(ProgressStore.Data.movementArrows ? "Bewegungspfeile" : "Joystick"),page,24,GameUI.Gold);
            var movementChoice=GameUI.Row(page,65);
            for(int i=0;i<2;i++){bool arrows=i==1;GameUI.Button(arrows ? "Bewegungspfeile" : "Joystick",movementChoice,()=>{ProgressStore.Data.movementArrows=arrows;ProgressStore.Save();menu.Refresh();menu.Notify((arrows ? "Bewegungspfeile" : "Joystick")+" ausgewählt. Auch im Pausenmenü umschaltbar.");},true,ProgressStore.Data.movementArrows==arrows);}
            TouchControlSizePanel.Build(page);
            GameUI.WrappedText("Touch: Joystick oder die klassischen Links-/Rechtspfeile zum Laufen. Sprung, Angriff und Waffenwechsel bleiben gleich. Die Auswahl wird gespeichert und lässt sich auch während einer Spielpause ändern. Tastatur: A/D oder Pfeile, Leertaste, F, Q und Esc. Frei verschiebbare Buttonpositionen werden noch nicht unterstützt.",page,22,GameUI.Muted);
            GameUI.Button("Credits & Lizenzen",page,()=>menu.Show("Credits"));
        }
        private static void Slider(Transform parent,string label,float value,UnityEngine.Events.UnityAction<float> changed)
        {
            GameUI.Text(label,parent,27);
            var root = GameUI.Rect(label + " Slider",parent);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 62;
            var slider = root.gameObject.AddComponent<Slider>();
            var track = GameUI.Panel("Track",root,new Color(.16f,.3f,.32f)); GameUI.Stretch(track,12,22,12,22);
            var handle = GameUI.Panel("Handle",root,GameUI.Gold);
            handle.anchorMin=handle.anchorMax=new Vector2(.5f,.5f);
            handle.sizeDelta = new Vector2(42,54);
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0; slider.maxValue = 1; slider.value = value;
            slider.onValueChanged.AddListener(changed);
        }
    }
}
