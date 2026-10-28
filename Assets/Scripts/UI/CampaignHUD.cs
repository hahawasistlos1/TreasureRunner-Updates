using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TreasureRunner
{
    public class CampaignHUD : MonoBehaviour
    {
        private LevelRun run;
        private RectTransform root;
        private RectTransform modal;
        private RectTransform controls;
        private RectTransform joystickRoot,arrowRoot;
        private RectTransform leftArrow,rightArrow,jumpButton,attackButton,weaponButton,sizeDialog;
        private Vector2 controlArea;
        private Text stats;private Text weaponLabel;
        private Text abilityLabel;
        private readonly HeartGraphic[] hearts=new HeartGraphic[3];
        private Text lives;
        private Text toast;
        private float toastUntil;
        private float nextRefresh;
        public void Build(LevelRun level)
        {
            run = level; run.HUD = this;
            root = GameUI.Canvas("Campaign HUD");
            var bar = GameUI.Panel("Status bar",root,GameUI.Ink);
            bar.anchorMin = new Vector2(.015f,.865f); bar.anchorMax = new Vector2(.985f,.985f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            for(int i=0;i<3;i++)
            {
                var icon=GameUI.Rect("Herz "+(i+1),bar);icon.anchorMin=new Vector2(0,.35f);icon.anchorMax=new Vector2(0,.35f);icon.pivot=Vector2.zero;
                icon.anchoredPosition=new Vector2(16+i*50,0);icon.sizeDelta=new Vector2(46,46);
                hearts[i]=icon.gameObject.AddComponent<HeartGraphic>();hearts[i].raycastTarget=false;
            }
            lives=GameUI.Text("",bar,19,GameUI.Gold);lives.rectTransform.anchorMin=new Vector2(0,0);lives.rectTransform.anchorMax=new Vector2(0,.38f);lives.rectTransform.offsetMin=new Vector2(20,2);lives.rectTransform.offsetMax=new Vector2(190,0);
            stats = GameUI.Text("",bar,21);
            GameUI.Stretch(stats.rectTransform,205,8,175,8); stats.alignment = TextAnchor.MiddleLeft;
            var pause = GameUI.Button("Pause",bar,run.Pause);
            var p = pause.GetComponent<RectTransform>(); p.anchorMin = new Vector2(1,0); p.anchorMax = Vector2.one;
            p.offsetMin = new Vector2(-160,8); p.offsetMax = new Vector2(-8,-8);
            toast = GameUI.Text("",root,26,GameUI.Gold);
            toast.rectTransform.anchorMin = new Vector2(.1f,.79f); toast.rectTransform.anchorMax = new Vector2(.9f,.88f);
            toast.rectTransform.offsetMin = toast.rectTransform.offsetMax = Vector2.zero;
            toast.alignment = TextAnchor.MiddleCenter;
            abilityLabel=GameUI.Text("",root,17,GameUI.Gold);abilityLabel.rectTransform.anchorMin=new Vector2(.025f,.755f);abilityLabel.rectTransform.anchorMax=new Vector2(.65f,.80f);abilityLabel.rectTransform.offsetMin=abilityLabel.rectTransform.offsetMax=Vector2.zero;
            controls = GameUI.Rect("Touch controls",root); GameUI.Stretch(controls);
            var joystickRect=GameUI.Rect("Bewegungs-Joystick",controls);joystickRoot=joystickRect;joystickRect.anchorMin=joystickRect.anchorMax=Vector2.zero;joystickRect.pivot=new Vector2(.5f,.5f);joystickRect.anchoredPosition=new Vector2(110,110);joystickRect.sizeDelta=Vector2.one*168;
            var baseGraphic=joystickRect.gameObject.AddComponent<ControlGlyph>();baseGraphic.shape=ControlGlyph.Shape.Disc;baseGraphic.color=new Color(.03f,.14f,.18f,.8f);
            ControlGlyph.Add(joystickRect,"Ring",ControlGlyph.Shape.Ring,new Color(.38f,.81f,.79f,.65f));
            var knob=GameUI.Rect("Daumen",joystickRect);knob.anchorMin=knob.anchorMax=knob.pivot=new Vector2(.5f,.5f);knob.sizeDelta=Vector2.one*68;
            var knobGraphic=knob.gameObject.AddComponent<ControlGlyph>();knobGraphic.shape=ControlGlyph.Shape.Disc;knobGraphic.color=new Color(.27f,.76f,.69f);knobGraphic.raycastTarget=false;
            ControlGlyph.Add(knob,"Griff",ControlGlyph.Shape.Ring,new Color(.8f,1,.93f),8);
            LibraryTheme.Control(joystickRect,"joystick_circle_pad_a");LibraryTheme.Control(knob,"joystick_circle_nub_a");
            var joystick=joystickRect.gameObject.AddComponent<VirtualJoystick>();joystick.player=run.Player.GetComponent<PlayerMovement>();joystick.knob=knob;
            arrowRoot=GameUI.Rect("Bewegungspfeile",controls);GameUI.Stretch(arrowRoot);
            leftArrow=DirectionAction("Links",-1,new Vector2(80,106));rightArrow=DirectionAction("Rechts",1,new Vector2(204,106));
            jumpButton=(RectTransform)CompactAction("Sprung",new Vector2(-72,113),94,ControlGlyph.Shape.Jump,true,false).transform;
            attackButton=(RectTransform)CompactAction("Angriff",new Vector2(-179,76),86,ControlGlyph.Shape.Sword,false,true).transform;
            var swap=CompactAction("Waffe",new Vector2(-286,66),72,ControlGlyph.Shape.Swap,false,false);
            swap.onClick.AddListener(()=>run.Player.GetComponent<PlayerCombat>().CycleWeapon());var swapRect=(RectTransform)swap.transform;weaponButton=swapRect;
            weaponLabel=GameUI.Text("",swap.transform,18,Color.white);weaponLabel.alignment=TextAnchor.MiddleCenter;
            weaponLabel.rectTransform.anchorMin=new Vector2(.5f,1);weaponLabel.rectTransform.anchorMax=new Vector2(.5f,1);weaponLabel.rectTransform.pivot=new Vector2(.5f,0);weaponLabel.rectTransform.anchoredPosition=new Vector2(0,6);weaponLabel.rectTransform.sizeDelta=new Vector2(174,40);
            ApplyMovementControls();
            TouchControlSizing.Changed+=ControlSizesChanged;
            if(DesktopTools.Active)
            {
                foreach(Transform child in controls)if(child!=swap.transform)child.gameObject.SetActive(false);
                swapRect.anchorMin=new Vector2(.74f,.02f);swapRect.anchorMax=new Vector2(.98f,.08f);
                swapRect.pivot=new Vector2(.5f,.5f);swapRect.offsetMin=swapRect.offsetMax=Vector2.zero;
                swap.image.enabled=false;foreach(var glyph in swap.GetComponentsInChildren<ControlGlyph>())glyph.enabled=false;GameUI.Stretch(weaponLabel.rectTransform);weaponLabel.fontSize=19;
                var help=GameUI.Text("A/D oder Pfeile: laufen   ·   Leertaste: springen   ·   F: Angriff   ·   Q: Waffe   ·   Esc: Pause",controls,18,Color.white);
                help.rectTransform.anchorMin=new Vector2(.02f,.02f);help.rectTransform.anchorMax=new Vector2(.73f,.08f);help.rectTransform.offsetMin=help.rectTransform.offsetMax=Vector2.zero;
            }
            modal = GameUI.Panel("Dialog overlay",root,new Color(.015f,.04f,.06f,.94f));
            GameUI.Stretch(modal); modal.gameObject.SetActive(false);
            RefreshStatus();Toast("3 Herzen · jeder Treffer kostet ein halbes Herz");
        }
        private RectTransform DirectionAction(string label,int direction,Vector2 position)
        {
            var button=CompactAction(label,position,110,ControlGlyph.Shape.Jump,false,false);var rect=(RectTransform)button.transform;rect.SetParent(arrowRoot,false);rect.anchorMin=rect.anchorMax=Vector2.zero;rect.anchoredPosition=position;
            rect.Find("Symbol").localRotation=Quaternion.Euler(0,0,direction<0 ? 90 : -90);
            var action=rect.gameObject.AddComponent<TouchActionButton>();action.player=run.Player.GetComponent<PlayerMovement>();action.direction=direction;
            return rect;
        }
        private void ApplyMovementControls()
        {
            run.Player.GetComponent<PlayerMovement>().ClearInput();bool arrows=ProgressStore.Data.movementArrows;
            joystickRoot.gameObject.SetActive(!DesktopTools.Active && !arrows);arrowRoot.gameObject.SetActive(!DesktopTools.Active && arrows);
            ApplyControlSizes();
        }
        private void ControlSizesChanged()
        {
            if(run==null || run.Player==null)return;
            run.Player.GetComponent<PlayerMovement>().ClearInput();joystickRoot.GetComponent<VirtualJoystick>().ResetStick();
            foreach(var action in controls.GetComponentsInChildren<TouchActionButton>(true))action.ReleasePointers();
            ApplyControlSizes();
        }
        private void ApplyControlSizes()
        {
            if(DesktopTools.Active || controls==null || weaponLabel==null)return;
            controlArea=controls.rect.size;var layout=TouchControlSizing.Layout(controlArea,ProgressStore.Data.movementArrows);
            Size(leftArrow,layout.left,layout.arrowDiameter);Size(rightArrow,layout.right,layout.arrowDiameter);
            Size(joystickRoot,layout.joystick,layout.joystickDiameter);joystickRoot.GetComponent<VirtualJoystick>().knob.sizeDelta=Vector2.one*layout.knobDiameter;
            Size(jumpButton,layout.jump,layout.jumpDiameter);Size(attackButton,layout.attack,layout.attackDiameter);Size(weaponButton,layout.weapon,layout.weaponDiameter);
            var label=weaponLabel.rectTransform;if(label.parent!=controls)label.SetParent(controls,false);
            label.anchorMin=label.anchorMax=new Vector2(1,0);label.pivot=new Vector2(1,0);label.anchoredPosition=layout.labelPosition;label.sizeDelta=new Vector2(layout.labelWidth,36);
            weaponLabel.resizeTextForBestFit=true;weaponLabel.resizeTextMinSize=14;weaponLabel.resizeTextMaxSize=18;
        }
        private static void Size(RectTransform rect,Vector2 position,float diameter)
        {
            rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*diameter;
            var symbol=rect.Find("Symbol") as RectTransform;if(symbol!=null)GameUI.Stretch(symbol,diameter*.15f,diameter*.15f,diameter*.15f,diameter*.15f);
        }
        private void LateUpdate(){if(!DesktopTools.Active && controls!=null && controls.rect.size!=controlArea)ControlSizesChanged();}
        private void OpenSizeDialog()
        {
            if(sizeDialog!=null || run.State!=RunState.Paused)return;
            sizeDialog=GameUI.Canvas("Tastengröße anpassen");sizeDialog.parent.GetComponent<Canvas>().sortingOrder=40;
            GameUI.Stretch(GameUI.Panel("Hintergrund",sizeDialog,new Color(.015f,.035f,.05f,.96f)));
            var card=FantasyMenu.Card(sizeDialog,"Tastengröße im Spiel",.10f,.06f,.8f,.88f);
            FantasyMenu.Label(card,"Touch-Steuerung",28,.035f,.88f,.60f,.09f,GameUI.Gold);
            FantasyMenu.Action(card,"Fertig",CloseSizeDialog,.73f,.88f,.23f,.09f,true,true);
            var region=GameUI.Rect("Größeneinstellungen",card);region.anchorMin=new Vector2(.025f,.025f);region.anchorMax=new Vector2(.975f,.85f);region.offsetMin=region.offsetMax=Vector2.zero;
            TouchControlSizePanel.Build(GameUI.Scroll(region));
        }
        private void CloseSizeDialog(){if(sizeDialog!=null){Destroy(sizeDialog.parent.gameObject);sizeDialog=null;}}
        private void OnDestroy(){TouchControlSizing.Changed-=ControlSizesChanged;CloseSizeDialog();}
        private void SelectMovementControls(bool arrows)
        {ProgressStore.Data.movementArrows=arrows;ProgressStore.Save();ApplyMovementControls();ShowState();}
        private Button CompactAction(string label,Vector2 position,float diameter,ControlGlyph.Shape symbol,bool jump,bool attack)
        {
            var rect=GameUI.Rect("Button "+label,controls);rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*diameter;
            var background=rect.gameObject.AddComponent<ControlGlyph>();background.shape=ControlGlyph.Shape.Disc;background.color=jump ? GameUI.Gold : new Color(.07f,.27f,.3f,.94f);
            ControlGlyph.Add(rect,"Kontur",ControlGlyph.Shape.Ring,jump ? new Color(1,.91f,.64f) : new Color(.38f,.86f,.77f));
            ControlGlyph.Add(rect,"Symbol",symbol,jump ? GameUI.Ink : Color.white,diameter*.15f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=background;
            var colors=button.colors;colors.pressedColor=new Color(.6f,.8f,.75f);button.colors=colors;
            if(jump || attack){var action=rect.gameObject.AddComponent<TouchActionButton>();action.player=run.Player.GetComponent<PlayerMovement>();action.combat=run.Player.GetComponent<PlayerCombat>();action.jump=jump;action.attack=attack;}
            return button;
        }
        private void Touch(string title,Vector2 min,Vector2 max,int direction,bool jump,bool attack)
        {
            var button = GameUI.Button(title,controls,() => {},true,jump);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var action = button.gameObject.AddComponent<TouchActionButton>();
            action.player = run.Player.GetComponent<PlayerMovement>(); action.combat = run.Player.GetComponent<PlayerCombat>();
            action.direction = direction; action.jump = jump; action.attack = attack;
        }
        private void Update()
        {
            if (run == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { if(sizeDialog!=null)CloseSizeDialog();else if (run.State == RunState.Paused) run.Resume(); else if (run.IsPlaying) run.Pause(); }
            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .1f;
                RefreshStatus();
                if (Time.unscaledTime > toastUntil) toast.text = "";
            }
        }
        private void RefreshStatus()
        {
            if(run==null || run.Player==null)return;
            for(int i=0;i<3;i++)hearts[i].Units=Mathf.Clamp(run.Player.HeartUnits-i*2,0,2);
            lives.text="Leben "+run.Player.health+" / "+run.Player.MaxHealth;
            weaponLabel.text=GameConfig.Current.EquippedWeapon.title;
            var ability=run.Player.GetComponent<CharacterAbility>();if(abilityLabel!=null)abilityLabel.text=LevelRun.IsCustom ? "Editor-/Community-Level: ohne gekaufte Fähigkeiten" : ability!=null ? ability.Status : "";
            stats.text=run.DisplayName+"\n"+(LevelRun.IsCustom ? "Level-Münzen " + run.CollectedCoins : "Münzen " + ProgressStore.Data.coins)+"    Punkte "+(ScoreManager.Instance!=null ? ScoreManager.Instance.Score : 0)+"    "+run.Elapsed.ToString("0.0")+" s";
            var ward=run.Player.GetComponent<PlayerWard>();if(ward!=null && ward.Active)stats.text+="\nSchutzschild · "+Mathf.CeilToInt(ward.Remaining)+" s · ein Treffer";
        }
        public void Toast(string text) { if (toast == null) return; toast.text = text; toastUntil = Time.unscaledTime + 3; }
        public void ShowState()
        {
            if(run.State!=RunState.Paused)CloseSizeDialog();
            bool show = run.State != RunState.Playing;
            controls.gameObject.SetActive(!show);
            run.Player.GetComponent<PlayerMovement>().ClearInput();
            modal.gameObject.SetActive(show);
            GameUI.Clear(modal);
            if (!show) return;
            var column = GameUI.Column(modal,18,22);
            column.anchorMin = new Vector2(.2f,.12f); column.anchorMax = new Vector2(.8f,.86f);
            column.offsetMin = column.offsetMax = Vector2.zero;
            string title = run.State == RunState.Complete ? "LEVEL GESCHAFFT" : run.State == RunState.Dead ? "GAME OVER" : "PAUSE";
            GameUI.Text(title,column,40,GameUI.Gold);
            if (run.State == RunState.Complete)
            {
                GameUI.Text(run.FinalScore + " Punkte · " + run.Elapsed.ToString("0.0") + " Sekunden",column,29);
                GameUI.Text("Schätze: " + run.CollectedCoins + " Münzen · Gegner: " + run.DefeatedEnemies,column,25);
                GameUI.Text(run.CompletionBonus > 0 ? "Einmaliger Abschlussbonus: " + run.CompletionBonus + " Münzen" :
                    LevelRun.IsCustom ? "Eigenes Level abgeschlossen. Kampagnenfortschritt und Guthaben bleiben getrennt." : "Abschlussbonus bereits erhalten. Dein Bestwert wurde geprüft.",column,24,GameUI.Muted);
                int next = LevelRun.IsCustom ? -1 : CampaignCatalog.Next(LevelRun.SelectedLevel);
                if (next>=0 && CampaignCatalog.Unlocked(next))
                    GameUI.Button("Nächstes Level",column,() => LevelRun.StartLevel(next),true,true);
            }
            else if (run.State == RunState.Paused)
            {
                if(!DesktopTools.Active)
                {GameUI.Text("Bewegungssteuerung",column,23,GameUI.Muted);var choice=GameUI.Row(column,58);GameUI.Button("Joystick",choice,()=>SelectMovementControls(false),true,!ProgressStore.Data.movementArrows);GameUI.Button("Bewegungspfeile",choice,()=>SelectMovementControls(true),true,ProgressStore.Data.movementArrows);GameUI.Button("Tastengröße",choice,OpenSizeDialog);}
                GameUI.Button("Weiterspielen",column,run.Resume,true,true);
            }
            else
            {
                GameUI.Text("Alle Leben verbraucht. Deine gesammelten Münzen bleiben erhalten.",column,26,GameUI.Muted);
                if (run.HasCheckpoint && run.Revives > 0)
                    GameUI.Button("Am Checkpoint weiter (" + run.Revives + ")",column,run.ReviveAtCheckpoint,true,true);
            }
            GameUI.Button("Level neu starten",column,run.Retry);
            GameUI.Button(EditorTestSession.IsTest ? "Zurück zum Editor" : "Zum Spielermenü",column,run.Menu);
        }
    }
}
