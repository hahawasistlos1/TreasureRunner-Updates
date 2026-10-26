using System;
using System.Collections.Generic;

namespace TreasureRunner
{
    [Serializable] public class ProgressData
    {
        public int version = 1;
        public int campaignRevision;
        public int[] weaponUpgrades = new int[8];
        public int[] characterSkills = new int[4];
        public int[] adventureUpgrades = new int[5];
        public int coins;
        public int highestUnlockedLevel;
        public int[] upgrades = new int[5];
        public List<string> weapons = new List<string> { "sword" };
        public string equippedWeapon = "sword";
        public string playerName = "Schatzjäger";
        public string skin = "Abenteurer";
        public string characterId = "goblin";
        public float musicVolume = 0.35f;
        public float effectsVolume = 0.7f;
        public bool muted;
        public bool movementArrows;
        public float touchMovementScale=1.2f,touchJumpScale=1.2f,touchAttackScale=1.2f,touchWeaponScale=1f;
        public string friendId;
        public List<FriendContact> friends = new List<FriendContact>();
        public int graphicsQuality = 1;
        public List<LevelRecord> levels = new List<LevelRecord>();
        public List<string> achievements = new List<string>();
        public int totalEnemies;
        public int lifetimeCoins;
        public LevelRecord FindLevel(int id) => levels.Find(l => l.id == id);
    }

    [Serializable] public class LevelRecord
    {
        public int id;
        public int bestScore;
        public float bestTime;
        public int completions;
        public bool firstBonusClaimed;
        public bool hasDetailedStats;
        public int mostCoins,mostEnemies;
    }
}
