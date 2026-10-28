#if TREASURERUNNER_ADMIN || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class LevelModerationScreen:MonoBehaviour
    {
        static LevelIssue selected;
        static string sessionToken="";
        RectTransform root,list,detail;Text status;bool busy;
        static CommunityLevel level;
        static string publishedPath;
        [Serializable] class GitHubAccount {public string login;}
        public void Build()
        {
            if(string.IsNullOrEmpty(sessionToken))sessionToken=ModeratorCredential.Load();
            root=GameUI.Canvas("TreasureRunner Levelverwaltung");
            root=ToolWindowTheme.Workspace(root,"Community-Verwaltung","Einreichungen prüfen · Freigaben verwalten","community");
            var bg=GameUI.Panel("Hintergrund",root,GameUI.Ink);GameUI.Stretch(bg);
            var head=GameUI.Column(root,8);head.anchorMin=new Vector2(.03f,.82f);head.anchorMax=new Vector2(.97f,.98f);head.offsetMin=head.offsetMax=Vector2.zero;
            var row=GameUI.Row(head,52);GameUI.Text("COMMUNITY · LEVELVERWALTUNG",row,30,GameUI.Gold);GameUI.Button("Beenden",row,()=>Application.Quit());
            row=GameUI.Row(head,48);GameUI.Button("Einreichungen laden",row,()=>{if(!busy){publishedPath=null;StartCoroutine(LoadQueue());}});
            GameUI.Button("Freigegebene Level",row,()=>{if(!busy){selected=null;level=null;publishedPath=null;Details();StartCoroutine(LoadPublished());}});
            GameUI.Button("Code / Link importieren",row,()=>LevelEditorScreen.ImportDialog(root,l=>{selected=null;publishedPath=null;level=l;Details();}));
            GameUI.Button("GitHub-Zugang",row,Credentials);GameUI.Button("Credits",row,()=>CreditsPage.Open());
            var left=ReferenceMenuSkin.Panel(root,"Einreichungen",.03f,.13f,.32f,.67f);list=GameUI.Scroll(left);
            var right=ReferenceMenuSkin.Panel(root,"Prüfung",.37f,.13f,.60f,.67f);detail=GameUI.Scroll(right);
            status=GameUI.Text("Lade die Einreichungen oder importiere einen Level-Code. Freigaben ändern nur den Community-Katalog.",root,21,GameUI.Muted);status.supportRichText=false;status.rectTransform.anchorMin=new Vector2(.03f,.02f);status.rectTransform.anchorMax=new Vector2(.97f,.11f);status.rectTransform.offsetMin=status.rectTransform.offsetMax=Vector2.zero;
            if(selected!=null && publishedPath==null){try{level=LevelModeration.FromIssue(selected);}catch(Exception){selected=null;}}
            Details();StartCoroutine(publishedPath==null ? LoadQueue() : LoadPublished());
        }
        void Say(string s){if(status!=null)status.text=s;}
        void Details()
        {
            GameUI.Clear(detail);
            if(level==null){GameUI.Text("Wähle links ein Level aus.",detail,26);return;}
            var title=GameUI.Text(level.title+" · von "+level.author,detail,30,GameUI.Gold);title.supportRichText=false;
            GameUI.Text(GameConfig.WorldNames[level.world]+" · "+level.width+" × "+level.height+" · "+level.tiles.Count+" Bauteile · Fassung "+level.revision,detail,23);
            GameUI.Text(EditorTestSession.Passed(level) ? "Ziel erreicht · freiwilliger Test bestanden ✓" : "Testspielen ist freiwillig. Du entscheidest selbst über die Freigabe.",detail,23,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=90;
            LevelMapPreview.Build(detail,level);
            GameUI.Button("In 3D spielen und prüfen",detail,()=>{EditorTestSession.Begin(level);LevelRun.StartCustom(level);},true,true);
            GameUI.Button("Level-Code kopieren",detail,()=>{GUIUtility.systemCopyBuffer=LevelCodec.Encode(level);Say("Code kopiert. Er lässt sich auch im Spiel über Community importieren.");});
            if(selected!=null)GameUI.Button("Einreichung auf GitHub ansehen",detail,()=>Application.OpenURL(LevelModeration.Api.Replace("api.github.com/repos","github.com")+"/issues/"+selected.number));
            if(publishedPath==null)GameUI.Button("Für alle freigeben …",detail,Confirm);
            else GameUI.Button("Freigabe zurücknehmen …",detail,ConfirmWithdrawal);
        }
        IEnumerator LoadQueue()
        {
            busy=true;GameUI.Clear(list);Say("Einreichungen werden geladen …");int found=0;
            for(int page=1;page<=10;page++)
            {
                string json=null,error=null;yield return Request("GET",LevelModeration.Api+"/issues?state=open&per_page=100&page="+page,null,false,s=>json=s,s=>error=s);
                if(error!=null){Say(error);busy=false;yield break;}
                LevelIssueList queue=null;try{queue=JsonUtility.FromJson<LevelIssueList>("{\"items\":"+json+"}");}catch(Exception){Say("GitHub antwortet mit ungültigen Daten.");busy=false;yield break;}
                foreach(var issue in queue.items)
                {
                    CommunityLevel candidate=null;try{candidate=LevelModeration.FromIssue(issue);}catch(Exception){continue;}
                    var captured=issue;var l=candidate;found++;
                    var b=GameUI.Button("#"+issue.number+" · "+candidate.title+"\nvon "+candidate.author+" · v"+candidate.revision,list,()=>{selected=captured;publishedPath=null;level=l;Details();});b.GetComponentInChildren<Text>().supportRichText=false;b.GetComponent<LayoutElement>().preferredHeight=90;
                }
                if(queue.items.Length<100)break;
                if(page==10)Say("Die ersten 1.000 offenen Einträge wurden geprüft.");
            }
            if(found==0)GameUI.Text("Keine gültigen Level-Einreichungen gefunden.",list,22);
            Say(found+" gültige Einreichung(en). Zum Prüfen auswählen. Ein bestandener eigener Test ist keine öffentliche Freigabe.");busy=false;
        }
        RectTransform Dialog(string title)
        {
            var p=GameUI.Panel("Dialog",root,GameUI.Ink);GameUI.Stretch(p,65,60,65,60);
            var col=GameUI.Column(p,12,24);GameUI.Stretch(col);GameUI.Text(title,col,28,GameUI.Gold);return col;
        }
        void Credentials()
        {
            var c=Dialog("GitHub-Zugang für Freigaben");
            GameUI.Text("Öffentliche Einreichungen kannst du ohne Anmeldung ansehen und testen. Zum Freigeben: persönliches Fine-grained-Token, nur TreasureRunner-Updates, Contents: Read and write. Windows kann den Zugang geschützt in seiner Anmeldeinformationsverwaltung behalten.",c,24).GetComponent<LayoutElement>().preferredHeight=145;
            var input=LevelEditorScreen.Input(c,"",300);input.contentType=InputField.ContentType.Password;
            GameUI.Button("Token-Verwaltung öffnen",c,()=>Application.OpenURL("https://github.com/settings/personal-access-tokens/new"));
            GameUI.Button("Für diese Sitzung verwenden",c,()=>{sessionToken=input.text.Trim();input.text="";Destroy(c.parent.gameObject);Say(string.IsNullOrEmpty(sessionToken) ? "Zugang entfernt." : "Zugang für diese Sitzung übernommen.");});
            GameUI.Button("Geschützt speichern und verbunden bleiben",c,()=>{var token=input.text.Trim();if(string.IsNullOrEmpty(token)){Say("Gib zuerst dein GitHub-Token ein.");return;}if(!ModeratorCredential.Store(token)){Say("Windows konnte den Zugang nicht speichern. Du kannst ihn für diese Sitzung verwenden.");return;}sessionToken=token;input.text="";Destroy(c.parent.gameObject);Say("GitHub-Zugang geschützt gespeichert. Beim nächsten Start wieder verfügbar.");});
            GameUI.Button("Zugang entfernen",c,()=>{if(!ModeratorCredential.Store("")){Say("Gespeicherter Zugang konnte nicht entfernt werden. Bitte in Windows → Anmeldeinformationsverwaltung prüfen.");return;}sessionToken="";input.text="";Destroy(c.parent.gameObject);Say("Zugang entfernt.");});
            GameUI.Button("Zurück",c,()=>Destroy(c.parent.gameObject));
        }
        void Confirm()
        {
            if(busy)return;
            if(!LevelModeration.CanPublish(level)){Say("Neue Entwicklungsbauteile erst nach einer ausdrücklich veröffentlichten Spielversion freigeben.");return;}
            if(string.IsNullOrEmpty(sessionToken)){Credentials();return;}
            var c=Dialog("Level öffentlich freigeben?");
            var text=GameUI.Text(level.title+" · von "+level.author+" · v"+level.revision+"\n\nTitel, Autorenname und Bauteile werden in den öffentlichen Community-Katalog übernommen. Andere Spieler laden den Katalog über Community → Aktualisieren. Die Spielversion und APK bleiben unverändert.",c,24);text.supportRichText=false;text.GetComponent<LayoutElement>().preferredHeight=200;
            var frozen=LevelCodec.Decode(LevelCodec.Encode(level));var issue=selected;string hash=EditorTestSession.Fingerprint(frozen);
            GameUI.Button("Jetzt in den Katalog übernehmen",c,()=>{Destroy(c.parent.gameObject);StartCoroutine(Approve(frozen,issue,hash));},true,true);
            GameUI.Button("Abbrechen",c,()=>Destroy(c.parent.gameObject));
        }
        IEnumerator Approve(CommunityLevel frozen,LevelIssue issue,string hash)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            if(!LevelModeration.CanPublish(frozen)){Say("Diese Bauteile sind noch nicht öffentlich unterstützt.");yield break;}
            busy=true;string json=null,error=null;
            bool owner=false;yield return VerifyOwner(v=>owner=v);if(!owner){busy=false;yield break;}
            if(issue!=null)
            {
                yield return Request("GET",LevelModeration.Api+"/issues/"+issue.number,null,false,s=>json=s,s=>error=s);
                if(error==null){try{if(EditorTestSession.Fingerprint(LevelModeration.FromIssue(JsonUtility.FromJson<LevelIssue>(json)))!=hash)error="Die Einreichung wurde geändert. Neu laden und erneut prüfen.";}catch(Exception){error="Die Einreichung ist nicht mehr gültig.";}}
                if(error!=null){Say(error);busy=false;yield break;}
            }
            string catalogPath=LevelModeration.PathFor(frozen);
            yield return Request("GET",LevelModeration.Api+"/contents/"+catalogPath+"?ref=TennoFarm",null,true,s=>json=s,s=>error=s,catalogPath!=LevelModeration.LegacyPath);
            RepositoryFile file=null;string merged=null;
            if(error==null)try{file=ReadFile(json);merged=LevelModeration.Merge(FileText(file),frozen,hash);}catch(Exception e){error="Keine Freigabe: "+e.Message;}
            if(error!=null){Say(error);busy=false;yield break;}
            var body=LevelModeration.WriteBody("Community: "+frozen.title+" (v"+frozen.revision+")",merged,file.sha);
            yield return Request("PUT",LevelModeration.Api+"/contents/"+catalogPath,body,true,s=>json=s,s=>error=s);
            Say(error ?? "Freigegeben! Im Spiel Community öffnen und den Katalog neu laden. Es wurde keine APK veröffentlicht.");busy=false;
#else
            Say("Freigaben sind nur im Windows-Verwaltungswerkzeug möglich.");yield break;
#endif
        }
        static RepositoryFile ReadFile(string json)
        {
            if(string.IsNullOrEmpty(json))return new RepositoryFile{encoding="base64",content=Convert.ToBase64String(Encoding.UTF8.GetBytes(LevelModeration.Empty))};
            var file=JsonUtility.FromJson<RepositoryFile>(json);
            if(file==null || file.encoding!="base64" || string.IsNullOrEmpty(file.sha))throw new FormatException("Ungültige Katalogdatei.");return file;
        }
        static string FileText(RepositoryFile file)=>Encoding.UTF8.GetString(Convert.FromBase64String(file.content));
        IEnumerator LoadPublished()
        {
            busy=true;GameUI.Clear(list);Say("Freigegebene Level werden geladen …");int count=0;bool failed=false;
            foreach(var path in LevelModeration.CatalogPaths)
            {
                string json=null,error=null;yield return Request("GET",LevelModeration.Api+"/contents/"+path+"?ref=TennoFarm",null,false,v=>json=v,v=>error=v,path!=LevelModeration.LegacyPath);
                CommunityCatalog catalog=null;
                if(error==null)try{catalog=LevelModeration.ReadCurrentCatalog(FileText(ReadFile(json)));}catch(Exception e){error=e.Message;}
                if(error!=null){failed=true;Say(error);continue;}
                foreach(var item in catalog.levels)
                {
                    var candidate=item;string origin=path;count++;
                    var b=GameUI.Button(item.title+" · v"+item.revision+"\nvon "+item.author,list,()=>{selected=null;level=candidate;publishedPath=origin;Details();});b.GetComponentInChildren<Text>().supportRichText=false;b.GetComponent<LayoutElement>().preferredHeight=90;
                }
            }
            if(count==0)GameUI.Text("Keine freigegebenen Level im geladenen Katalog.",list,22);
            if(!failed)Say(count+" Freigabe(n). Auswählen → Freigabe zurücknehmen entfernt diese Fassung aus der öffentlichen Auswahl. Lokale Kopien bleiben erhalten.");busy=false;
        }
        void ConfirmWithdrawal()
        {
            if(busy || publishedPath==null || level==null)return;
            if(string.IsNullOrEmpty(sessionToken)){Credentials();return;}
            string path=publishedPath,id=level.id,hash=EditorTestSession.Fingerprint(level);
            var c=Dialog("Freigabe zurücknehmen?");
            var t=GameUI.Text(level.title+" · v"+level.revision+"\n\nDiese freigegebene Fassung wird aus dem Community-Katalog gelöscht. Sie verschwindet nach dem Aktualisieren aus der Online-Auswahl. Bereits gespeicherte lokale Kopien und die ursprüngliche Einreichung bleiben erhalten. Eventuell separat freigegebene ältere Fassungen werden in der Liste einzeln angezeigt.",c,24);t.supportRichText=false;t.GetComponent<LayoutElement>().preferredHeight=240;
            GameUI.Button("Diese Freigabe zurücknehmen",c,()=>{Destroy(c.parent.gameObject);StartCoroutine(Withdraw(path,id,hash));},true,true);
            GameUI.Button("Abbrechen",c,()=>Destroy(c.parent.gameObject));
        }
        IEnumerator Withdraw(string path,string id,string hash)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            busy=true;string json=null,error=null,content=null;RepositoryFile file=null;
            if(path!=LevelModeration.CurrentPath){Say("Dieses Werkzeug verwaltet ausschließlich Version "+ReleaseInfo.Version+".");busy=false;yield break;}
            bool owner=false;yield return VerifyOwner(v=>owner=v);if(!owner){busy=false;yield break;}
            yield return Request("GET",LevelModeration.Api+"/contents/"+path+"?ref=TennoFarm",null,true,v=>json=v,v=>error=v);
            if(error==null)try{file=ReadFile(json);content=LevelModeration.Withdraw(FileText(file),id,hash);}catch(Exception e){error=e.Message;}
            if(error==null)
            {
                string body=JsonUtility.ToJson(new CatalogWrite{message="Community: Freigabe zurückgenommen",sha=file.sha,content=Convert.ToBase64String(Encoding.UTF8.GetBytes(content))});
                yield return Request("PUT",LevelModeration.Api+"/contents/"+path,body,true,v=>json=v,v=>error=v);
            }
            busy=false;if(error!=null){Say("Nicht entfernt: "+error);yield break;}
            level=null;publishedPath=null;Details();yield return LoadPublished();Say("Freigabe zurückgenommen. Spieler sehen die Änderung nach Katalog aktualisieren.");
#else
            Say("Freigaben verwaltest du im Windows-Werkzeug.");yield break;
#endif
        }
        IEnumerator VerifyOwner(Action<bool> result)
        {
            string json=null,error=null;yield return Request("GET","https://api.github.com/user",null,true,v=>json=v,v=>error=v);
            bool owner=false;if(error==null)try{owner=LevelModeration.IsOwner(JsonUtility.FromJson<GitHubAccount>(json)?.login);}catch(Exception){}
            if(!owner)Say(error ?? "Nur der Spielbetreiber hahawasistlos1 darf Level freigeben oder entfernen.");result(owner);
        }
        IEnumerator Request(string method,string url,string body,bool authenticated,Action<string> accept,Action<string> fail,bool missingIsEmpty=false)
        {
            using(var request=new UnityWebRequest(url,method))
            {
                var bounded=new LimitedDownload(2097152);request.downloadHandler=bounded;request.timeout=25;request.redirectLimit=0;
                request.SetRequestHeader("Accept","application/vnd.github+json");request.SetRequestHeader("X-GitHub-Api-Version","2022-11-28");request.SetRequestHeader("User-Agent","TreasureRunner-Levelverwaltung");
                if(authenticated)request.SetRequestHeader("Authorization","Bearer "+sessionToken);
                if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.SetRequestHeader("Content-Type","application/json");}
                yield return request.SendWebRequest();
                if(request.responseCode==404 && missingIsEmpty){accept("");yield break;}
                if(request.result!=UnityWebRequest.Result.Success){fail(request.responseCode==409 ? "Katalog wurde inzwischen geändert. Erneut laden und Freigabe wiederholen." : "GitHub-Anfrage fehlgeschlagen ("+request.responseCode+"). Verbindung und gegebenenfalls Token-Rechte prüfen.");yield break;}
                accept(bounded.Value);
            }
        }
        class LimitedDownload:DownloadHandlerScript
        {
            readonly int limit;readonly System.IO.MemoryStream stream=new System.IO.MemoryStream();
            public LimitedDownload(int size):base(new byte[8192]){limit=size;}
            protected override bool ReceiveData(byte[] bytes,int length){if(stream.Length+length>limit)return false;stream.Write(bytes,0,length);return true;}
            public string Value=>Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}
#endif
