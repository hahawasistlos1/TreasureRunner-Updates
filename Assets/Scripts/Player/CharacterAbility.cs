using UnityEngine;
namespace TreasureRunner
{
    // Automatic, visible class powers: no extra button is required on a crowded touch screen.
    public class CharacterAbility:MonoBehaviour
    {
        public string CharacterId {get;private set;}
        float nextReady;
        public int Rank=>CharacterSkills.Level(CharacterId);
        public float Remaining=>Mathf.Max(0,nextReady-Time.time);
        void Awake(){CharacterId=CharacterCatalog.Current.id;}
        bool CanTrigger=>Rank>0 && Remaining<=0 && LevelRun.Instance!=null && LevelRun.Instance.Player!=null && LevelRun.Instance.Player.gameObject==gameObject && LevelRun.Instance.IsPlaying;
        public float MagnetRange=>CharacterId=="goblin" ? Rank*.5f : 0;
        public string Status=>CharacterSkills.Title(CharacterId)+" "+Rank+"/3 · "+(Rank==0 ? "im Shop kaufen" : CharacterId=="goblin" ? "automatisch" : Remaining>0 ? Mathf.CeilToInt(Remaining)+" s" : "bereit");
        public bool Absorb()
        {
            if(CharacterId!="skeleton-knight" || !CanTrigger)return false;
            nextReady=Time.time+28-4*Rank;GetComponent<PlayerHealth>()?.GrantInvulnerability(.5f);Pulse(new Color(.95f,.88f,.6f),"Knochenwache: Treffer abgefangen");return true;
        }
        public int OnHit(Enemy enemy,string weapon,int damage)
        {
            if(enemy==null || enemy.IsDefeated || !CanTrigger)return damage;
            if(CharacterId=="skeleton-scout" && ArsenalProgression.IsArrow(weapon))
            {EnemySlow.Apply(enemy, .5f+.5f*Rank);nextReady=Time.time+14-2*Rank;Pulse(GameUI.Gold,"Fokus: Gegner verlangsamt");}
            else if(CharacterId=="skeleton-mage" && ArsenalProgression.IsMagic(weapon))
            {nextReady=Time.time+14-2*Rank;Pulse(new Color(.7f,.4f,1),"Runenecho: +1 Trefferschaden");return damage+1;}
            return damage;
        }
        void Pulse(Color color,string text){GameEffects.Impact(transform.position,color,"circle_05");LevelRun.Instance?.HUD?.Toast(text);}
    }
    public class EnemySlow:MonoBehaviour
    {
        float expires,originalSpeed,appliedSpeed;Enemy enemy;
        public static void Apply(Enemy target,float seconds)
        {if(target==null || target.IsDefeated)return;var slow=target.GetComponent<EnemySlow>()??target.gameObject.AddComponent<EnemySlow>();slow.expires=Mathf.Max(slow.expires,Time.time+seconds);}
        void Awake(){enemy=GetComponent<Enemy>();originalSpeed=enemy.speed;appliedSpeed=originalSpeed*.65f;enemy.speed=appliedSpeed;GameEffects.Impact(transform.position,new Color(.3f,.85f,1),"circle_05");}
        void Update(){if(Time.time>=expires)Destroy(this);}
        void OnDestroy(){if(enemy!=null && Mathf.Approximately(enemy.speed,appliedSpeed))enemy.speed=originalSpeed;}
    }
}
