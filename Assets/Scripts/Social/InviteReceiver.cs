using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class InviteReceiver:MonoBehaviour
    {
        public static InviteReceiver Instance {get;private set;}
        readonly Queue<FriendContact> pending=new Queue<FriendContact>();
        RectTransform dialog;FriendContact shown;
        public int PendingCount=>pending.Count+(shown!=null ? 1 : 0);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Ensure(){if(Instance==null)new GameObject("Friend invitations").AddComponent<InviteReceiver>();}
        void Awake()
        {
            if(Instance!=null){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);
            Application.deepLinkActivated+=Receive;
            if(!string.IsNullOrEmpty(Application.absoluteURL))Receive(Application.absoluteURL);
        }
        public void Receive(string link){Queue(link,out _);}
        public bool Queue(string link,out string message)
        {
            if(!FriendInvites.TryParse(link,out var contact,out message))return false;
            if(contact.id==FriendInvites.OwnId){message="Das ist deine eigene Einladung.";return false;}
            if(FriendInvites.Contacts.Any(c=>c.id==contact.id)){message="Dieser Kontakt ist bereits gespeichert.";return false;}
            if(shown?.id==contact.id || pending.Any(c=>c.id==contact.id)){message="Diese Einladung wartet bereits auf deine Bestätigung.";return false;}
            if(PendingCount>=5){message="Bitte bearbeite zuerst die wartenden Einladungen.";return false;}
            pending.Enqueue(contact);message="Einladung empfangen. Bestätigung im Hauptmenü.";return true;
        }
        void Update()
        {
            if(dialog!=null || pending.Count==0 || LevelRun.Instance!=null || DesktopTools.Active || FindFirstObjectByType<LevelEditorScreen>()!=null)return;
            var home=FindFirstObjectByType<HomeScreen>();if(home!=null)home.OpenPlayerMenu();
            var menu=FindFirstObjectByType<MainMenuController>();if(menu==null)return;
            shown=pending.Dequeue();Show(menu);
        }
        void Show(MainMenuController menu)
        {
            dialog=GameUI.Canvas("Freundeseinladung");dialog.parent.GetComponent<Canvas>().sortingOrder=95;
            GameUI.Stretch(GameUI.Panel("Hintergrund",dialog,new Color(0,0,0,.88f)));
            var card=FantasyMenu.Card(dialog,"Einladung",.15f,.12f,.7f,.76f);
            FantasyMenu.Label(card,"Einladung von "+shown.name,29,.04f,.77f,.92f,.16f,GameUI.Gold,true);
            FantasyMenu.Label(card,"Kontakt-ID: "+shown.id.Substring(0,8)+"…",20,.04f,.65f,.92f,.1f,GameUI.Muted);
            FantasyMenu.Label(card,"Als Kontakt auf diesem Gerät hinzufügen?\nDer Name ist nicht online bestätigt. Es gibt noch keinen Online-Status und keine automatische gegenseitige Freundschaft.",22,.04f,.29f,.92f,.34f);
            FantasyMenu.Action(card,"Hinzufügen",()=>{bool added=FriendInvites.Add(shown,out var message);Dismiss();menu.Show("Freunde");menu.Notify(message);},.04f,.06f,.45f,.16f,true,true);
            FantasyMenu.Action(card,"Ablehnen",Dismiss,.52f,.06f,.44f,.16f);
        }
        public void Dismiss(){if(dialog!=null){dialog.parent.gameObject.SetActive(false);Destroy(dialog.parent.gameObject);}dialog=null;shown=null;}
        void OnDestroy(){Application.deepLinkActivated-=Receive;if(dialog!=null)Destroy(dialog.parent.gameObject);if(Instance==this)Instance=null;}
    }
}
