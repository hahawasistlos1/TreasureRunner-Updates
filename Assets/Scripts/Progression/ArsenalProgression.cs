using System;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    // Progress uses unique cleared campaign IDs: replaying or opening a late world cannot skip milestones.
    public static class ArsenalProgression
    {
        public const int FinalMilestone=72;
        public static int Completed=>CompletedIn(ProgressStore.Data);
        public static int CompletedIn(ProgressData data)=>data.levels==null ? 0 : data.levels.Where(r=>r!=null && r.id>=0 && r.id<CampaignCatalog.Count && r.completions>0).Select(r=>r.id).Distinct().Count();
        public static int RankRequirement(int nextRank,int cap)
            =>nextRank<=1 ? 0 : Mathf.RoundToInt(Mathf.Lerp(14,FinalMilestone,(nextRank-2f)/Mathf.Max(1,cap-2)));
        public static int WeaponRankRequirement(string id,int nextRank)=>Mathf.Max(SignatureUnlock(id),nextRank==1 ? 8 : nextRank==2 ? 40 : FinalMilestone);
        public static int AbilityRequirement(int nextRank)=>nextRank==1 ? 4 : nextRank==2 ? 36 : FinalMilestone;
        public static string LockText(int required)=>"Nach "+required+" / 80 Leveln";
        public static string RequirementText(int required)=>Completed>=required ? "Freigeschaltet" : LockText(required)+" · aktuell "+Completed;
        public static string Owner(string id)=>id=="spear" ? "goblin" : id=="hammer" ? "skeleton-knight" : id=="crossbow" ? "skeleton-scout" : id=="froststaff" ? "skeleton-mage" : null;
        public static int SignatureUnlock(string id)=>id=="spear" ? 12 : id=="hammer" ? 28 : id=="crossbow" ? 44 : id=="froststaff" ? 68 : 0;
        public static bool CanUse(string id,string character=null)=>Owner(id)==null || Owner(id)==CharacterCatalog.Get(character??CharacterCatalog.Current.id).abilityId;
        public static bool IsMagic(string id)=>id=="staff" || id=="froststaff";
        public static bool IsArrow(string id)=>id=="bow" || id=="crossbow";
        public static bool BreaksArmor(string id)=>id=="axe" || id=="hammer" || IsMagic(id);
        public static int WeaponIndex(string id)=>Array.IndexOf(new[]{"sword","axe","bow","staff","spear","hammer","crossbow","froststaff"},id);
        public const int WeaponCount=8;
        public static bool WeaponUnlocked(WeaponDefinition weapon)=>Owner(weapon.id)!=null ? Completed>=SignatureUnlock(weapon.id) : ProgressStore.Data.highestUnlockedLevel>=weapon.requiredLevel;
        public static bool UsableInRun(string id)=>LevelRun.IsCustom || CanUse(id);
    }
}
