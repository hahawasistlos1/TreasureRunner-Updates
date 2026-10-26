using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class HomeScreen:MonoBehaviour
    {
        RectTransform root;
        public static bool ReturnToPlayerMenu;
        public void Build()
        {
            root=GameUI.Canvas("TreasureRunner Home");
            FantasyMenu.Background(root);
            var panel=FantasyMenu.Card(root,"Willkommen",.52f,.15f,.44f,.70f);
            FantasyMenu.Logo(panel,.05f,.61f,.90f,.36f);
            FantasyMenu.Label(panel,"Dein nächstes Abenteuer wartet",23,.07f,.49f,.86f,.11f);
            FantasyMenu.Label(panel,"Acht Welten. Verborgene Schätze.\nUnd Abenteuer, die du selbst baust.",21,.07f,.33f,.86f,.14f,GameUI.Muted);
            FantasyMenu.Action(panel,"Abenteuer starten",OpenPlayerMenu,.07f,.19f,.86f,.12f,true,true);
            FantasyMenu.Action(panel,"Eigene Level bauen",()=>{OpenPlayerMenu();LevelEditorScreen.Open();},.07f,.045f,.57f,.11f);
            FantasyMenu.Action(panel,"Credits",CreditsPage.Open,.67f,.045f,.26f,.11f);
            FantasyMenu.Label(root,"Willkommen, "+ProgressStore.Data.playerName+" · v"+Application.version,20,.53f,.055f,.43f,.07f,GameUI.Muted);
            if(GameUpdateClient.Instance==null)new GameObject("Game Updates").AddComponent<GameUpdateClient>();
        }
        public void OpenPlayerMenu()
        {
            if(root==null)return;root.parent.gameObject.SetActive(false);Destroy(root.parent.gameObject);root=null;
            gameObject.AddComponent<MainMenuController>().Build();Destroy(this);
        }
    }
}
