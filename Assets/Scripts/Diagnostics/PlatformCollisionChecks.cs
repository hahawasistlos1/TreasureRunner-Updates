#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace TreasureRunner
{
    public class PlatformCollisionChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Logged(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/PlatformCollision");string save=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");var hashes=Directory.GetFiles("Builds/Publish-0.8.0").Where(p=>p.EndsWith(".apk") || p.EndsWith(".zip")).ToDictionary(p=>p,p=>PatchCodec.Hash(File.ReadAllBytes(p)));
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;bool failed=false;var scenarios=Scenarios();
            while(true){bool more;object item=null;try{more=scenarios.MoveNext();if(more)item=scenarios.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return item;}
            ProgressStore.EndMemoryTest();Time.timeScale=1;Application.logMessageReceived-=Logged;
            try{Check(save==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real progress and backup unchanged");Check(hashes.All(p=>PatchCodec.Hash(File.ReadAllBytes(p.Key))==p.Value),"Published APK and archives unchanged");Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App and Android versions unchanged");}catch(Exception e){failed=true;report.Add("FAIL: "+e);}
            failed|=errors;report.Add(failed ? "PLATFORM COLLISION CHECKS FAILED" : "ALL PLATFORM COLLISION CHECKS PASSED");File.WriteAllLines("Logs/PlatformCollision/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        GameObject Cube(string name,Vector3 position,Vector3 size){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.position=position;cube.transform.localScale=size;return cube;}
        IEnumerator Scenarios()
        {
            var level=LevelCodec.Starter();level.format=2;level.tiles.Add(new LevelBlock{x=10,y=3,kind=LevelTile.Lift});level.tiles.Add(new LevelBlock{x=14,y=3,kind=LevelTile.Lift,asset="art.Platformer_block-moving"});Check(LevelCodec.Validate(level,out string fixtureReason),"Custom platform fixture validates: "+fixtureReason);
            LevelRun.StartCustom(level);yield return null;yield return null;yield return null;var run=LevelRun.Instance;var player=run.Player;var body=player.GetComponent<Rigidbody>();var movement=player.GetComponent<PlayerMovement>();var collider=player.GetComponent<Collider>();player.GrantInvulnerability(999);
            foreach(var ride in FindObjectsByType<MovingRide>(FindObjectsSortMode.None)){Check(ride.GetComponent<Rigidbody>().isKinematic && !ride.GetComponent<Collider>().isTrigger && ride.GetComponent<OneWayLedge>()==null,"Editor lift is a solid kinematic body: "+ride.name);ride.enabled=false;}
            var floor=Cube("Collision test floor",new Vector3(1000,-.5f,0),new Vector3(80,1,3));
            body.position=new Vector3(996,1.02f,0);body.linearVelocity=Vector3.zero;var side=Cube("Solid side test ride",new Vector3(1000,1,0),new Vector3(2,2,2));var sideways=side.AddComponent<MovingRide>();sideways.travel=Vector3.zero;
            yield return new WaitForSeconds(.2f);movement.SetTouchDirection(1,true);for(int n=0;n<65;n++)yield return new WaitForFixedUpdate();movement.ClearInput();Check(body.position.x<998.6f && body.position.x>997.5f,"Walking into lift side cannot pass through it: x="+body.position.x);Destroy(side);yield return null;
            foreach(var travel in new[]{new Vector3(3,0,0),new Vector3(0,2.6f,0),new Vector3(1.6f,1.8f,0)})
            {
                var lift=Cube("Ride stability "+travel,new Vector3(1000,2,0),new Vector3(3,.3f,2));var ride=lift.AddComponent<MovingRide>();ride.travel=travel;ride.period=2;
                body.position=new Vector3(1000,3.2f,0);body.linearVelocity=Vector3.zero;movement.ClearInput();Physics.SyncTransforms();yield return null;
                float maxGap=0,minGap=100,maxOffset=0;int supported=0;for(int step=0;step<230;step++){yield return new WaitForFixedUpdate();float gap=collider.bounds.min.y-lift.GetComponent<Collider>().bounds.max.y;maxGap=Mathf.Max(maxGap,gap);minGap=Mathf.Min(minGap,gap);if(step>15){maxOffset=Mathf.Max(maxOffset,Mathf.Abs(body.position.x-lift.GetComponent<Rigidbody>().position.x));if(movement.IsGrounded)supported++;}}
                Check(minGap>-.12f && maxGap<.25f,"Rider remains on surface through repeated up/down/reverse motion: "+travel+" gaps="+minGap+".."+maxGap);
                Check(maxOffset<.15f && supported>195,"Rider keeps position and grounded contact: "+travel+" offset="+maxOffset+" grounded="+supported);
                run.Pause();Vector3 stop=body.position,platformStop=lift.GetComponent<Rigidbody>().position;yield return new WaitForSecondsRealtime(.2f);Check(Vector3.Distance(stop,body.position)<.001f && Vector3.Distance(platformStop,lift.GetComponent<Rigidbody>().position)<.001f,"Pause freezes rider and platform");run.Resume();
                movement.RequestJump();float startY=body.position.y;for(int step=0;step<7;step++)yield return new WaitForFixedUpdate();Check(body.position.y>startY+.35f && movement.SupportRide==null,"Jump detaches from moving support: "+travel);
                Destroy(lift);yield return null;body.position=new Vector3(996,1.02f,0);body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.3f);
            }
            var overhead=Cube("Solid underside ride",new Vector3(1000,3.0f,0),new Vector3(3,.4f,2));var overheadRide=overhead.AddComponent<MovingRide>();overheadRide.travel=Vector3.zero;body.position=new Vector3(1000,1.02f,0);body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.2f);movement.RequestJump();float highest=0;for(int n=0;n<35;n++){yield return new WaitForFixedUpdate();highest=Mathf.Max(highest,collider.bounds.max.y);}Check(highest<2.9f,"Jump from below hits lift underside instead of passing through: head="+highest);Destroy(overhead);yield return null;
            var legacy=Cube("Legacy one-way lift",new Vector3(1000,1,0),new Vector3(3,.3f,2));legacy.AddComponent<OneWayLedge>();legacy.AddComponent<MovingRide>().travel=Vector3.zero;body.position=new Vector3(1000,1,0);body.linearVelocity=Vector3.zero;yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();Check(!Physics.GetIgnoreCollision(legacy.GetComponent<Collider>(),collider),"Legacy one-way component cannot disable a moving ride collision");Destroy(legacy);yield return null;
            var treasure=Cube("Yellow treasure block",new Vector3(1000,1,0),Vector3.one);treasure.GetComponent<Renderer>().sharedMaterial=ComicPalette.Material("Test gold",GameUI.Gold);treasure.AddComponent<TreasureBlock>();body.position=new Vector3(996,1.02f,0);body.linearVelocity=Vector3.zero;yield return new WaitForSeconds(.2f);movement.SetTouchDirection(1,true);for(int n=0;n<65;n++)yield return new WaitForFixedUpdate();movement.ClearInput();Check(body.position.x<999.1f,"Yellow treasure block blocks running from side");Destroy(treasure);yield return null;
            Destroy(floor);
            var regression=ExpansionRuntimeChecks.Run(Check);while(regression.MoveNext())yield return regression.Current;
        }
    }
}
#endif
