using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class BackgroundUpdatePanel:MonoBehaviour
    {
        Text state;Button off;float next;
        public static void Build(Transform parent,MainMenuController menu)
        {
            var card=GameUI.Panel("Updatebenachrichtigungen",parent,FantasyMenu.Surface);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight=530;
            FantasyMenu.Frame(card.GetComponent<Image>());var column=GameUI.Column(card,10,16);GameUI.Stretch(column);
            GameUI.Text("UPDATES BEI GESCHLOSSENEM SPIEL",column,25,GameUI.Gold);
            var panel=card.gameObject.AddComponent<BackgroundUpdatePanel>();
            panel.state=GameUI.WrappedText("",column,21,GameUI.Muted);panel.state.GetComponent<LayoutElement>().preferredHeight=85;
            var row=GameUI.Row(column,62);row.GetComponent<LayoutElement>().flexibleHeight=0;
            GameUI.Button("Aktivieren / Freigabe",row,()=>{BackgroundUpdateNotifications.Enable();panel.Refresh();},BackgroundUpdateNotifications.Supported,true);
            panel.off=GameUI.Button("Ausschalten",row,()=>{BackgroundUpdateNotifications.Disable();panel.Refresh();},BackgroundUpdateNotifications.Supported);
            GameUI.Button("Android-Benachrichtigungseinstellungen",column,BackgroundUpdateNotifications.OpenSettings,BackgroundUpdateNotifications.Supported);
            GameUI.WrappedText("Nach Freigabe meldet Android neue Versionen mit Änderungen. Antippen öffnet den Download im Spiel. Energiesparen kann die Prüfung verzögern. Nach „Stopp erzwingen“ das Spiel einmal öffnen.",column,19,GameUI.Muted);
            panel.Refresh();
        }
        void Update(){if(Time.unscaledTime>=next){next=Time.unscaledTime+1;Refresh();}}
        void Refresh(){state.text=BackgroundUpdateNotifications.Status;off.interactable=BackgroundUpdateNotifications.Supported && BackgroundUpdateNotifications.Enabled;}
    }
}
