using UnityEngine;
namespace TreasureRunner
{
    public static class FriendsPage
    {
        public static void Build(Transform page,MainMenuController menu)
        {
            FantasyMenu.PageCard(page,"Gemeinsam auf Schatzsuche","Teile deinen Einladungslink. Wer ihn öffnet, kann dich als Kontakt speichern. Ohne installiertes Spiel führt die Webseite zum Download.","friends",175);
            GameUI.Button("Freund einladen · Link teilen",page,()=>menu.Notify(FriendInvites.Share()),!ProgressStore.IsReadOnly,true);
            GameUI.Button("Meinen Einladungslink kopieren",page,()=>{GUIUtility.systemCopyBuffer=FriendInvites.MyLink();menu.Notify("Link kopiert. Sende ihn selbst an deine Freunde.");},!ProgressStore.IsReadOnly);
            GameUI.WrappedText("Meine Kontakt-ID: "+FriendInvites.OwnId+"\nDein sichtbarer Name kommt aus deinem Profil.",page,20,GameUI.Muted);
            GameUI.Text("Eine Einladung erhalten?",page,25,GameUI.Gold);
            var input=LevelEditorScreen.Input(page,"",FriendInvites.MaxUrl,65);
            GameUI.Button("Eingefügten Einladungslink öffnen",page,()=>{InviteReceiver.Ensure();InviteReceiver.Instance.Queue(input.text.Trim(),out var message);menu.Notify(message);});
            GameUI.Button("Link aus Zwischenablage einfügen",page,()=>{SharedLinkInput.Paste(input,out var message);menu.Notify(message);});
            GameUI.WrappedText("Auf diesem Gerät gespeicherte Kontakte · kein Online-Status. Für Kontakte auf beiden Geräten teilt ihr eure Einladungslinks gegenseitig. Einladen startet noch kein Koop-Spiel.",page,21,GameUI.Muted);
            var contacts=FriendInvites.Contacts;
            GameUI.Text("Deine Kontakte · "+contacts.Count,page,27,GameUI.Gold);
            if(contacts.Count==0)GameUI.WrappedText("Noch keine Kontakte. Öffne eine Einladung, um jemanden hinzuzufügen.",page,22,GameUI.Muted);
            foreach(var contact in contacts){string id=contact.id;FantasyMenu.PageCard(page,contact.name,"Lokal gespeichert · ID "+id.Substring(0,8)+"…","friends",110);GameUI.Button("Kontakt entfernen",page,()=>{FriendInvites.Remove(id);menu.Refresh();menu.Notify("Kontakt von diesem Gerät entfernt.");},!ProgressStore.IsReadOnly);}
        }
    }
}
