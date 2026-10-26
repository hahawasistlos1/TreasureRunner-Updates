namespace TreasureRunner
{
    public static class ShopService
    {
        public static string BuyUpgrade(GameConfig config, int index)
        {
            if (index < 0 || index >= config.upgrades.Length) return "Unbekanntes Upgrade.";
            var data = ProgressStore.Data;
            var item = config.upgrades[index];
            int level = data.upgrades[index];
            if (level >= item.maxLevel) return "Maximalstufe erreicht.";
            int required=ArsenalProgression.RankRequirement(level+1,item.maxLevel);
            if(ArsenalProgression.Completed<required)return ArsenalProgression.LockText(required)+" kaufbar.";
            if (!ProgressStore.TrySpend(item.Price(level))) return "Nicht genügend Münzen.";
            data.upgrades[index]++;
            ProgressStore.Save();
            return item.title + " verbessert!";
        }

        public static string BuyWeapon(GameConfig config, string id)
        {
            var item = System.Array.Find(config.weapons, w => w.id == id);
            if (item == null) return "Unbekannte Waffe.";
            if (ProgressStore.Data.weapons.Contains(id)) return "Bereits im Inventar.";
            if (!ArsenalProgression.WeaponUnlocked(item)) return "Noch nicht freigeschaltet.";
            if (!ProgressStore.TrySpend(item.price)) return "Nicht genügend Münzen.";
            ProgressStore.Data.weapons.Add(id);
            ProgressStore.Save();
            return item.title + " gekauft!";
        }

        public static void Equip(string id)
        {
            if (ProgressStore.IsReadOnly || !ProgressStore.Data.weapons.Contains(id) || !ArsenalProgression.CanUse(id) || System.Array.Find(GameConfig.Current.weapons,w=>w.id==id)==null) return;
            ProgressStore.Data.equippedWeapon = id;
            ProgressStore.Save();
        }
    }
}
