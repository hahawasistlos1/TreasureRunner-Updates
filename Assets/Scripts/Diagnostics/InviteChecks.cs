#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace TreasureRunner
{
    public class InviteChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        Button Button(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==label);
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/InvitePreview");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;var sequence=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Log;if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real save changed");}else report.Add("PASS: Real player save and backup unchanged");failed|=errors;report.Add(failed ? "INVITATION TESTS FAILED" : "ALL INVITATION TESTS PASSED");File.WriteAllLines("Logs/InviteTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var data=ProgressStore.Data;data.coins=717;data.highestUnlockedLevel=9;data.characterId="skeleton-mage";
            Check(FriendInvites.ValidId(FriendInvites.OwnId) && FriendInvites.OwnId==data.friendId,"Stable random contact identity is stored without account or personal identifiers");
            string own=FriendInvites.OwnId,foreign="0123456789abcdef0123456789abcdef",link=FriendInvites.Create(foreign,"Waldläufer Mia");
            Check(FriendInvites.TryParse(link,out var friend,out _) && friend.name=="Waldläufer Mia" && friend.id==foreign,"Generated HTTPS invitation round-trips German names");
            Check(FriendInvites.TryParse(link.Replace(FriendInvites.WebRoot,"treasurerunner://invite"),out _,out _),"Fallback custom-scheme invitation carries identical contact information");
            foreach(var bad in new[]{"", "garbage",link.Replace("https:","http:"),link.Replace("hahawasistlos1.github.io","evil.example"),link.Replace(".io/",".io:444/"),link.Replace("https://","https://name@"),link.Replace("/treasurerunner/invite/","/other/"),link.Replace("#","?"),link.Replace("#","?track=1#"),link+"&id="+foreign,link+"&extra=1",link.Replace("v=1","v=2"),link.Replace(foreign,"short"),link.Replace(foreign,foreign.ToUpperInvariant()),link.Replace("name=","name=%0A"),link.Replace("name=","name=%00"),link.Replace("name=","name=%C0%AF"),link.Replace("name=","name=%oops"),link.Replace("name=","name=%25"),link.Replace("name=","name="+new string('a',81)),link+new string('a',1025),"treasurerunner://invite/other#v=1&id="+foreign+"&name=Mia"})Check(!FriendInvites.TryParse(bad,out _,out _),"Reject untrusted/malformed invitation #"+report.Count);
            Check(FriendInvites.CleanName("<b>Mia</b>\n\u202e")=="bMiab","Names cannot introduce rich text, controls or invisible direction overrides");
            Check(!FriendInvites.Add(new FriendContact{id=own,name="Self"},out _) && FriendInvites.Contacts.Count==0,"Cannot add own invitation");
            Check(FriendInvites.Add(friend,out _) && FriendInvites.Contacts.Count==1,"Accepted invitation stores exactly one local contact");
            Check(!FriendInvites.Add(new FriendContact{id=foreign,name="Changed"},out _) && FriendInvites.Contacts[0].name=="Waldläufer Mia","Duplicate invitation cannot overwrite an existing name");
            var json=JsonUtility.ToJson(data);var reload=JsonUtility.FromJson<ProgressData>(json);Check(reload.friendId==own && reload.friends.Single().name==friend.name && reload.coins==717 && reload.highestUnlockedLevel==9,"Contact identity and list persist alongside unchanged progress");
            Check(FriendInvites.Remove(foreign) && !FriendInvites.Remove(foreign),"Removing a local contact is persistent and idempotent");
            data.friends=null;Check(FriendInvites.Contacts.Count==0,"Old save without a friends list is accepted");
            data.friends.Add(null);data.friends.Add(new FriendContact{id="bad",name="Bad"});data.friends.Add(new FriendContact{id=own,name="Self"});Check(FriendInvites.Contacts.Count==0,"Damaged optional contact entries do not affect campaign save");
            for(int i=0;i<FriendInvites.MaxContacts;i++)data.friends.Add(new FriendContact{id=(i+1).ToString("x32"),name="Kontakt"});Check(!FriendInvites.Add(friend,out _) && FriendInvites.Contacts.Count==200,"Contact limit bounds save size");data.friends.Clear();
            InviteReceiver.Ensure();var receiver=InviteReceiver.Instance;
            Check(receiver.Queue(link,out _) && !receiver.Queue(link,out _) && receiver.PendingCount==1,"Warm-link handler queues once and deduplicates repeated taps");
            Check(!receiver.Queue(FriendInvites.MyLink(),out _),"Warm-link handler rejects own link");
            for(int i=1;i<=4;i++)Check(receiver.Queue(FriendInvites.Create((900+i).ToString("x32"),"Mia"),out _),"Small bounded queue accepts invitation "+i);
            Check(!receiver.Queue(FriendInvites.Create(999.ToString("x32"),"Mia"),out _) && receiver.PendingCount==5,"Invitation spam cannot grow queue indefinitely");
            Destroy(receiver.gameObject);yield return null;InviteReceiver.Ensure();receiver=InviteReceiver.Instance;
            var camera=new GameObject("Menu test camera",typeof(Camera)).GetComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;
            var menu=new GameObject("Invitation test menu").AddComponent<MainMenuController>();menu.Build();menu.Show("Freunde");yield return null;yield return null;
            Check(Button("Freund einladen · Link teilen").interactable,"Friends menu offers sharing without requiring an online service");
            Button("Meinen Einladungslink kopieren").onClick.Invoke();Check(FriendInvites.TryParse(GUIUtility.systemCopyBuffer,out var clipboard,out _) && clipboard.id==own,"Copy button produces complete HTTPS invitation");
            var input=FindObjectsByType<InputField>(FindObjectsSortMode.None).First();input.text=link;Button("Eingefügten Einladungslink öffnen").onClick.Invoke();yield return null;
            Check(receiver.PendingCount==1 && FriendInvites.Contacts.Count==0 && Button("Hinzufügen")!=null,"Opening a link asks before adding a contact");Capture(camera,"Einladung");
            Button("Ablehnen").onClick.Invoke();yield return null;Check(receiver.PendingCount==0 && FriendInvites.Contacts.Count==0,"Declining stores no contact");
            receiver.Receive(link);yield return null;Button("Hinzufügen").onClick.Invoke();yield return null;yield return null;
            Check(FriendInvites.Contacts.Single().name==friend.name && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text==friend.name),"Accept button adds contact and renders it on friends page");yield return new WaitForSecondsRealtime(.4f);Capture(camera,"Freunde");
            var friendScroll=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First(s=>s.gameObject.activeInHierarchy);Canvas.ForceUpdateCanvases();friendScroll.verticalNormalizedPosition=0;yield return null;Capture(camera,"Kontaktliste");
            Button("Kontakt entfernen").onClick.Invoke();yield return null;Check(FriendInvites.Contacts.Count==0,"Actual remove button removes contact from list");
            LevelEditorScreen.Working=LevelCodec.Starter();string draft=LevelCodec.Encode(LevelEditorScreen.Working);LevelEditorScreen.Open();yield return null;receiver.Receive(link);yield return null;Check(receiver.PendingCount==1 && !FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Hinzufügen"),"Invitation waits while user is building a level");
            Check(LevelCodec.Encode(LevelEditorScreen.Working)==draft,"Receiving an invitation never changes the editor draft");
            typeof(LevelEditorScreen).GetMethod("Close",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(FindFirstObjectByType<LevelEditorScreen>(),null);yield return null;yield return null;Check(Button("Hinzufügen")!=null,"Queued invitation appears after closing editor");Button("Ablehnen").onClick.Invoke();yield return null;
            Check(data.coins==717 && data.highestUnlockedLevel==9 && data.characterId=="skeleton-mage","All menu social actions preserve currency, campaign progress and selected figure");
            LevelRun.StartLevel(0);yield return null;yield return null;var player=LevelRun.Instance.Player;player.GetComponent<PlayerMovement>().ControlLocked=true;var body=player.GetComponent<Rigidbody>();body.useGravity=false;body.position=new Vector3(1000,10,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();int runCoins=data.coins,runProgress=data.highestUnlockedLevel;string runFigure=data.characterId;receiver.Receive(link);yield return null;Check(LevelRun.Instance!=null && receiver.PendingCount==1 && !FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Hinzufügen"),"Receiving invitation does not interrupt running campaign");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");yield return null;yield return null;yield return null;Check(Button("Hinzufügen")!=null,"Invitation survives scene switch and opens from home screen");Button("Ablehnen").onClick.Invoke();
            Check(data.coins==runCoins && data.highestUnlockedLevel==runProgress && data.characterId==runFigure,"Receiving an in-game invitation preserves current progress (coins "+data.coins+", level "+data.highestUnlockedLevel+", figure "+data.characterId+")");
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App and Android versions remain unchanged");
        }
        void Capture(Camera camera,string name)
        {
            Canvas.ForceUpdateCanvases();var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();var rt=new RenderTexture(1600,900,24);var old=camera.targetTexture;camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Builds/InvitePreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=old;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
