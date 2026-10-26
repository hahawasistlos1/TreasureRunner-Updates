using System;
using UnityEngine;
namespace TreasureRunner
{
    public static class AdventureUpgrades
    {
        public static readonly UpgradeDefinition[] Items={
            Item("Münzmagnet","Zieht nahe Münzen an. +0,4 m pro Stufe.",180,.4f,5),
            Item("Schnellfeuer","Bogen und Stab: 7 % kürzere Pause pro Stufe.",240,.07f,5),
            Item("Schatzbonus","Zusätzliche Münze je 10 / 5 / 3 / 2 gesammelte Münzen.",320,1,4),
            Item("Rettungsrucksack","Eine weitere Wiederbelebung am Checkpoint pro Stufe.",450,1,3),
            Item("Schutzamulett","Nach Treffern 0,4 Sekunden länger geschützt.",210,.4f,5)
        };
        static UpgradeDefinition Item(string name,string text,int price,float increase,int cap)=>new UpgradeDefinition{title=name,description=text,basePrice=price,increase=increase,maxLevel=cap};
        public static int Level(int index)=>ProgressStore.Data.adventureUpgrades!=null && index>=0 && index<Items.Length && index<ProgressStore.Data.adventureUpgrades.Length ? Mathf.Clamp(ProgressStore.Data.adventureUpgrades[index],0,Items[index].maxLevel) : 0;
        public static float Value(int index)=>Level(index)*Items[index].increase;
        public static string Buy(int index)
        {
            if(index<0 || index>=Items.Length)return "Unbekanntes Upgrade.";
            var item=Items[index];int level=Level(index);
            if(level>=item.maxLevel)return "Maximalstufe erreicht.";
            int required=ArsenalProgression.RankRequirement(level+1,item.maxLevel);if(ArsenalProgression.Completed<required)return ArsenalProgression.LockText(required)+" kaufbar.";
            if(!ProgressStore.TrySpend(item.Price(level)))return "Nicht genügend Münzen.";
            if(ProgressStore.Data.adventureUpgrades==null || ProgressStore.Data.adventureUpgrades.Length!=Items.Length)Array.Resize(ref ProgressStore.Data.adventureUpgrades,Items.Length);
            ProgressStore.Data.adventureUpgrades[index]++;ProgressStore.Save();return item.title+" verbessert!";
        }
    }
}
