using System;
using UnityEngine;

namespace TreasureRunner
{
    public static class ProgressStore
    {
        public const string SaveKey = "TreasureRunner.Progress.v1";
        public const string BackupKey = "TreasureRunner.Progress.backup";
        private static ProgressData current;
        public static ProgressData Data => current ?? (current = Load());
        public static event Action Changed;
        public static bool IsReadOnly { get; private set; }
        public static string LoadNotice { get; private set; }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private static bool memoryTest;
        public static bool IsMemoryTest=>memoryTest;
        private static ProgressData beforeTest;
        public static void BeginMemoryTest()
        {
            if(memoryTest) throw new InvalidOperationException("Test session already active");
            beforeTest=current;current=new ProgressData();memoryTest=true;IsReadOnly=false;LoadNotice=null;
        }
        public static void EndMemoryTest(){current=beforeTest;beforeTest=null;memoryTest=false;IsReadOnly=false;LoadNotice=null;}
#endif
        private static ProgressData Parse(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            try
            {
                var data = JsonUtility.FromJson<ProgressData>(value);
                if (data == null || data.version < 1 || data.coins < 0 || data.upgrades == null ||
                    data.upgrades.Length != 5 || data.levels == null || data.weapons == null ||
                    data.achievements == null || (data.adventureUpgrades!=null && Array.Exists(data.adventureUpgrades,n=>n<0 || n>100)) || data.highestUnlockedLevel < 0 ||
                    Array.Exists(data.upgrades, n => n < 0 || n > 100)) return null;
                return data;
            }
            catch (ArgumentException) { return null; }
        }

        private static ProgressData Load()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if(memoryTest) return new ProgressData();
#endif
            IsReadOnly = false;
            LoadNotice = null;
            var data = Parse(PlayerPrefs.GetString(SaveKey, ""));
            if (data == null)
            {
                data = Parse(PlayerPrefs.GetString(BackupKey, ""));
                if (data != null) LoadNotice = "Der vorherige Speicherstand wurde wiederhergestellt.";
            }
            if (data != null && data.version > 1)
            {
                IsReadOnly = true;
                LoadNotice = "Der Speicherstand stammt aus einer neueren Version. Speichern ist gesperrt.";
            }
            if (data != null) { if(data.adventureUpgrades==null || data.adventureUpgrades.Length!=5) Array.Resize(ref data.adventureUpgrades,5); return Migrate(data); }
            data = new ProgressData { coins = Mathf.Max(0, PlayerPrefs.GetInt("Coins", 0)) };
            if (PlayerPrefs.HasKey(SaveKey))
            {
                // Retain damaged data for diagnosis instead of deleting it.
                PlayerPrefs.SetString("TreasureRunner.Progress.recovery", PlayerPrefs.GetString(SaveKey));
                LoadNotice = "Speicherstand beschädigt. Das bisherige Münzguthaben wurde übernommen.";
            }
            return Migrate(data);
        }

        private static ProgressData Migrate(ProgressData data)
        {
            if(IsReadOnly)return data;
            if(data.weaponUpgrades==null || data.weaponUpgrades.Length<ArsenalProgression.WeaponCount)Array.Resize(ref data.weaponUpgrades,ArsenalProgression.WeaponCount);
            if(data.characterSkills==null || data.characterSkills.Length<4)Array.Resize(ref data.characterSkills,4);
            bool marker=PlayerPrefs.GetInt(CampaignMigration.Marker,0)==1;
            string before=JsonUtility.ToJson(data);
            bool reset=CampaignMigration.Apply(data,marker);
            if(!marker)
            {
                if(!PlayerPrefs.HasKey("TreasureRunner.Progress.before06"))PlayerPrefs.SetString("TreasureRunner.Progress.before06",before);
                // Reset state and its stamp are flushed together. Never touch my-levels files.
                string migrated=JsonUtility.ToJson(data);
                PlayerPrefs.SetString(SaveKey,migrated);PlayerPrefs.SetString(BackupKey,migrated);
                PlayerPrefs.SetInt(CampaignMigration.Marker,1);PlayerPrefs.SetInt("Coins",data.coins);PlayerPrefs.Save();
            }
            if(reset)LoadNotice="Neues Abenteuer: Kampagnenfortschritt einmalig für 0.6 zurückgesetzt. Deine eigenen Level und Einstellungen sind erhalten.";
            return data;
        }

        public static void Save()
        {
            var data = Data;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            if(memoryTest){Changed?.Invoke();return;}
#endif
            if (IsReadOnly) return;
            string previous = PlayerPrefs.GetString(SaveKey, "");
            if (Parse(previous) != null) PlayerPrefs.SetString(BackupKey, previous);
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.SetInt("Coins", data.coins);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void AddCoins(int amount)
        {
            var data = Data;
            if (amount <= 0 || IsReadOnly) return;
            Data.coins = (int)Math.Min(int.MaxValue, (long)Data.coins + amount);
            Data.lifetimeCoins = (int)Math.Min(int.MaxValue, (long)Data.lifetimeCoins + amount);
            Save();
        }

        public static bool TrySpend(int amount)
        {
            var data = Data;
            if (IsReadOnly || amount < 0 || Data.coins < amount) return false;
            Data.coins -= amount;
            return true; // Caller commits the item and wallet together in one Save.
        }

        public static int CompleteLevel(int id, int score, float time, int totalLevels,int collectedCoins=-1,int defeatedEnemies=-1)
        {
            var data = Data;
            if (IsReadOnly) return 0;
            LevelRecord record = Data.FindLevel(id);
            if (record == null) { record = new LevelRecord { id = id }; Data.levels.Add(record); }
            int bonus = record.firstBonusClaimed ? 0 : 100 + id * 25;
            record.firstBonusClaimed = true;
            record.bestScore = Mathf.Max(record.bestScore, score);
            if (record.bestTime <= 0 || time < record.bestTime) record.bestTime = time;
            record.completions++;
            if(collectedCoins>=0 && defeatedEnemies>=0){record.hasDetailedStats=true;record.mostCoins=Mathf.Max(record.mostCoins,collectedCoins);record.mostEnemies=Mathf.Max(record.mostEnemies,defeatedEnemies);}
            if(id<24) Data.highestUnlockedLevel = Mathf.Max(Data.highestUnlockedLevel, Mathf.Min(id + 1, 23));
            Data.coins = (int)Math.Min(int.MaxValue, (long)Data.coins + bonus);
            if (!Data.achievements.Contains("Erster Schatz")) Data.achievements.Add("Erster Schatz");
            if (Data.totalEnemies >= 10 && !Data.achievements.Contains("Monsterjäger")) Data.achievements.Add("Monsterjäger");
            if (Data.lifetimeCoins >= 1000 && !Data.achievements.Contains("Goldrausch")) Data.achievements.Add("Goldrausch");
            Save();
            return bonus;
        }

        public static void Reload() {
#if UNITY_EDITOR
            if(memoryTest) return;
#endif
            current = null; IsReadOnly = false; LoadNotice = null; }
    }
}
