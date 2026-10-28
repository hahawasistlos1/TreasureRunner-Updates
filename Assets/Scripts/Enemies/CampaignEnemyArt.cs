using UnityEngine;
namespace TreasureRunner
{
    public class CampaignEnemySkin
    {
        public readonly string id,title,weapon;public readonly Color tint;
        public CampaignEnemySkin(string id,string title,string weapon,Color tint){this.id=id;this.title=title;this.weapon=weapon;this.tint=tint;}
        public GameObject Model=>id=="goblin" ? GameConfig.Current.goblinVisual : CharacterCatalog.Get(id).Model;
    }
    public static class CampaignEnemyArt
    {
        static readonly string[][] melee={new[]{"hero.rogue","goblin","hero.ranger"},new[]{"skeleton-scout","skeleton-knight","hero.knight"},new[]{"hero.barbarian","hero.rogue","skeleton-scout"},new[]{"skeleton-knight","hero.barbarian","hero.knight"},new[]{"skeleton-scout","skeleton-knight","hero.rogue"},new[]{"hero.rogue-hooded","goblin","hero.mage"},new[]{"hero.knight","skeleton-knight","hero.barbarian"},new[]{"hero.mage","skeleton-mage","hero.rogue-hooded"}};
        static readonly string[][] shooters={new[]{"hero.ranger","goblin"},new[]{"skeleton-mage","hero.ranger"},new[]{"hero.ranger","skeleton-scout"},new[]{"skeleton-mage","hero.mage"},new[]{"skeleton-mage","skeleton-scout"},new[]{"hero.mage","hero.rogue-hooded"},new[]{"hero.ranger","skeleton-scout"},new[]{"hero.mage","skeleton-mage"}};
        static readonly string[] titles={"Waldräuber","Frostwache","Küstenräuber","Glutwache","Minenwache","Nebelräuber","Burgwache","Runenhüter"};
        public static CampaignEnemySkin Select(int world,int variant,bool ranged)
        {
            world=Mathf.Clamp(world,0,7);var choices=ranged ? shooters[world] : melee[world];string id=choices[Mathf.Abs(variant)%choices.Length];
            string weapon=ranged ? id.Contains("mage") ? "staff" : world==6 || world==4 ? "crossbow" : "bow" : world==2 || world==3 ? "axe" : "sword";
            return new CampaignEnemySkin(id,titles[world]+(ranged ? weapon=="staff" ? " · Zauberer" : " · Schütze" : ""),weapon,LibraryWorldArt.Tint(world));
        }
    }
}
