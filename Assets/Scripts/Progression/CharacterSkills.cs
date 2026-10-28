using System;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public static class CharacterSkills
    {
        public const int Cap=3;
        static readonly string[] Ids={"goblin","skeleton-knight","skeleton-scout","skeleton-mage"};
        static readonly string[] Names={"Schatzsinn","Knochenwache","Fokus","Runenecho"};
        static readonly string[] Descriptions={
            "Zieht Münzen zusätzlich aus 0,5 / 1 / 1,5 m an. Wirkt automatisch und ergänzt den Münzmagneten.",
            "Fängt automatisch einen Gegner- oder Fallentreffer ab. Danach 24 / 20 / 16 s Ladezeit. Schützt nicht vor Stürzen.",
            "Ein Treffer mit Bogen oder Armbrust verlangsamt den Gegner für 1 / 1,5 / 2 s. Danach 12 / 10 / 8 s Ladezeit.",
            "Ein Treffer mit Runen- oder Froststab verursacht +1 Schaden und zeigt ein Runenecho. Danach 12 / 10 / 8 s Ladezeit. Schilde gelten weiter."
        };
        public static int Index(string id){var character=CharacterCatalog.All.FirstOrDefault(c=>c.id==id);return character==null ? -1 : Array.IndexOf(Ids,character.abilityId);}
        public static string Title(string id){int i=Index(id);return i<0 ? "Unbekannte Fähigkeit" : Names[i];}
        public static string Description(string id){int i=Index(id);return i<0 ? "" : Descriptions[i];}
        public static int StoredLevel(string id){int i=Index(id);var values=ProgressStore.Data.characterSkills;return i<0 || values==null || i>=values.Length ? 0 : Mathf.Clamp(values[i],0,Cap);}
        // Editor/community levels remain independent of bought campaign powers.
        public static int Level(string id)=>LevelRun.IsCustom ? 0 : StoredLevel(id);
        public static int Price(string id)=>StoredLevel(id)==0 ? 180 : StoredLevel(id)==1 ? 600 : 1500;
        public static string Buy(string id)
        {
            int i=Index(id),rank=StoredLevel(id);if(i<0)return "Unbekannte Fähigkeit.";
            if(rank>=Cap)return "Fähigkeit vollständig verbessert.";
            int required=ArsenalProgression.AbilityRequirement(rank+1);if(ArsenalProgression.Completed<required)return ArsenalProgression.LockText(required)+" kaufbar.";
            if(!ProgressStore.TrySpend(Price(id)))return "Nicht genügend Münzen.";
            if(ProgressStore.Data.characterSkills==null || ProgressStore.Data.characterSkills.Length!=4)Array.Resize(ref ProgressStore.Data.characterSkills,4);
            ProgressStore.Data.characterSkills[i]=rank+1;ProgressStore.Save();return Title(id)+" verbessert!";
        }
    }
}
