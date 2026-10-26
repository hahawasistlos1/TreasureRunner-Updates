namespace TreasureRunner
{
    public static class CampaignMigration
    {
        public const string Marker="TreasureRunner.CampaignReset.06";
        public static bool Apply(ProgressData data,bool alreadyApplied)
        {
            if(data.campaignRevision>=1 || alreadyApplied){data.campaignRevision=1;return false;}
            data.coins=0;data.highestUnlockedLevel=0;data.upgrades=new int[5];data.adventureUpgrades=new int[5];data.weaponUpgrades=new int[8];data.characterSkills=new int[4];
            data.weapons=new System.Collections.Generic.List<string>{"sword"};data.equippedWeapon="sword";
            data.levels.Clear();data.achievements.Clear();data.totalEnemies=data.lifetimeCoins=0;data.campaignRevision=1;
            return true;
        }
    }
}
