using System;
using UnityEngine;

namespace TreasureRunner
{
    [Serializable] public class UpgradeDefinition
    {
        public string title;
        public string description;
        public int basePrice;
        public int maxLevel = 5;
        public float increase;
        public int Price(int currentLevel) => Mathf.RoundToInt(basePrice * Mathf.Pow(1.65f, currentLevel));
    }
    [Serializable] public class WeaponDefinition
    {
        public string id;
        public string title;
        public string description;
        public int price;
        public int requiredLevel;
        public int damage = 1;
        public float cooldown = 0.5f;
        public float range = 1.6f;
        public bool ranged;
        public Color color = Color.white;
    }

    [CreateAssetMenu(menuName = "TreasureRunner/Spielkonfiguration")]
    public class GameConfig : ScriptableObject
    {
        public const int LevelsPerWorld = 3;
        public static readonly string[] WorldNames = { "Wald", "Eiswelt", "Wüste", "Vulkan", "Höhle", "Dunkler Wald", "Burg", "Magische Welt" };
        public static readonly Color[] WorldColors = { new Color(.2f,.48f,.3f), new Color(.52f,.78f,.86f),
            new Color(.79f,.59f,.3f), new Color(.5f,.19f,.14f), new Color(.28f,.32f,.4f),
            new Color(.18f,.27f,.26f), new Color(.4f,.38f,.48f), new Color(.47f,.3f,.68f) };
        public UpgradeDefinition[] upgrades;
        public WeaponDefinition[] weapons;
        public GameObject goblinVisual;
        public GameObject characterVisual;
        public GameObject monsterVisual;
        public GameObject swordVisual;
        public GameObject bowVisual;
        public GameObject[] trees;
        public GameObject rock;
        public GameObject grass;
        public Material groundMaterial;
        public Material coinMaterial;
        public int TotalLevels => CampaignCatalog.Count;
        private static GameConfig instance;
        public static GameConfig Current => instance != null ? instance : (instance = Resources.Load<GameConfig>("TreasureRunnerConfig"));
        public WeaponDefinition EquippedWeapon => Array.Find(weapons, w => w.id == (LevelRun.IsCustom ? LevelRun.CustomWeapon : ProgressStore.Data.equippedWeapon) && ArsenalProgression.UsableInRun(w.id)) ?? weapons[0];
        public float Stat(int index) => upgrades[index].increase * ProgressStore.Data.upgrades[index];
        public string LevelName(int id) => WorldNames[CampaignCatalog.World(id)] + " " + (CampaignCatalog.Stage(id)+1) + " · " + CampaignCatalog.Name(id);
    }
}
