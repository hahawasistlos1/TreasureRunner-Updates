#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class TouchScalingChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool condition,string label){if(!condition)throw new Exception(label);report.Add("PASS: "+label);}
        void Logged(string message,string stack,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+message+"\n"+stack);}}
        static Rect Box(Vector2 center,float diameter)=>new Rect(center-Vector2.one*diameter/2,Vector2.one*diameter);
        static Button Button(string text)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(button=>button.GetComponentInChildren<Text>()?.text==text);
        static RectTransform Rect(string name)=>GameObject.Find(name).GetComponent<RectTransform>();
        string Hash(string path){using(var sha=SHA256.Create())using(var stream=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(stream));}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/TouchScaling");
            string save=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");string apk=Hash("Builds/Publish-0.8.0/TreasureRunner.apk");
            ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;bool failed=false;var sequence=Scenarios();
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception exception){failed=true;report.Add("FAIL: "+exception);break;}if(!more)break;yield return current;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Logged;Time.timeScale=1;
            try{Check(PlayerPrefs.GetString(ProgressStore.SaveKey,"")==save && PlayerPrefs.GetString(ProgressStore.BackupKey,"")==backup,"Real progress and backup unchanged");Check(Hash("Builds/Publish-0.8.0/TreasureRunner.apk")==apk,"Published APK unchanged");Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App version and Android code unchanged");}catch(Exception exception){failed=true;report.Add("FAIL: "+exception);}
            failed|=errors;report.Add(failed ? "TOUCH SCALING CHECKS FAILED" : "ALL TOUCH SCALING CHECKS PASSED");File.WriteAllLines("Logs/TouchScaling/Checks.txt",report);foreach(string line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var data=ProgressStore.Data;data.coins=777;data.weapons.Add("bow");
            var legacy=JsonUtility.FromJson<ProgressData>("{\"version\":1,\"coins\":93,\"movementArrows\":true}");
            Check(legacy.coins==93 && legacy.movementArrows,"Legacy save retains wallet and movement choice");
            foreach(var group in (TouchControlGroup[])Enum.GetValues(typeof(TouchControlGroup)))
            {
                foreach(float invalid in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})Check(TouchControlSizing.Normalize(invalid,group)==TouchControlSizing.Default(group),"Invalid or missing size falls back safely: "+group+" / "+invalid);
                Check(TouchControlSizing.Normalize(.1f,group)==.8f && TouchControlSizing.Normalize(99f,group)==1.8f,"Size bounded to safe range: "+group);
            }
            foreach(Vector2 size in new[]{new Vector2(1500,720),new Vector2(1280,720),new Vector2(1060,790),new Vector2(800,600)})
            foreach(bool arrows in new[]{true,false})for(int combination=0;combination<16;combination++)
            {
                float movement=(combination&1)==0 ? .8f : 1.8f,jump=(combination&2)==0 ? .8f : 1.8f,attack=(combination&4)==0 ? .8f : 1.8f,weapon=(combination&8)==0 ? .8f : 1.8f;
                var layout=TouchControlSizing.Calculate(size,arrows,movement,jump,attack,weapon);
                var rectangles=new List<Rect>{Box(new Vector2(size.x,0)+layout.jump,layout.jumpDiameter),Box(new Vector2(size.x,0)+layout.attack,layout.attackDiameter),Box(new Vector2(size.x,0)+layout.weapon,layout.weaponDiameter)};
                if(arrows){rectangles.Add(Box(layout.left,layout.arrowDiameter));rectangles.Add(Box(layout.right,layout.arrowDiameter));}else rectangles.Add(Box(layout.joystick,layout.joystickDiameter));
                Check(rectangles.All(rect=>rect.xMin>=0 && rect.yMin>=0 && rect.xMax<=size.x+.01f && rect.yMax<size.y*.865f),"All touch targets inside safe area and below HUD: "+size+" / "+arrows+" / "+combination);
                Check(!rectangles.Where((rect,i)=>rectangles.Skip(i+1).Any(other=>rect.Overlaps(other))).Any(),"Touch targets never overlap at mixed sizes: "+size+" / "+arrows+" / "+combination);
            }
            data.movementArrows=true;LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;Canvas.ForceUpdateCanvases();yield return null;
            var run=LevelRun.Instance;var player=run.Player;var movementComponent=player.GetComponent<PlayerMovement>();var body=player.GetComponent<Rigidbody>();player.GrantInvulnerability(999);
            var right=Rect("Button Rechts").GetComponent<TouchActionButton>();var jumpButton=Rect("Button Sprung");var attackButton=Rect("Button Angriff");
            Check(Mathf.Approximately(Rect("Button Rechts").sizeDelta.x,132) && Mathf.Approximately(jumpButton.sizeDelta.x,112.8f) && Mathf.Approximately(attackButton.sizeDelta.x,103.2f),"Pfeile, jump and attack use larger 120-percent defaults");
            Capture("Pfeile-Standard");yield return null;yield return null;
            float jumpBefore=jumpButton.sizeDelta.x;TouchControlSizing.Set(TouchControlGroup.Attack,1.8f);Check(Mathf.Approximately(attackButton.sizeDelta.x,154.8f) && Mathf.Approximately(jumpButton.sizeDelta.x,jumpBefore),"Attack can be resized independently");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(80,1,3);body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForSeconds(.25f);
            var finger=new PointerEventData(EventSystem.current){pointerId=881};right.OnPointerDown(finger);yield return new WaitForSeconds(.12f);Check(body.linearVelocity.x>0,"Enlarged arrow still drives real movement");TouchControlSizing.Set(TouchControlGroup.Movement,1.8f);yield return new WaitForSeconds(.12f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Resizing releases held arrow and prevents stuck movement");right.OnPointerUp(finger);
            TouchControlSizing.Set(TouchControlGroup.Jump,1.8f);TouchControlSizing.Set(TouchControlGroup.Weapon,1.8f);Canvas.ForceUpdateCanvases();
            foreach(string name in new[]{"Button Links","Button Rechts","Button Sprung","Button Angriff","Button Waffe"})
            {
                var rect=Rect(name);var point=rect.TransformPoint(rect.rect.center+new Vector2(rect.rect.width*.36f,0));var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,point)};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
                Check(hits.Any() && hits[0].gameObject.GetComponentInParent<Button>()?.gameObject==rect.gameObject,"Actual enlarged touch edge belongs to correct button: "+name);
            }
            var jumpAction=jumpButton.GetComponent<TouchActionButton>();float height=body.position.y;jumpAction.OnPointerDown(finger);jumpAction.OnPointerUp(finger);yield return new WaitForSeconds(.12f);Check(body.position.y>height+.2f && body.linearVelocity.y>0,"Scaled jump button still starts physical jump");yield return new WaitForSeconds(.9f);
            Capture("Pfeile-Maximal");yield return null;run.Pause();yield return null;Button("Tastengröße").onClick.Invoke();yield return null;
            Check(run.State==RunState.Paused && Time.timeScale==0,"Size dialog leaves gameplay paused");var panel=FindFirstObjectByType<TouchControlSizePanel>();Check(panel!=null && panel.GetComponentsInChildren<Slider>().Length==4,"Pause exposes four independent size sliders");
            panel.GetComponentsInChildren<Slider>().Single(slider=>slider.name=="Größe Sprung").value=8;Check(TouchControlSizing.Get(TouchControlGroup.Jump)==.8f && Mathf.Approximately(jumpButton.sizeDelta.x,75.2f),"Real pause slider updates jump target immediately");
            Canvas.ForceUpdateCanvases();Check(panel.GetComponentsInChildren<Slider>().All(slider=>Mathf.Approximately(slider.handleRect.rect.height,48)),"Slider grips stay within their rows");
            Capture("Pause-Tastengröße");yield return null;Button("Standardgrößen wiederherstellen").onClick.Invoke();Check(TouchControlSizing.Get(TouchControlGroup.Movement)==1.2f && TouchControlSizing.Get(TouchControlGroup.Jump)==1.2f && TouchControlSizing.Get(TouchControlGroup.Attack)==1.2f && TouchControlSizing.Get(TouchControlGroup.Weapon)==1,"Reset restores larger defaults only");
            Button("Fertig").onClick.Invoke();yield return null;Check(FindFirstObjectByType<TouchControlSizePanel>()==null && run.State==RunState.Paused,"Closing size dialog restores pause without canvas leak");
            Button("Tastengröße").onClick.Invoke();yield return null;run.Resume();yield return null;Check(FindFirstObjectByType<TouchControlSizePanel>()==null && run.State==RunState.Playing,"Resuming gameplay also closes size overlay");run.Pause();yield return null;
            Button("Joystick").onClick.Invoke();Button("Weiterspielen").onClick.Invoke();yield return null;TouchControlSizing.Set(TouchControlGroup.Movement,1.8f);var stick=FindFirstObjectByType<VirtualJoystick>();Check(stick!=null && Mathf.Approximately(((RectTransform)stick.transform).sizeDelta.x,302.4f),"Same movement setting scales joystick");
            stick.SetDeflection(Vector2.right);yield return new WaitForSeconds(.1f);TouchControlSizing.Set(TouchControlGroup.Movement,1f);yield return new WaitForSeconds(.1f);Check(stick.Axis==0 && Mathf.Abs(body.linearVelocity.x)<.01f,"Resizing releases joystick thumb and motion");TouchControlSizing.Set(TouchControlGroup.Movement,1.8f);BuilderChecks.Joystick(stick);Capture("Joystick-Maximal");yield return null;
            TouchControlSizing.Set(TouchControlGroup.Jump,1.3f);TouchControlSizing.Set(TouchControlGroup.Weapon,.8f);string json=JsonUtility.ToJson(data);var restored=JsonUtility.FromJson<ProgressData>(json);
            Check(restored.touchMovementScale==1.8f && restored.touchJumpScale==1.3f && restored.touchWeaponScale==.8f && restored.coins==777 && restored.weapons.Contains("bow"),"JSON round-trip retains sizes and unrelated progress");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;Check(Mathf.Approximately(Rect("Button Sprung").sizeDelta.x,122.2f),"Next level uses stored custom size");
            LevelRun.Instance.Menu();yield return null;yield return null;yield return null;var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Einstellungen");yield return null;Check(FindFirstObjectByType<TouchControlSizePanel>()!=null,"Settings expose the same scale controls");
            panel=FindFirstObjectByType<TouchControlSizePanel>();panel.GetComponentsInChildren<Slider>().Single(slider=>slider.name=="Größe Angriff").value=15;Check(TouchControlSizing.Get(TouchControlGroup.Attack)==1.5f,"Settings slider saves independently");
            var scroll=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First();scroll.verticalNormalizedPosition=0;yield return null;Capture("Einstellungen");
            Check(data.coins==777 && data.weapons.Contains("bow"),"Scaling does not spend coins or replace inventory");
        }
        void Capture(string name)
        {
            var camera=Camera.main??FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera");var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(2340,1080,24);var previous=camera.targetTexture;camera.targetTexture=rt;
            foreach(var canvas in canvases){canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)typeof(CanvasScaler).GetMethod("Handle",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(scaler,null);}
            Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(2340,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,2340,1080),0,0);image.Apply();File.WriteAllBytes("Logs/TouchScaling/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=oldCameras[i];}rt.Release();Destroy(rt);Destroy(image);
        }
    }
}
#endif
