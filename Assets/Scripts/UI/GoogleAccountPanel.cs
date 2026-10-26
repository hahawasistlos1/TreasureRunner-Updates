using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class GoogleAccountPanel:MonoBehaviour
    {
        MainMenuController menu;
        public static void Build(Transform page,MainMenuController menu)
        {
            var account=GoogleIdentity.Current;var card=GameUI.Panel("Google-Spielerkonto",page,FantasyMenu.Surface);FantasyMenu.Frame(card.GetComponent<Image>());card.gameObject.AddComponent<LayoutElement>().preferredHeight=account.HasSession ? 510 : 450;
            var column=GameUI.Column(card,10,14);GameUI.Stretch(column);GameUI.Text("SPIELERKONTO",column,27,GameUI.Gold);GameUI.WrappedText(account.Status,column,21,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=62;
            GameUI.WrappedText("Im Spiel erscheint nur dein selbst gewählter Nickname. Google-E-Mail, Profilname und Profilbild werden nicht in dein Spielerprofil übernommen. Dein Spielstand bleibt lokal auf diesem Gerät.",column,19,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=92;
            if(account.HasSession)
            {
                GameUI.Text(account.State==IdentityState.Ready ? "Nickname ändern" : "Deinen Nickname wählen",column,23,GameUI.Gold);
                var field=GameUI.Panel("Nickname-Eingabe",column,new Color(.13f,.24f,.27f));field.gameObject.AddComponent<LayoutElement>().preferredHeight=60;
                var input=field.gameObject.AddComponent<InputField>();var label=GameUI.Text("",field,24);GameUI.Stretch(label.rectTransform,12,5,12,5);input.textComponent=label;input.characterLimit=20;input.text=account.Nickname;input.interactable=!account.Busy && !ProgressStore.IsReadOnly;
                var row=GameUI.Row(column,60);GameUI.Button("Nickname speichern",row,()=>account.SaveNickname(input.text),!account.Busy && !ProgressStore.IsReadOnly,true);GameUI.Button("Abmelden",row,account.SignOut);
            }
            else GameUI.Button("Mit Google anmelden",column,account.SignIn,account.Configured && account.Supported && !account.Busy && !ProgressStore.IsReadOnly,true);
            var observer=card.gameObject.AddComponent<GoogleAccountPanel>();observer.menu=menu;GoogleIdentity.Changed+=observer.Refresh;
        }
        void Refresh(){if(menu!=null)menu.Refresh();}
        void OnDestroy(){GoogleIdentity.Changed-=Refresh;}
    }
}
