using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public static class MenuShopPages
    {
        public static void Shop(Transform page,MainMenuController menu)
        {
            var config=GameConfig.Current;
            GameUI.WrappedText("Abenteuerfortschritt: "+ArsenalProgression.Completed+" / 80 verschiedene Level abgeschlossen. Neue Spezialwaffen nach 12, 28, 44 und 68 Leveln. Letzte Verbesserungen nach 72 Leveln. Wiederholungen überspringen keine Freischaltung. Jeder Kauf wird gespeichert.",page,21,GameUI.Muted);
            for(int i=0;i<config.upgrades.Length;i++)
            {int index=i,level=ProgressStore.Data.upgrades[i];var item=config.upgrades[i];int required=ArsenalProgression.RankRequirement(level+1,item.maxLevel);var card=FantasyMenu.PageCard(page,item.title+" · "+level+" / "+item.maxLevel,item.description+"\n"+ProgressText(level,item.maxLevel,required,item.Price(level)),"shop",168);
                FantasyMenu.Action(card,Label(level,item.maxLevel,required,item.Price(level)),()=>{if(ProgressStore.Data.upgrades[index]!=level)return;var result=ShopService.BuyUpgrade(config,index);menu.Refresh();menu.Notify(result);GameAudio.Instance?.Play("buy");},.76f,.20f,.215f,.58f,CanBuy(level,item.maxLevel,required,item.Price(level)));}
            GameUI.Text("ABENTEUER-AUSRÜSTUNG",page,26,GameUI.Gold);
            for(int i=0;i<AdventureUpgrades.Items.Length;i++)
            {int index=i,level=AdventureUpgrades.Level(i);var item=AdventureUpgrades.Items[i];int required=ArsenalProgression.RankRequirement(level+1,item.maxLevel);var card=FantasyMenu.PageCard(page,item.title+" · "+level+" / "+item.maxLevel,item.description+"\n"+ProgressText(level,item.maxLevel,required,item.Price(level)),"shop",176);
                FantasyMenu.Action(card,Label(level,item.maxLevel,required,item.Price(level)),()=>{if(AdventureUpgrades.Level(index)!=level)return;string result=AdventureUpgrades.Buy(index);menu.Refresh();menu.Notify(result);},.76f,.20f,.215f,.58f,CanBuy(level,item.maxLevel,required,item.Price(level)));}
            Abilities(page,menu);
            GameUI.Text("WAFFEN & VERBESSERUNGEN",page,26,GameUI.Gold);
            foreach(var item in config.weapons)
            {var weapon=item;bool owned=ProgressStore.Data.weapons.Contains(item.id),unlocked=ArsenalProgression.WeaponUnlocked(item);string owner=ArsenalProgression.Owner(item.id);
                var card=FantasyMenu.PageCard(page,item.title,item.description+"\n"+(owner==null ? "Für alle Figuren" : "Nur für "+CharacterCatalog.Get(owner).title)+" · Schaden "+WeaponUpgrades.Damage(item)+" · "+WeaponUpgrades.Cooldown(item).ToString("0.00")+" s\n"+(owned ? "Gekauft" : "Preis: "+item.price+" Münzen"),"weapons",210);MenuWeaponPreview.Show(card,item.id);
                FantasyMenu.Action(card,owned ? "Gekauft" : !unlocked ? owner==null ? "Ab Level "+(item.requiredLevel+1) : ArsenalProgression.LockText(ArsenalProgression.SignatureUnlock(item.id)) : item.price+" Münzen",()=>{string result=ShopService.BuyWeapon(config,weapon.id);menu.Refresh();menu.Notify(result);GameAudio.Instance?.Play("buy");},.76f,.26f,.215f,.52f,!owned && unlocked && ProgressStore.Data.coins>=item.price);
                if(owned){int rank=WeaponUpgrades.Level(item.id),required=ArsenalProgression.WeaponRankRequirement(item.id,rank+1);var upgrade=FantasyMenu.PageCard(page,item.title+" · Verbesserung "+rank+" / 3",(item.ranged ? "Schneller angreifen · Stufe 2: +1 Schaden" : "+1 Schaden je Stufe · etwas schneller")+"\n"+ProgressText(rank,3,required,WeaponUpgrades.Price(item.id)),"weapons",170);
                    FantasyMenu.Action(upgrade,Label(rank,3,required,WeaponUpgrades.Price(item.id)),()=>{if(WeaponUpgrades.Level(weapon.id)!=rank)return;string result=WeaponUpgrades.Buy(weapon.id);menu.Refresh();menu.Notify(result);},.76f,.20f,.215f,.58f,CanBuy(rank,3,required,WeaponUpgrades.Price(item.id)));}
            }
        }
        static bool CanBuy(int rank,int cap,int required,int price)=>!ProgressStore.IsReadOnly && rank<cap && ArsenalProgression.Completed>=required && ProgressStore.Data.coins>=price;
        static string Label(int rank,int cap,int required,int price)=>rank>=cap ? "Maximum" : ArsenalProgression.Completed<required ? ArsenalProgression.LockText(required) : price+" Münzen";
        static string ProgressText(int rank,int cap,int required,int price)=>rank>=cap ? "Vollständig verbessert" : ArsenalProgression.RequirementText(required)+" · "+price+" Münzen";
        public static void Abilities(Transform page,MainMenuController menu,string onlyCharacter=null)
        {
            GameUI.Text("FIGURENFÄHIGKEITEN",page,26,GameUI.Gold);
            foreach(var character in CharacterCatalog.All)
            {
                if(onlyCharacter!=null && character.id!=onlyCharacter)continue;
                string id=character.id;int rank=CharacterSkills.StoredLevel(id),required=ArsenalProgression.AbilityRequirement(rank+1);
                var card=FantasyMenu.PageCard(page,CharacterSkills.Title(id)+" · "+rank+" / 3 · "+character.title,CharacterSkills.Description(id)+"\n"+ProgressText(rank,3,required,CharacterSkills.Price(id)),"profile",220);
                FantasyMenu.Action(card,Label(rank,3,required,CharacterSkills.Price(id)),()=>{if(CharacterSkills.StoredLevel(id)!=rank)return;string result=CharacterSkills.Buy(id);menu.Refresh();menu.Notify(result);},.76f,.20f,.215f,.58f,CanBuy(rank,3,required,CharacterSkills.Price(id)));
            }
        }
        public static void Inventory(Transform page,MainMenuController menu)
        {
            GameUI.WrappedText("Gekaufte Waffen bleiben erhalten. Im Level wechselst du über das Waffensymbol zwischen den Waffen deiner Figur. Spezialwaffen gehören zu ihrer Figur; die vier bisherigen Waffen kann jede Figur verwenden.",page,21,GameUI.Muted);
            foreach(var item in GameConfig.Current.weapons)
            {if(!ProgressStore.Data.weapons.Contains(item.id))continue;var weapon=item;bool usable=ArsenalProgression.CanUse(item.id),equipped=GameConfig.Current.EquippedWeapon.id==item.id;
                var c=FantasyMenu.PageCard(page,item.title,"Schaden "+WeaponUpgrades.Damage(item)+" · Stufe "+WeaponUpgrades.Level(item.id)+" / 3\n"+(equipped ? "Aktuell ausgerüstet" : "In deinem Inventar"),"weapons",190);MenuWeaponPreview.Show(c,item.id);FantasyMenu.Frame(c.GetComponent<Image>(),equipped);
                FantasyMenu.Action(c,equipped ? "Ausgerüstet" : !usable ? "Figur wechseln" : "Ausrüsten",()=>{ShopService.Equip(weapon.id);menu.Refresh();menu.Notify(weapon.title+" ausgerüstet.");},.76f,.24f,.215f,.48f,!equipped && usable,equipped);}
            GameUI.Button("Zum Waffen-Shop",page,()=>menu.Show("Shop"));
        }
    }
}
