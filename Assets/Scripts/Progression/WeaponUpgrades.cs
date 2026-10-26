using UnityEngine;
namespace TreasureRunner
{
    public static class WeaponUpgrades
    {
        public const int Cap=3;
        public static int Index(string id)=>ArsenalProgression.WeaponIndex(id);
        public static int Level(string id)
        {
            if(LevelRun.IsCustom)return 0;
            var a=ProgressStore.Data.weaponUpgrades;int i=Index(id);
            return i<0 || a==null || a.Length<=i ? 0 : Mathf.Clamp(a[i],0,Cap);
        }
        public static int Damage(WeaponDefinition w)
        {
            int level=Level(w.id);
            // General strength never increases projectile damage. Ranged milestones cost two ranks.
            int baseline=PatchRuntime.WeaponDamage(w,w.ranged ? (w.id=="staff" ? 2 : 1) : w.damage);
            return w.ranged ? baseline+level/2 : baseline+level+(LevelRun.IsCustom ? 0 : Mathf.Min(2,ProgressStore.Data.upgrades[3]/2));
        }
        public static float Cooldown(WeaponDefinition w)=>Mathf.Max(w.ranged ? .62f : .35f,
            PatchRuntime.WeaponCooldown(w,w.id=="bow" ? .95f : w.id=="staff" ? 1.25f : w.cooldown)*(1-Level(w.id)*.045f)*(w.ranged ? 1-AdventureUpgrades.Value(1)*.5f : 1));
        public static int Price(string id)=>Mathf.RoundToInt(BasePrice(id)*Mathf.Pow(2,Level(id)));
        public static int BasePrice(string id)=>id=="sword" ? 160 : id=="axe" ? 240 : id=="bow" ? 320 : id=="staff" ? 420 : id=="spear" ? 350 : id=="hammer" ? 450 : id=="crossbow" ? 550 : 650;
        public static string Buy(string id)
        {
            if(Index(id)<0)return "Unbekannte Waffe.";
            if(!ProgressStore.Data.weapons.Contains(id))return "Kaufe zuerst diese Waffe.";
            if(Level(id)>=Cap)return "Waffe vollständig verbessert.";
            int required=ArsenalProgression.WeaponRankRequirement(id,Level(id)+1);if(ArsenalProgression.Completed<required)return ArsenalProgression.LockText(required)+" kaufbar.";
            if(!ProgressStore.TrySpend(Price(id)))return "Nicht genügend Münzen.";
            if(ProgressStore.Data.weaponUpgrades==null || ProgressStore.Data.weaponUpgrades.Length<ArsenalProgression.WeaponCount)System.Array.Resize(ref ProgressStore.Data.weaponUpgrades,ArsenalProgression.WeaponCount);
            ProgressStore.Data.weaponUpgrades[Index(id)]++;ProgressStore.Save();return "Waffe verbessert!";
        }
    }
}
