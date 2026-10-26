#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;
namespace TreasureRunner
{
    public static class ExpansionRuntimeChecks
    {
        public static IEnumerator Run(Action<bool,string> check)
        {
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var body=run.Player.GetComponent<Rigidbody>();var movement=run.Player.GetComponent<PlayerMovement>();
            foreach(var e in UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
            foreach(var t in UnityEngine.Object.FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))t.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(24,1,3);
            body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;
            var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.position=new Vector3(1000,3.2f,0);block.transform.localScale=Vector3.one*.85f;
            var treasure=block.AddComponent<TreasureBlock>();int coins=ProgressStore.Data.coins;
            yield return new WaitForFixedUpdate();movement.Bounce(7);yield return new WaitForSeconds(.35f);
            check(treasure.Claimed && ProgressStore.Data.coins==coins+5,"Jumping into treasure block awards its coins once");UnityEngine.Object.Destroy(block);
            var gate=GameObject.CreatePrimitive(PrimitiveType.Cube);gate.transform.position=new Vector3(1004,1.5f,0);
            var pad=GameObject.CreatePrimitive(PrimitiveType.Cube);pad.transform.position=new Vector3(1000,.12f,0);pad.transform.localScale=new Vector3(1.8f,.3f,2);pad.GetComponent<Collider>().isTrigger=true;
            var pressure=pad.AddComponent<PressureGate>();pressure.gate=gate;body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            check(pressure.Opened && !gate.GetComponent<Collider>().enabled,"Stepping on pressure switch opens physical gate");UnityEngine.Object.Destroy(gate);UnityEngine.Object.Destroy(pad);
            var lift=GameObject.CreatePrimitive(PrimitiveType.Cube);lift.transform.position=new Vector3(1000,.2f,0);lift.transform.localScale=new Vector3(3,.3f,2);
            var ride=lift.AddComponent<MovingRide>();ride.travel=new Vector3(2,0,0);ride.period=2;body.position=new Vector3(1000,1.36f,0);body.linearVelocity=Vector3.zero;
            yield return new WaitForSeconds(.45f);check(lift.transform.position.x>1000.3f && body.position.x>1000.2f,"Moving platform carries standing player");
            run.Pause();var stopped=lift.transform.position;yield return new WaitForSecondsRealtime(.15f);check(Vector3.Distance(stopped,lift.transform.position)<.01f,"Pause freezes moving platforms");run.Resume();UnityEngine.Object.Destroy(lift);
            body.useGravity=false;body.position=new Vector3(997,1,0);body.linearVelocity=Vector3.zero;run.Player.GrantInvulnerability(10);
            foreach(var tactic in new[]{EnemyTactic.Hopper,EnemyTactic.Charger})
            {
                var go=new GameObject("Isolated "+tactic);go.SetActive(false);go.transform.position=new Vector3(1000,.6f,0);var enemy=go.AddComponent<Enemy>();enemy.speed=1;
                go.AddComponent<EnemyAwareness>();var behavior=go.AddComponent<EnemyTactics>();behavior.tactic=tactic;go.SetActive(true);
                yield return new WaitForSeconds(1.25f);
                if(tactic==EnemyTactic.Hopper)check(behavior.Actions>0 && go.transform.position.y>.8f,"Nearby player triggers actual enemy hopping");
                else{check(behavior.WindingUp && go.GetComponent<EnemyPatrol>().Suspended,"Charging enemy telegraphs before attacking");yield return new WaitForSeconds(.7f);check(behavior.Actions>0 && enemy.speed>1,"Charging enemy accelerates after warning");}
                UnityEngine.Object.Destroy(go);yield return null;
            }
            var ledge=GameObject.CreatePrimitive(PrimitiveType.Cube);ledge.transform.position=new Vector3(1000,1.4f,0);ledge.AddComponent<OneWayLedge>();body.position=new Vector3(1000,1,0);
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();check(Physics.GetIgnoreCollision(ledge.GetComponent<Collider>(),run.Player.GetComponent<Collider>()),"Upper secret path allows passage beneath it");
            body.position=new Vector3(1000,3,0);body.linearVelocity=Vector3.zero;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();check(!Physics.GetIgnoreCollision(ledge.GetComponent<Collider>(),run.Player.GetComponent<Collider>()),"Upper secret path supports landing from above");
            UnityEngine.Object.Destroy(ledge);UnityEngine.Object.Destroy(floor);
        }
    }
}
#endif
