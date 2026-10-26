#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
namespace TreasureRunner
{
    public class ControlChoiceChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string description){if(!ok)throw new Exception(description);report.Add("PASS: "+description);}
        void Logged(string text,string stack,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        Button Button(string text)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==text);
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/ControlChoicePreview");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;
            var sequence=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            ProgressStore.EndMemoryTest();if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real save changed");}else report.Add("PASS: Real save and backup untouched");Time.timeScale=1;Application.logMessageReceived-=Logged;failed|=errors;report.Add(failed ? "CONTROL CHOICE TESTS FAILED" : "ALL CONTROL CHOICE TESTS PASSED");File.WriteAllLines("Logs/ControlChoiceTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            string legacy=JsonUtility.ToJson(new ProgressData{coins=93}).Replace(",\"movementArrows\":false","");var old=JsonUtility.FromJson<ProgressData>(legacy);Check(!old.movementArrows && old.coins==93,"Existing saves retain joystick default and wallet");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;
            var run=LevelRun.Instance;var player=run.Player;var movement=player.GetComponent<PlayerMovement>();var body=player.GetComponent<Rigidbody>();var stick=FindFirstObjectByType<VirtualJoystick>();
            Check(stick!=null && !GameObject.Find("Bewegungspfeile"),"Joystick alone is active by default");Check(Mathf.Approximately(((RectTransform)stick.transform).sizeDelta.x,201.6f) && Mathf.Approximately(stick.knob.sizeDelta.x,81.6f),"Joystick and grip use new 120-percent default");
            var jump=GameObject.Find("Button Sprung").GetComponent<RectTransform>();var attack=GameObject.Find("Button Angriff").GetComponent<RectTransform>();var swap=GameObject.Find("Button Waffe").GetComponent<RectTransform>();Check(Mathf.Approximately(jump.sizeDelta.x,112.8f) && Mathf.Approximately(attack.sizeDelta.x,103.2f) && Mathf.Approximately(swap.sizeDelta.x,72),"Jump and attack default to 120 percent; weapon button retains original default");
            BuilderChecks.Joystick(stick);Check(true,"Joystick analog input, dead zone, finger ownership and disable release remain correct");Capture("Joystick");
            run.Pause();yield return new WaitForSecondsRealtime(.25f);Canvas.ForceUpdateCanvases();var pauseColumn=(RectTransform)Button("Weiterspielen").transform.parent;Check(LayoutUtility.GetPreferredHeight(pauseColumn)<=pauseColumn.rect.height+2,"Pause choices and existing actions fit inside the available menu height");Capture("Pausenauswahl");Button("Bewegungspfeile").onClick.Invoke();Check(ProgressStore.Data.movementArrows && run.State==RunState.Paused,"Pause selector stores arrow choice without leaving pause");Check(!stick.gameObject.activeSelf,"Choosing arrows disables joystick");Button("Weiterspielen").onClick.Invoke();yield return null;
            var left=GameObject.Find("Button Links").GetComponent<TouchActionButton>();var right=GameObject.Find("Button Rechts").GetComponent<TouchActionButton>();Check(left.direction==-1 && right.direction==1 && left.isActiveAndEnabled && right.isActiveAndEnabled,"Classic left and right arrows are active with correct directions");Capture("Bewegungspfeile");
            // Capture temporarily changes canvas dimensions; allow the restored layout to settle before holding a finger.
            yield return null;yield return new WaitForFixedUpdate();
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(50,1,3);body.useGravity=false;body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;player.GrantInvulnerability(999);Physics.SyncTransforms();
            var first=new PointerEventData(EventSystem.current){pointerId=71};var second=new PointerEventData(EventSystem.current){pointerId=72};
            ExecuteEvents.Execute(right.gameObject,first,ExecuteEvents.pointerDownHandler);yield return new WaitForSeconds(.14f);Check(body.linearVelocity.x>movement.moveSpeed*.9f,"Holding right arrow actually moves player right");
            ExecuteEvents.Execute(left.gameObject,second,ExecuteEvents.pointerDownHandler);yield return new WaitForSeconds(.10f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Holding both arrows cancels movement");
            ExecuteEvents.Execute(right.gameObject,first,ExecuteEvents.pointerUpHandler);yield return new WaitForSeconds(.10f);Check(body.linearVelocity.x< -movement.moveSpeed*.9f,"Releasing right while holding left moves player left");
            ExecuteEvents.Execute(left.gameObject,second,ExecuteEvents.pointerExitHandler);yield return new WaitForSeconds(.10f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Sliding finger off arrow stops movement");
            right.OnPointerDown(first);right.OnPointerDown(second);right.OnPointerUp(first);yield return new WaitForSeconds(.10f);Check(body.linearVelocity.x>0,"One remaining finger continues holding an arrow");right.OnPointerUp(second);yield return new WaitForSeconds(.10f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Last finger release stops arrow movement");
            right.OnPointerDown(first);run.Pause();Button("Weiterspielen").onClick.Invoke();yield return new WaitForSeconds(.10f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Pause clears held arrows before resuming");
            right.OnPointerDown(first);right.SendMessage("OnApplicationFocus",false);right.OnPointerDown(second);right.OnPointerUp(second);yield return new WaitForSeconds(.10f);Check(Mathf.Abs(body.linearVelocity.x)<.01f,"Focus loss clears old arrow fingers and prevents stuck input");
            run.Pause();Button("Joystick").onClick.Invoke();Button("Weiterspielen").onClick.Invoke();yield return null;Check(!ProgressStore.Data.movementArrows && stick.isActiveAndEnabled && !left.gameObject.activeInHierarchy,"Switching back activates only joystick");stick.SetDeflection(Vector2.right);yield return new WaitForSeconds(.12f);Check(body.linearVelocity.x>movement.moveSpeed*.9f,"Larger joystick still drives actual player physics");run.Pause();Button("Bewegungspfeile").onClick.Invoke();Button("Weiterspielen").onClick.Invoke();yield return new WaitForSeconds(.10f);Check(stick.Axis==0 && Mathf.Abs(body.linearVelocity.x)<.01f,"Switching modes does not carry joystick motion into arrows");
            string json=JsonUtility.ToJson(ProgressStore.Data);Check(JsonUtility.FromJson<ProgressData>(json).movementArrows,"Arrow choice survives save JSON round-trip");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;Check(GameObject.Find("Button Links")!=null && FindFirstObjectByType<VirtualJoystick>()==null,"Next level restores selected arrow controls");LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
            var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Einstellungen");yield return null;Button("Joystick").onClick.Invoke();yield return null;Check(!ProgressStore.Data.movementArrows,"Settings page selects and stores joystick");Button("Bewegungspfeile").onClick.Invoke();yield return null;Check(ProgressStore.Data.movementArrows && FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Bewegung: Bewegungspfeile"),"Settings page selects arrows and immediately refreshes its displayed choice");
            foreach(var scroll in FindObjectsByType<ScrollRect>(FindObjectsSortMode.None))scroll.verticalNormalizedPosition=0;yield return new WaitForSecondsRealtime(.25f);Capture("Einstellungen");
            Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"App and Android versions unchanged");
        }
        void Capture(string name)
        {
            var camera=Camera.main??FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera");var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(1600,900,24);var beforeTarget=camera.targetTexture;camera.targetTexture=rt;
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Builds/ControlChoicePreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=beforeTarget;rt.Release();Destroy(rt);Destroy(image);for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
