using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class TouchControlSizePanel:MonoBehaviour
    {
        readonly Slider[] sliders=new Slider[4];readonly Text[] labels=new Text[4];Button reset;
        static readonly string[] Titles={"Pfeile / Joystick","Sprung","Angriff","Waffenwechsel"};
        public static TouchControlSizePanel Build(Transform parent)
        {
            var card=GameUI.Panel("Touch-Tastengröße",parent,FantasyMenu.Surface);card.gameObject.AddComponent<LayoutElement>().preferredHeight=550;
            FantasyMenu.Frame(card.GetComponent<Image>());var column=GameUI.Column(card,10,12);GameUI.Stretch(column);
            GameUI.Text("TASTENGRÖSSE",column,27,GameUI.Gold);
            var help=GameUI.WrappedText("80–180 % in 10-%-Schritten. Sichtbare Tasten und Touch-Flächen wachsen gemeinsam. Deine Auswahl wird automatisch gespeichert.",column,20,GameUI.Muted);help.GetComponent<LayoutElement>().preferredHeight=58;
            var panel=card.gameObject.AddComponent<TouchControlSizePanel>();
            for(int i=0;i<4;i++)
            {
                var group=(TouchControlGroup)i;
                var row=GameUI.Rect(Titles[i],column);row.gameObject.AddComponent<LayoutElement>().preferredHeight=72;
                var label=GameUI.Text("",row,22);label.alignment=TextAnchor.MiddleLeft;
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=new Vector2(.36f,1);label.rectTransform.offsetMin=label.rectTransform.offsetMax=Vector2.zero;panel.labels[i]=label;
                var field=GameUI.Rect("Größe "+Titles[i],row);field.anchorMin=new Vector2(.40f,.06f);field.anchorMax=new Vector2(.99f,.94f);field.offsetMin=field.offsetMax=Vector2.zero;
                var slider=field.gameObject.AddComponent<Slider>();slider.minValue=8;slider.maxValue=18;slider.wholeNumbers=true;slider.direction=Slider.Direction.LeftToRight;panel.sliders[i]=slider;
                var track=GameUI.Panel("Spur",field,new Color(.08f,.21f,.25f));track.anchorMin=new Vector2(0,.5f);track.anchorMax=new Vector2(1,.5f);track.offsetMin=new Vector2(0,-7);track.offsetMax=new Vector2(0,7);
                var fill=GameUI.Panel("Größe",track,new Color(.26f,.78f,.66f));GameUI.Stretch(fill);fill.GetComponent<Image>().raycastTarget=false;slider.fillRect=fill;
                var handleArea=GameUI.Rect("Griffbereich",field);handleArea.anchorMin=new Vector2(0,.5f);handleArea.anchorMax=new Vector2(1,.5f);handleArea.offsetMin=new Vector2(22,-24);handleArea.offsetMax=new Vector2(-22,24);
                var handle=GameUI.Panel("Griff",handleArea,GameUI.Gold);handle.sizeDelta=new Vector2(44,0);
                slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.SetValueWithoutNotify(TouchControlSizing.Get(group)*10);
                slider.onValueChanged.AddListener(value=>TouchControlSizing.Set(group,value/10));
            }
            panel.reset=GameUI.Button("Standardgrößen wiederherstellen",column,TouchControlSizing.Reset,!ProgressStore.IsReadOnly);
            panel.reset.GetComponent<LayoutElement>().preferredHeight=62;
            TouchControlSizing.Changed+=panel.Refresh;panel.Refresh();return panel;
        }
        void Refresh()
        {
            for(int i=0;i<4;i++)if(sliders[i]!=null){float value=TouchControlSizing.Get((TouchControlGroup)i);sliders[i].SetValueWithoutNotify(value*10);sliders[i].interactable=!ProgressStore.IsReadOnly;labels[i].text=Titles[i]+"\n"+Mathf.RoundToInt(value*100)+" %";}
            if(reset!=null)reset.interactable=!ProgressStore.IsReadOnly;
        }
        void OnDestroy(){TouchControlSizing.Changed-=Refresh;}
    }
}
