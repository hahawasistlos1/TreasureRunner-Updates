using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public sealed class CommunityReviewPanel:MonoBehaviour
    {
        CommunityLevel level;CommunityReviews service;RectTransform content,listing;Text status,selection;InputField comment;Button save,remove,next;string key,cursor="";int stars;bool loading;
        public static void Open(Transform parent,CommunityLevel level)
        {
            if(string.IsNullOrWhiteSpace(level.id))return;
            var cover=GameUI.Panel("Levelbewertungen",parent,GameUI.Ink);GameUI.Stretch(cover,35,18,35,18);
            var panel=cover.gameObject.AddComponent<CommunityReviewPanel>();panel.level=level;panel.Build();
        }
        void Build()
        {
            gameObject.name="Levelbewertungen-"+Guid.NewGuid().ToString("N");key=ReviewPolicy.Key(level.id);service=gameObject.AddComponent<CommunityReviews>();
            FantasyMenu.Label(transform,level.title+" · Bewertungen",25,.02f,.91f,.76f,.07f,GameUI.Gold);
            FantasyMenu.Action(transform,"Schließen",()=>Destroy(gameObject),.81f,.91f,.17f,.07f);
            content=GameUI.Scroll(FantasyMenu.Region(transform,"Bewertungen und Eingabe",.015f,.02f,.97f,.875f));
            GameUI.WrappedText("1 Stern = gefällt mir nicht · 5 Sterne = sehr gut. Eine Bewertung pro Konto und Level; du kannst sie ändern oder löschen. Dein Nickname und Text sind für andere sichtbar.",content,19,GameUI.Muted);
            status=GameUI.WrappedText("Lade Bewertungen …",content,20,GameUI.Gold);
            listing=GameUI.Column(content,6);listing.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var navigation=GameUI.Row(content,42);GameUI.Button("Erste Seite",navigation,()=>Load(""));next=GameUI.Button("Weitere Bewertungen",navigation,()=>Load(cursor));
            GameUI.Text("Deine Bewertung",content,23,GameUI.Gold);var row=GameUI.Row(content,44);
            for(int n=1;n<=5;n++){int value=n;GameUI.Button(n+" ★",row,()=>{stars=value;selection.text=ReviewPolicy.Stars(stars);UpdateButtons();});}
            selection=GameUI.Text("Wähle 1 bis 5 Sterne",content,21,GameUI.Gold);
            comment=LevelEditorScreen.Input(content,"",ReviewPolicy.MaxComment,100);comment.lineType=InputField.LineType.MultiLineNewline;comment.onValueChanged.AddListener(_=>UpdateButtons());
            var hint=GameUI.Text("Optionaler Kommentar · höchstens 400 Zeichen",comment.transform,18,GameUI.Muted);hint.supportRichText=false;GameUI.Stretch(hint.rectTransform,12,6,12,6);comment.placeholder=hint;
            row=GameUI.Row(content,44);save=GameUI.Button("Öffentlich speichern",row,Save);remove=GameUI.Button("Eigene Bewertung löschen",row,Delete);
            GameUI.WrappedText(Application.platform==RuntimePlatform.Android ? "Zum Bewerten im Profil mit Google anmelden und einen eigenen Nickname wählen. Zwischen Änderungen bitte eine Minute warten. Keine Google-Namen oder E-Mail-Adressen veröffentlichen." : "Öffentliche Bewertungen kannst du hier lesen. Schreiben ist in der Android-App mit Google-Anmeldung verfügbar.",content,18,GameUI.Muted);
            GoogleIdentity.Changed+=UpdateButtons;UpdateButtons();Load("");
        }
        void UpdateButtons()
        {
            bool ready=Application.platform==RuntimePlatform.Android && GoogleIdentity.Current.State==IdentityState.Ready && !service.Busy && !loading;
            save.interactable=ready && ReviewPolicy.Valid(stars,comment.text);remove.interactable=ready;
        }
        void Load(string after)
        {
            if(loading)return;loading=true;UpdateButtons();status.text="Lade Bewertungen …";
            service.Fetch(key,after,(reviews,error)=>{
                loading=false;if(this==null)return;foreach(Transform child in listing)Destroy(child.gameObject);
                if(reviews==null){status.text=error;next.interactable=false;UpdateButtons();return;}
                status.text=reviews.Length==0 ? "Noch keine Bewertungen auf dieser Seite." : "Diese Seite: Ø "+reviews.Average(r=>r.stars).ToString("0.0")+" / 5 · "+reviews.Length+" Bewertungen";
                foreach(var review in reviews)
                {
                    var r=review;GameUI.Text(ReviewPolicy.Stars(r.stars)+" · "+r.nickname+" · Fassung "+r.revision,listing,21,GameUI.Gold).supportRichText=false;
                    if(r.comment!="")GameUI.WrappedText(r.comment,listing,20).supportRichText=false;
                    var actions=GameUI.Row(listing,36);
                    if(r.uid==GoogleIdentity.Current.PlayerId)GameUI.Button("Bearbeiten",actions,()=>{stars=r.stars;comment.text=r.comment;selection.text=ReviewPolicy.Stars(stars);UpdateButtons();});
                    GameUI.Button("Melden",actions,()=>Application.OpenURL("https://github.com/hahawasistlos1/TreasureRunner-Updates/issues/new?title="+Uri.EscapeDataString("Bewertung melden: "+level.title)+"&body="+Uri.EscapeDataString("Level-ID: "+level.id+"\nBewertung: "+key+"/"+r.uid+"\nGrund (bitte ergänzen):")));
                }
                cursor=reviews.LastOrDefault()?.uid??"";next.interactable=reviews.Length==ReviewPolicy.PageSize;UpdateButtons();
            });
        }
        void Save(){if(loading || service.Busy)return;service.Save(key,level.revision,stars,comment.text,success=>{if(this==null)return;if(success)Load("");else status.text="Nicht gespeichert. Anmeldung und Verbindung prüfen; nach einer Änderung eine Minute warten.";UpdateButtons();});UpdateButtons();}
        void Delete(){if(loading || service.Busy)return;service.Delete(key,success=>{if(this==null)return;if(success)Load("");else status.text="Löschen nicht möglich. Bitte Anmeldung und Verbindung prüfen.";UpdateButtons();});UpdateButtons();}
        void OnDestroy(){GoogleIdentity.Changed-=UpdateButtons;}
    }
}
