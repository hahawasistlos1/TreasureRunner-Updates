using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class ToolWindowTheme
    {
        public static RectTransform Workspace(RectTransform safe,string title,string subtitle,string icon)
        {
            safe.parent.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1280,840);
            safe.parent.GetComponent<CanvasScaler>().matchWidthOrHeight=1;
            ReferenceMenuSkin.Background(safe);
            var head=ReferenceMenuSkin.Panel(safe,"Werkzeugkopf",.275f,.862f,.711f,.119f);
            ReferenceMenuSkin.Icon(head,icon,.02f,.26f,.065f,.56f);
            ReferenceMenuSkin.Label(head,title,28,.105f,.46f,.66f,.46f,ReferenceMenuSkin.Gold,true);
            ReferenceMenuSkin.Label(head,subtitle,17,.105f,.08f,.66f,.36f,GameUI.Muted);
            ReferenceMenuSkin.Label(head,"v"+Application.version,16,.81f,.53f,.16f,.30f,GameUI.Muted).alignment=TextAnchor.MiddleRight;
            ReferenceMenuSkin.Button(head,"Credits",CreditsPage.Open,.80f,.12f,.17f,.34f,false,17);
            return FantasyMenu.Region(safe,"Werkzeug-Arbeitsfläche",0,0,1,.835f);
        }
    }
}
