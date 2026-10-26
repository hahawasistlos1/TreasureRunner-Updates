using UnityEngine;
namespace TreasureRunner
{
    public enum BossPhase { Watching,Warning,Attack,Recovery }
    public class WorldBoss:MonoBehaviour
    {
        public int world;public BossPhase Phase{get;private set;}public float PhaseSeconds{get;private set;}
        Enemy enemy;EnemyPatrol patrol;Transform warning;float next=2;int attack;float lockedX;bool applied;
        void Start()
        {
            enemy=GetComponent<Enemy>();patrol=GetComponent<EnemyPatrol>();
            enemy.onDefeated.AddListener(()=>{if(warning!=null)warning.gameObject.SetActive(false);int reward=SpecialTreasure.Claim("boss."+LevelRun.SelectedLevel);if(reward>0){CoinManager.Instance?.AddCoins(reward);LevelRun.Instance?.HUD?.Toast("Weltwächter besiegt! Seltener Wächter-Schatz: +"+reward+" Münzen.");}});
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Boss · angekündigte Trefferzone";go.GetComponent<Collider>().enabled=false;Destroy(go.GetComponent<Collider>());warning=go.transform;warning.SetParent(transform.parent);warning.GetComponent<Renderer>().sharedMaterial=ComicPalette.Material("Bosswarnung",new Color(1,.2f,.07f));warning.gameObject.SetActive(false);
        }
        void Update()
        {
            var run=LevelRun.Instance;if(enemy==null || enemy.IsDefeated || run==null || !run.IsPlaying || run.Player==null)return;
            if(Vector3.Distance(run.Player.transform.position,transform.position)>10 && Phase==BossPhase.Watching)return;
            PhaseSeconds+=Time.deltaTime;next-=Time.deltaTime;
            if(Phase==BossPhase.Watching && next<=0)
            {
                attack=(attack+1)%2;lockedX=attack==0 ? transform.position.x : run.Player.transform.position.x;
                Phase=BossPhase.Warning;PhaseSeconds=0;next=1.2f;patrol.Suspended=true;applied=false;
                warning.position=new Vector3(lockedX,transform.position.y-1.02f,0);warning.localScale=new Vector3(attack==0 ? 5 : 1.6f,.10f,2.5f);warning.gameObject.SetActive(true);
                run.HUD?.Toast(attack==0 ? "Wächter holt aus – spring über die Bodenwelle!" : "Magieschlag markiert – verlasse die rote Fläche!");
            }
            else if(Phase==BossPhase.Warning && next<=0){Phase=BossPhase.Attack;PhaseSeconds=0;next=.3f;GetComponent<CharacterPresentation>()?.AttackPose();}
            else if(Phase==BossPhase.Attack)
            {
                if(!applied){applied=true;var p=run.Player.transform.position;float width=attack==0 ? 2.5f : .8f;bool inRange=Mathf.Abs(p.x-lockedX)<width+.45f && Mathf.Abs(p.z)<1;bool inHeight=attack==0 ? p.y<transform.position.y+.45f && p.y>transform.position.y-1.2f : Mathf.Abs(p.y-transform.position.y)<3;if(inRange && inHeight)run.Player.TakeDamage(1);GameEffects.Spark(new Vector3(lockedX,transform.position.y,0));}
                if(next<=0){Phase=BossPhase.Recovery;PhaseSeconds=0;next=1.5f;warning.gameObject.SetActive(false);run.HUD?.Toast("Wächter erschöpft – jetzt angreifen!");}
            }
            else if(Phase==BossPhase.Recovery && next<=0){Phase=BossPhase.Watching;PhaseSeconds=0;next=1.4f;patrol.Suspended=false;}
            if(Phase==BossPhase.Warning)warning.localScale=new Vector3(attack==0 ? 5 : 1.6f,.1f+Mathf.Sin(PhaseSeconds*18)*.04f,2.5f);
        }
        void OnDestroy(){if(warning!=null){Destroy(warning.GetComponent<Renderer>().sharedMaterial);Destroy(warning.gameObject);}}
    }
}
