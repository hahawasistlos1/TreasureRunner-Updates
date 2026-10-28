#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace TreasureRunner
{
    [InitializeOnLoad]public static class ExpansionReview
    {
        static ExpansionReview(){EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("TR.ExpansionReview",false)){SessionState.SetBool("TR.ExpansionReview",false);new GameObject("Expansion review").AddComponent<ExpansionReviewRunner>();}};}
        public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("TR.ExpansionReview",true);EditorApplication.EnterPlaymode();}
        public static void RunUIOnly(){SessionState.SetBool("TR.ExpansionReview.UIOnly",true);Run();}
    }
    public class ExpansionReviewRunner:MonoBehaviour
    {
        IEnumerator Start()
        {
            ShaderUtil.allowAsyncCompilation=false;DontDestroyOnLoad(gameObject);ProgressStore.BeginMemoryTest();ProgressStore.Data.highestUnlockedLevel=23;ProgressStore.Data.coins=2000;
            for(int id=0;id<80;id++)ProgressStore.Data.levels.Add(new LevelRecord{id=id,completions=1});
            Directory.CreateDirectory("Builds/ExpansionPreview");
            bool uiOnly=SessionState.GetBool("TR.ExpansionReview.UIOnly",false);SessionState.SetBool("TR.ExpansionReview.UIOnly",false);
            for(int world=uiOnly ? 8 : 0;world<8;world++)
            {
                int stage=new[]{0,1,4,6,5,3,8,7}[world];LevelRun.StartLevel(CampaignCatalog.Id(world,stage));yield return null;yield return null;
                foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))e.gameObject.SetActive(false);
                foreach(var trap in FindObjectsByType<WorldTrap>(FindObjectsSortMode.None))trap.enabled=false;
                var route=FindFirstObjectByType<LevelFactory>().Route;
                var wanted=new[]{EncounterRoom.SplitPath,EncounterRoom.Steps,EncounterRoom.SplitPath,EncounterRoom.Collapse,EncounterRoom.Tunnel,EncounterRoom.Bridges,EncounterRoom.Cannons,EncounterRoom.Lift}[world];
                var section=route.First(s=>s.room==wanted);
                var body=LevelRun.Instance.Player.GetComponent<Rigidbody>();body.useGravity=false;body.position=new Vector3(section.x+2,section.height+1.1f,0);body.linearVelocity=Vector3.zero;
                var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=body.position+new Vector3(5,3.8f,-19);camera.transform.rotation=Quaternion.Euler(8,0,0);
                foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.gameObject.SetActive(false);
                yield return null;yield return null;yield return Capture(camera,"World-"+world+"-Stage-"+(stage+1));
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");yield return null;yield return null;yield return null;
            var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Levelauswahl");yield return null;yield return UIShot("Levelauswahl");
            menu.Show("Shop");yield return null;yield return UIShot("Shop");
            LevelEditorScreen.OpenDocument(LevelCodec.Starter());yield return null;
            var paint=FindFirstObjectByType<EditorLevelScene>();paint.paint(9,1);paint.paint(10,2);paint.paint(11,3);yield return null;yield return UIShot("Level-Editor");
            var editor=FindFirstObjectByType<LevelEditorScreen>();editor.SendMessage("Close");yield return null;
            menu.Show("Community");yield return new WaitForSecondsRealtime(2);yield return UIShot("Community");
            var updates=GameUpdateClient.Instance;
            typeof(GameUpdateClient).GetMethod("Show",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(updates,new object[]{new UpdateRelease{tag_name="VORSCHAU",body="Dies ist die Vorschau des Update-Dialogs. Echte Updates zeigen hier ihre Versionsnummer, Änderungen und den Downloadfortschritt."}});
            yield return null;yield return UIShot("Update-Dialog-Vorschau");
            ProgressStore.EndMemoryTest();File.WriteAllText("Logs/ExpansionVisualReview.txt","Rendered 8 route-family views and 5 UI views in Unity. Update dialog is a synthetic preview; no phone test.");EditorApplication.Exit(0);
        }
        IEnumerator UIShot(string name)
        {
            var camera=Camera.main!=null ? Camera.main : FindFirstObjectByType<Camera>();camera.cullingMask=1<<5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=GameUI.Ink;
            foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;}
            Canvas.ForceUpdateCanvases();yield return null;yield return Capture(camera,name);
        }
        IEnumerator Capture(Camera camera,string name)
        {
            var rt=new RenderTexture(1560,720,24);camera.targetTexture=rt;camera.aspect=1560f/720;Canvas.ForceUpdateCanvases();camera.Render();yield return null;Canvas.ForceUpdateCanvases();camera.Render();
            var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1560,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1560,720),0,0);image.Apply();File.WriteAllBytes("Builds/ExpansionPreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Destroy(image);rt.Release();Destroy(rt);
        }
    }
}
#endif
