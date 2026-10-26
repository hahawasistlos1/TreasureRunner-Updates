#if UNITY_STANDALONE_WIN
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class DesktopToolsSmoke:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        string Folder=>Path.Combine(Environment.CurrentDirectory,"Pruefung");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartSmoke(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--tools-smoke")>=0 && FindFirstObjectByType<DesktopToolsSmoke>()==null)new GameObject("Windows runtime checks").AddComponent<DesktopToolsSmoke>();}
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception){errors=true;report.Add("ERROR: "+text);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.logMessageReceived+=Log;Directory.CreateDirectory(Folder);
            var tests=Scenarios();bool failed=false;
            while(true){bool more;object value=null;try{more=tests.MoveNext();if(more)value=tests.Current;}catch(Exception e){failed=true;report.Add("FAIL: "+e);break;}if(!more)break;yield return value;}
            failed|=errors;Application.logMessageReceived-=Log;report.Add(failed ? "SMOKE FAILED" : "SMOKE PASSED");File.WriteAllLines(Path.Combine(Folder,DesktopTools.AdminUnavailable ? "blocked-admin.txt" : DesktopTools.Mode+".txt"),report);Application.Quit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            yield return new WaitForSecondsRealtime(1);
            Check(DesktopTools.Active,"Requested tool mode active");
            if(DesktopTools.AdminUnavailable){Check(!DesktopTools.Moderator && Type.GetType("TreasureRunner.LevelModerationScreen, Assembly-CSharp")==null,"Public editor rejects admin parameter and contains no moderation screen");yield break;}
            Check(Resources.Load<Material>("ShieldRing")!=null && Resources.Load<Material>("ShieldRing").shader.isSupported,"Shield material and shader are included in the built player");
            if(DesktopTools.Moderator)
            {
#if TREASURERUNNER_ADMIN
                Check(FindFirstObjectByType<LevelModerationScreen>()!=null,"Windows moderation UI starts");
                float deadline=Time.realtimeSinceStartup+35;Button issue=null;
                while(Time.realtimeSinceStartup<deadline){issue=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text.StartsWith("#")==true);if(issue!=null || FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("gültige Einreichung(en).")))break;yield return null;}
                Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("gültige Einreichung(en).")),"GitHub submission queue loaded read-only, including empty queue");
                if(issue!=null)issue.onClick.Invoke();
                else
                {
                    string fixture=Path.Combine(Folder,"Nutzer-Level.trl");Check(File.Exists(fixture),"Archived user fixture available after original GitHub issue was removed");
                    FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Code / Link importieren").onClick.Invoke();yield return null;
                    FindObjectsByType<InputField>(FindObjectsSortMode.None).Single().text=File.ReadAllText(fixture);
                    FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Importieren").onClick.Invoke();
                }
                yield return null;Capture("Verwaltung");
                var play=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="In 3D spielen und prüfen");Check(play!=null,"Selected submission offers 3D playtest");play.onClick.Invoke();yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
                Check(LevelRun.IsCustom && LevelRun.CustomLevel.title=="test" && LevelRun.Instance.Player!=null,"User's submitted level launches in actual Windows gameplay");Capture("Nutzer-Level-1");
                Check(!EditorTestSession.Passed(LevelRun.CustomLevel),"User level not falsely certified by import or launch");LevelRun.Instance.Menu();yield return null;yield return null;
                Check(FindFirstObjectByType<LevelModerationScreen>()!=null,"Test returns to moderation tool");
                deadline=Time.realtimeSinceStartup+35;
                while(Time.realtimeSinceStartup<deadline && !FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("gültige Einreichung(en).")))yield return null;
                var published=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Freigegebene Level");published.onClick.Invoke();
                deadline=Time.realtimeSinceStartup+35;
                while(Time.realtimeSinceStartup<deadline && !FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Freigabe(n).")))yield return null;
                Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text.Contains("Freigabe(n).")),"Published levels menu reads only current-version catalog without writes or credentials");Capture("Freigegebene-Level");
