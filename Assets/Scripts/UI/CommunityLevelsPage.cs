using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class BoundedCatalogDownload:DownloadHandlerScript
    {
        public readonly MemoryStream bytes=new MemoryStream();
        public BoundedCatalogDownload():base(new byte[8192]){}
        protected override bool ReceiveData(byte[] data,int length){if(data==null || bytes.Length+length>262144)return false;bytes.Write(data,0,length);return true;}
        protected override byte[] GetData()=>bytes.ToArray();
        protected override string GetText()=>System.Text.Encoding.UTF8.GetString(bytes.ToArray());
    }
    public class CommunityLevelsPage:MonoBehaviour
    {
        public const string CatalogUrl="https://raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/TennoFarm/"+LevelModeration.CurrentPath;
        Transform content;MainMenuController menu;Text status;UnityWebRequest request;string catalogJson,search="";
        public static void Build(Transform page,MainMenuController owner)
        {
            var go=new GameObject("Community loader");go.transform.SetParent(page,false);var component=go.AddComponent<CommunityLevelsPage>();component.content=page;component.menu=owner;component.Draw();
        }
        void Draw()
        {
            GameUI.Text("Level von anderen Schatzbauern",content,25,GameUI.Gold);
            GameUI.Text("Einreichen schickt dein Level zur Entscheidung an den Spielbetreiber. Testspielen ist freiwillig. Eigene Codes und GitHub-Einreichungen kannst du sofort importieren. Nur vom Betreiber freigegebene Level erscheinen im Katalog.",content,22,GameUI.Muted).GetComponent<LayoutElement>().preferredHeight=100;
            var row=GameUI.Row(content,60);GameUI.Button("Code / GitHub-Link öffnen",row,()=>LevelEditorScreen.ImportDialog(content.root,l=>ShowImported(l)));
            GameUI.Button("Level bauen",row,LevelEditorScreen.Open);GameUI.Button("Katalog aktualisieren",row,()=>StartCoroutine(Fetch()));
            status=GameUI.Text("Lade Community-Auswahl …",content,22,GameUI.Muted);
            var searchBox=LevelEditorScreen.Input(content,"",60,52);var placeholder=GameUI.Text("Titel oder Ersteller suchen …",searchBox.transform,21,GameUI.Muted);GameUI.Stretch(placeholder.rectTransform,12,6,12,6);searchBox.placeholder=placeholder;
            searchBox.onValueChanged.AddListener(value=>{search=value;if(catalogJson!=null)ShowCatalog(catalogJson);});
            GameUI.WrappedText("Spielen, im Editor öffnen und über Codes oder Links importieren ist verfügbar. Beliebtheitsranglisten und Bewertungen benötigen einen zusätzlichen Dienst und sind noch nicht verfügbar.",content,19,GameUI.Muted);
            var cached=Resources.Load<TextAsset>("CommunityStarter");if(cached!=null)ShowCatalog(cached.text);
            StartCoroutine(Fetch());
        }
        RectTransform listing;
        void ShowCatalog(string json)
        {
            catalogJson=json;
            CommunityCatalog catalog;try{catalog=JsonUtility.FromJson<CommunityCatalog>(json);}catch(Exception){return;}
            if(!LevelModeration.Compatible(catalog) || catalog.version!=1 || catalog.levels==null || catalog.levels.Length>90)return;
            if(listing!=null)Destroy(listing.gameObject);listing=GameUI.Column(content,12);listing.gameObject.AddComponent<LayoutElement>();
            listing.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            int count=0;
            foreach(var l in catalog.levels)
            {
                if(!LevelCodec.Validate(l,out _))continue;
                if(!string.IsNullOrWhiteSpace(search) && ((l.title??"")+" "+(l.author??"")).IndexOf(search,StringComparison.OrdinalIgnoreCase)<0)continue;
                count++;var level=l;
                var row=GameUI.Row(listing,125);var info=GameUI.Column(row,2);
                GameUI.Text(l.title+" · v"+l.revision,info,24,GameUI.Gold);GameUI.Text(l.author+" · "+GameConfig.WorldNames[l.world],info,20,GameUI.Muted);
                GameUI.Button("Spielen",row,()=>LevelRun.StartCustom(level),true,true);
                GameUI.Button("Im Editor öffnen",row,()=>{LevelEditorScreen.Working=LevelCodec.Decode(LevelCodec.Encode(level));LevelEditorScreen.Open();});
                GameUI.Button("Melden",row,()=>Application.OpenURL("https://github.com/hahawasistlos1/TreasureRunner-Updates/issues/new?title="+Uri.EscapeDataString("Level melden: "+level.title)+"&body="+Uri.EscapeDataString("Level-ID: "+level.id+"\nVersion: "+level.revision+"\nBitte beschreibe das Problem:")));
            }
            status.text=count==0 ? "Keine passenden freigegebenen Level. Eigene Level über Code, GitHub-Link oder Bauplätze öffnen." : count+" freigegebene Level · Community-Auswahl";
        }
        void ShowImported(CommunityLevel l)
        {
            LevelEditorScreen.Working=l;LevelEditorScreen.Open();menu.Notify("Level importiert: "+l.title+". Speichere es in einem freien Bauplatz.");
        }
        bool fetching;
        IEnumerator Fetch()
        {
            if(fetching)yield break;fetching=true;status.text="Suche neue Community-Level …";
            var catalogs=new CommunityCatalog[LevelModeration.CatalogPaths.Length];string error=null;
            for(int i=0;i<catalogs.Length;i++)
            {
                string url="https://raw.githubusercontent.com/hahawasistlos1/TreasureRunner-Updates/TennoFarm/"+LevelModeration.CatalogPaths[i];
                using(request=new UnityWebRequest(url+"?t="+DateTimeOffset.UtcNow.ToUnixTimeSeconds(),UnityWebRequest.kHttpVerbGET))
                {
                    request.downloadHandler=new BoundedCatalogDownload();request.timeout=20;
                    yield return request.SendWebRequest();
                    if(request.responseCode==404)catalogs[i]=LevelModeration.ReadCurrentCatalog(LevelModeration.Empty);
                    else if(request.result==UnityWebRequest.Result.Success)
                    {try{catalogs[i]=LevelModeration.ReadCurrentCatalog(request.downloadHandler.text);}catch(Exception){error="Katalog gehört zu einer anderen Spiel-/Verwaltungsversion oder enthält ungültige Daten.";}}
                    else error="Katalog derzeit nicht erreichbar. Eigene Level über Code oder Bauplätze öffnen.";
                }
                if(error!=null)break;
            }
            if(error==null)ShowCatalog(JsonUtility.ToJson(LevelModeration.Combine(catalogs)));
            else{catalogJson=null;if(listing!=null)Destroy(listing.gameObject);status.text=error;}
            request=null;fetching=false;
        }
        void OnDestroy(){request?.Abort();}
    }
}
