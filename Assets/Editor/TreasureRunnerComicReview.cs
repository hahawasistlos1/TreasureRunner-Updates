#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace TreasureRunner
{
    [InitializeOnLoad]
    public static class ComicReview
    {
        const string Pending="TreasureRunner.ComicReview.Pending";
        static ComicReview(){EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)){SessionState.SetBool(Pending,false);new GameObject("Comic visual review").AddComponent<ComicReviewRunner>();}};}
        public static void Run(){ShaderUtil.allowAsyncCompilation=false;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
    }
    public class ComicReviewRunner:MonoBehaviour
    {
        IEnumerator Start()
        {
            ShaderUtil.allowAsyncCompilation=false;DontDestroyOnLoad(gameObject);ProgressStore.BeginMemoryTest();ProgressStore.Data.highestUnlockedLevel=23;ProgressStore.Data.graphicsQuality=1;Directory.CreateDirectory("Builds/ComicPreview");
            for(int w=0;w<8;w++)
            {
                LevelRun.StartLevel(w*3);yield return null;yield return null;yield return new WaitForSecondsRealtime(.5f);
                var factory=FindFirstObjectByType<LevelFactory>();
                foreach(var e in FindObjectsByType<Enemy>(FindObjectsSortMode.None)){e.speed=0;var ranged=e.GetComponent<EnemyRangedAttack>();if(ranged!=null)ranged.enabled=false;}
                var route=factory.Route;
                for(int view=0;view<2;view++)
                {
                    int index=view==0 ? 0 : route.Count/2;var s=route[index];
                    var body=LevelRun.Instance.Player.GetComponent<Rigidbody>();body.useGravity=false;body.position=new Vector3(s.x+1.5f,s.height+1.1f,0);body.linearVelocity=Vector3.zero;
                    var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=body.position+new Vector3(3.2f,3.1f,-15);camera.transform.rotation=Quaternion.Euler(8,0,0);
                    yield return null;yield return null;
                    var rt=new RenderTexture(1560,720,24);camera.targetTexture=rt;camera.aspect=1560f/720;camera.Render();yield return null;camera.Render();
                    var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1560,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1560,720),0,0);image.Apply();
                    File.WriteAllBytes("Builds/ComicPreview/World-"+w+"-"+view+".png",image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Destroy(image);rt.Release();Destroy(rt);
                }
            }
            ProgressStore.EndMemoryTest();File.WriteAllText("Logs/ComicVisualReview.txt","Rendered 16 actual Unity camera views from all eight worlds.");EditorApplication.Exit(0);
        }
        void OnDestroy(){ProgressStore.EndMemoryTest();}
    }
}
#endif
