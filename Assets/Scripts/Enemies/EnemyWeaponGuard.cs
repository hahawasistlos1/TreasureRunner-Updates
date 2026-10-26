using UnityEngine;
namespace TreasureRunner
{
    public enum WeaponGuardKind { Armor, Magic, Spellproof }
    public class EnemyWeaponGuard:MonoBehaviour
    {
        public WeaponGuardKind kind;private float nextHint;
        public static bool Apply(Enemy enemy,int damage,string weapon)
        {
            if(enemy==null || enemy.IsDefeated || damage<=0)return false;
            var guard=enemy.GetComponent<EnemyWeaponGuard>();
            if(guard!=null && (guard.kind==WeaponGuardKind.Magic ? !ArsenalProgression.IsMagic(weapon) : guard.kind==WeaponGuardKind.Spellproof ? ArsenalProgression.IsMagic(weapon) : !ArsenalProgression.BreaksArmor(weapon)))
            {
                if(Time.time>=guard.nextHint){guard.nextHint=Time.time+2;LevelRun.Instance?.HUD?.Toast(guard.kind==WeaponGuardKind.Magic ? "Magieschild: Runen- oder Froststab!" : guard.kind==WeaponGuardKind.Spellproof ? "Magieresistent: Pfeile, Bolzen oder Nahkampf!" : "Panzerung: Axt, Hammer oder Magie!");}
                return false;
            }
            if(damage<=0)return false;
            var ability=LevelRun.Instance?.Player?.GetComponent<CharacterAbility>();if(ability!=null)damage=ability.OnHit(enemy,weapon,damage);
            if(weapon=="froststaff")EnemySlow.Apply(enemy,.7f);
            enemy.TakeDamage(damage);return true;
        }
    }
}
