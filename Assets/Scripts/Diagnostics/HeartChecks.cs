#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace TreasureRunner
{
    public class HeartChecks:MonoBehaviour
    {
        public static string ReportPath="Logs/HeartTests061.txt",PreviewDirectory="Builds/HeartPreview";
        readonly List<string> report=new List<string>();bool error;
        RenderTexture captureTarget;
        void Check(bool ok,string description){if(!ok)throw new Exception(description);report.Add("PASS: "+description);}
        void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){error=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;
            var tests=Scenarios();bool failed=false;
            while(true){bool more;object value=null;try{more=tests.MoveNext();if(more)value=tests.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return value;}
            failed|=error;Application.logMessageReceived-=Log;ProgressStore.EndMemoryTest();Time.timeScale=1;
            report.Add(failed ? "HEART TESTS FAILED" : "ALL HEART TESTS PASSED");Directory.CreateDirectory("Logs");File.WriteAllLines(ReportPath,report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            Directory.CreateDirectory(PreviewDirectory);ProgressStore.Data.coins=10000;
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();var movement=player.GetComponent<PlayerMovement>();
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))enemy.gameObject.SetActive(false);
            foreach(var trap in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))trap.enabled=false;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(40,1,3);
            body.useGravity=false;movement.ControlLocked=true;body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;
            player.invulnerabilitySeconds=.35f;Physics.SyncTransforms();PrepareCapture();yield return new WaitForSeconds(.15f);
            Check(player.health==3 && player.MaxHealth==3 && player.HeartUnits==6,"Campaign starts with three lives and three full hearts");
            var hearts=FindObjectsByType<HeartGraphic>(FindObjectsSortMode.None);Check(hearts.Length==3 && hearts.All(h=>h.Units==2),"HUD renders exactly three full vector hearts");
            int fullRedPixels=Capture("01-Drei-Herzen");Check(fullRedPixels>500,"Rendered HUD visibly contains three red hearts: "+fullRedPixels+" red pixels");
            var attacker=new GameObject("Actual enemy half-heart contact");attacker.SetActive(false);attacker.transform.position=body.position+Vector3.right*.7f;
            var enemyContact=attacker.AddComponent<Enemy>();enemyContact.maxHealth=20;enemyContact.contactDamage=99;enemyContact.speed=0;attacker.GetComponent<BoxCollider>().size=new Vector3(.65f,1.2f,.7f);attacker.SetActive(true);attacker.GetComponent<EnemyPatrol>().enabled=false;
            for(int i=0;i<4;i++)yield return new WaitForFixedUpdate();
            Check(player.HeartUnits==5 && player.health==3 && run.IsPlaying,"Real enemy contact removes half a heart even with large damage value");
            Check(Mathf.Abs(body.position.x-1000)<.1f,"Ordinary hit does not teleport or consume a life");
            player.TakeDamage(10);Check(player.HeartUnits==5,"Invulnerability prevents overlapping and repeated hits");Destroy(attacker);yield return new WaitForSeconds(.12f);
            Check(hearts.Sum(h=>h.Units)==5 && hearts.Count(h=>h.Units==1)==1,"HUD shows two full hearts and one half heart");int halfRedPixels=Capture("02-Halbes-Herz-verloren");Check(halfRedPixels<fullRedPixels*.95f && halfRedPixels>fullRedPixels*.6f,"Rendered half-heart loss is visibly different from three full hearts: "+halfRedPixels+" red pixels");
            run.Pause();player.TakeDamage(1);player.TakeFallDamage();Check(player.HeartUnits==5 && player.health==3,"Pause blocks ordinary and fall damage");run.Resume();
            yield return new WaitForSeconds(.4f);player.invulnerabilitySeconds=0;
            for(int hit=2;hit<=5;hit++){player.TakeDamage(999);Check(player.HeartUnits==6-hit && player.health==3 && run.IsPlaying,"Hit "+hit+" removes only another half heart");}
            player.TakeDamage(1);Check(player.HeartUnits==0 && player.health==2 && run.State==RunState.Recovering,"Sixth accepted hit consumes exactly one life");
            for(int i=0;i<5;i++){player.TakeDamage(1);player.TakeFallDamage();}
            Check(player.health==2 && player.HeartUnits==0,"Recovery cannot consume a second life from overlapping damage");yield return new WaitForSeconds(.12f);int emptyRedPixels=Capture("03-Herzen-leer-Leben-verloren");Check(emptyRedPixels<10,"Rendered empty hearts are no longer red");
            yield return new WaitForSeconds(.5f);
            Check(run.IsPlaying && player.health==2 && player.HeartUnits==6 && Mathf.Abs(body.position.x-run.Spawn.x)<.1f,"Lost life restores three hearts at the start without restoring lost lives");
            run.SetCheckpoint(new Vector3(1005,1.05f,0));body.position=new Vector3(1007,1.05f,0);movement.ControlLocked=true;yield return new WaitForSeconds(2.1f);
            for(int i=0;i<6;i++)player.TakeDamage(1);yield return new WaitForSeconds(.6f);
            Check(player.health==1 && player.HeartUnits==6 && Mathf.Abs(body.position.x-1005)<.1f,"Second empty heart meter consumes one life and returns to checkpoint");
            yield return new WaitForSeconds(2.1f);for(int i=0;i<6;i++)player.TakeDamage(1);yield return null;
            Check(player.health==0 && player.HeartUnits==0 && run.State==RunState.Dead && Time.timeScale==0,"Game Over occurs only when final life and all its hearts are exhausted");
            Check(GameObject.Find("Button Level neu starten")!=null,"Final death opens normal game-over actions");
            int revives=run.Revives;run.ReviveAtCheckpoint();yield return null;
            Check(run.IsPlaying && player.health==3 && player.HeartUnits==6 && run.Revives==revives-1,"Existing checkpoint revival refills both meters and consumes one revival");
            player.GrantInvulnerability(30);body.position=new Vector3(1007,-9,0);yield return null;yield return new WaitForSeconds(.6f);
            float recoveryDeadline=Time.realtimeSinceStartup+2;while(run.State==RunState.Recovering && Time.realtimeSinceStartup<recoveryDeadline)yield return null;
            Check(player.health==3 && player.HeartUnits==5 && run.IsPlaying && Mathf.Abs(body.position.x-1005)<.1f,"Fall during immunity loses half a heart and returns safely without costing a life: lives="+player.health+", hearts="+player.HeartUnits+", state="+run.State+", body="+body.position+", transform="+player.transform.position);
            body.position=new Vector3(1007,-9,0);yield return null;yield return new WaitForSeconds(.6f);
            recoveryDeadline=Time.realtimeSinceStartup+2;while((run.State==RunState.Recovering || player.HeartUnits!=4) && Time.realtimeSinceStartup<recoveryDeadline)yield return null;
            Check(player.HeartUnits==4 && player.health==3 && run.IsPlaying,"Separate falls each cost half a heart without refilling remaining hearts: hearts="+player.HeartUnits+", lives="+player.health+", state="+run.State);
            player.Heal(1);Check(player.HeartUnits==6 && player.health==3,"Heart healing restores one full heart without creating extra lives");player.Heal(int.MaxValue);Check(player.HeartUnits==6,"Healing caps safely at three hearts");
            player.RestoreFullHealth();player.TakeDamage(1);Check(player.HeartUnits==5,"Fresh hit after explicit full restoration damages hearts");
            var pickup=GameObject.CreatePrimitive(PrimitiveType.Sphere);pickup.name="Actual heart pickup";pickup.transform.position=body.position;pickup.GetComponent<Collider>().isTrigger=true;pickup.AddComponent<HealthPickup>();Physics.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            Check(pickup==null && player.HeartUnits==6 && player.health==3,"Actual healing pickup heals hearts and is collected once");
            var fullPickup=GameObject.CreatePrimitive(PrimitiveType.Sphere);fullPickup.transform.position=body.position;fullPickup.GetComponent<Collider>().isTrigger=true;fullPickup.AddComponent<HealthPickup>();Physics.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
            Check(fullPickup!=null && player.health==3,"Full-heart pickup is not wasted and never grants a life");Destroy(fullPickup);
            int coins=ProgressStore.Data.coins;ShopService.BuyUpgrade(GameConfig.Current,0);Check(ProgressStore.Data.coins==coins-100,"Existing life-upgrade purchase remains one wallet transaction");
            LevelRun.StartLevel(0);yield return null;yield return null;yield return null;run=LevelRun.Instance;player=run.Player;
            Check(player.health==4 && player.MaxHealth==4 && player.HeartUnits==6,"Life upgrade adds a life while heart count stays exactly three");
            Check(FindObjectsByType<HeartGraphic>(FindObjectsSortMode.None).Length==3,"Upgraded life counter still renders only three hearts");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;run=LevelRun.Instance;player=run.Player;
            Check(player.health==3 && player.HeartUnits==6 && ProgressStore.Data.upgrades[0]==1,"Editor/custom tests keep base lives/hearts and preserve campaign upgrades");
            var prior=new ProgressData{campaignRevision=1,coins=41};prior.upgrades[0]=2;Check(!CampaignMigration.Apply(prior,true) && prior.coins==41 && prior.upgrades[0]==2,"Heart update does not introduce another campaign reset");
            run.Menu();yield return null;yield return null;
        }
        void PrepareCapture()
        {
            var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=new Vector3(1002,3.7f,-12);camera.transform.rotation=Quaternion.Euler(8,0,0);
            captureTarget=new RenderTexture(1560,720,24);camera.targetTexture=captureTarget;camera.aspect=1560f/720;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;}
        }
        int Capture(string name)
        {
            var camera=Camera.main;Canvas.ForceUpdateCanvases();camera.Render();var before=RenderTexture.active;RenderTexture.active=captureTarget;
            var image=new Texture2D(1560,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1560,720),0,0);image.Apply();File.WriteAllBytes(PreviewDirectory+"/"+name+".png",image.EncodeToPNG());
            int red=image.GetPixels(15,600,215,120).Count(c=>c.r>.8f && c.g<.4f && c.b<.6f);RenderTexture.active=before;Destroy(image);return red;
        }
        void OnDestroy(){if(captureTarget!=null){captureTarget.Release();Destroy(captureTarget);}}
    }
}
#endif
