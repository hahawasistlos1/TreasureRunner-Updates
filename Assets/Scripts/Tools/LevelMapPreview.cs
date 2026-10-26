using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class LevelMapPreview
    {
        public static void Build(Transform parent,CommunityLevel level)
        {
            var r=GameUI.Panel("Levelübersicht",parent,new Color(.04f,.09f,.13f));r.gameObject.AddComponent<LayoutElement>().preferredHeight=200;
            foreach(var tile in level.tiles)
            {
                var cell=GameUI.Panel("Bauteil",r,tile.kind==LevelTile.Goal ? GameUI.Gold : tile.kind==LevelTile.Coin ? new Color(1,.85f,.3f) : tile.kind==LevelTile.Ground ? ComicPalette.Ground[level.world] : new Color(.95f,.4f,.4f));
                cell.anchorMin=new Vector2(tile.x/(float)level.width,tile.y/(float)level.height);cell.anchorMax=new Vector2((tile.x+1f)/level.width,(tile.y+1f)/level.height);cell.offsetMin=Vector2.one*.4f;cell.offsetMax=-Vector2.one*.4f;cell.GetComponent<Image>().raycastTarget=false;
            }
        }
    }
}