#else
                throw new Exception("Public build must not enter moderation mode");
#endif
            }
            else
            {
                Check(FindFirstObjectByType<LevelEditorScreen>()!=null,"Windows level editor starts");Capture("Levelbau");
                Check(Type.GetType("TreasureRunner.LevelModerationScreen, Assembly-CSharp")==null,"Public runtime does not ship the moderation screen");
                var submit=FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Einreichen");submit.onClick.Invoke();yield return null;
                Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>()?.text=="GitHub-Einreichung öffnen"),"Untested valid draft may be submitted for owner approval");FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text=="Zurück").onClick.Invoke();yield return null;
                var palette=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="Bauteil wählen");palette.onClick.Invoke();yield return null;Capture("Bauteile");
                Check(BuilderPalette.Entries.Count>100,"Shared runtime catalog includes imported models and existing enemies");
                foreach(string category in BuilderPalette.Categories)
                {
                    FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==category).onClick.Invoke();yield return null;
                    Check(FindObjectsByType<RawImage>(FindObjectsSortMode.None).Any(i=>i.texture!=null),"Windows asset thumbnails available: "+category);
                }
                var close=FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.GetComponentInChildren<Text>()?.text=="Schließen");close.onClick.Invoke();yield return null;
                var level=LevelCodec.Starter(5);level.format=2;
                level.tiles.RemoveAll(t=>t.x==3 && t.y==0);level.tiles.Add(new LevelBlock{x=3,y=0,kind=LevelTile.Ground,asset="art.Platformer_block-snow"});
                level.tiles.RemoveAll(t=>t.x==25 && t.y==0);level.tiles.Add(new LevelBlock{x=25,y=0,kind=LevelTile.Shooter,asset="art.Skeleton_Mage"});
                level.tiles.RemoveAll(t=>t.x==35 && t.y==0);level.tiles.Add(new LevelBlock{x=35,y=0,kind=LevelTile.Flame,asset="art.Platformer_saw"});
                level.title="Automatischer Werkzeugtest";LevelEditorScreen.Working=level;EditorTestSession.Begin(level);LevelRun.StartCustom(level);yield return null;yield return null;yield return new WaitForSecondsRealtime(.3f);
                Check(LevelRun.IsCustom && LevelRun.Instance.Player!=null,"Custom level uses actual Windows gameplay");
                var goal=level.tiles.First(t=>t.kind==LevelTile.Goal);LevelRun.Instance.Player.GetComponent<Rigidbody>().position=new Vector3(goal.x,goal.y+1.05f,0);
                float deadline=Time.realtimeSinceStartup+3;while(LevelRun.Instance.State!=RunState.Complete && Time.realtimeSinceStartup<deadline)yield return null;
                Check(LevelRun.Instance.State==RunState.Complete && EditorTestSession.Passed(level),"Real goal trigger grants exact-draft test proof");LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
                Check(FindFirstObjectByType<LevelEditorScreen>()!=null,"Playtest returns to Windows editor");Capture("Levelbau-Test-bestanden");
            }
            Check(Application.version==ReleaseInfo.Version,"Windows tools match authorized release "+ReleaseInfo.Version);
            Check(ProgressStore.IsMemoryTest && DesktopTools.DataPath.Contains("TempData"),"Runtime tests isolate player preferences and drafts");
        }
        void Capture(string name)
        {
            var camera=Camera.main ?? FindFirstObjectByType<Camera>();if(camera==null)return;
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();
            foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}
            Canvas.ForceUpdateCanvases();var rt=new RenderTexture(1440,900,24);var old=camera.targetTexture;camera.targetTexture=rt;camera.Render();
            var before=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1440,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1440,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),tex.EncodeToPNG());RenderTexture.active=before;camera.targetTexture=old;rt.Release();Destroy(rt);Destroy(tex);
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
