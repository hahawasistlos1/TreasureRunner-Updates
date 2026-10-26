#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class Release09Checks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Reject(Action operation,string label){bool rejected=false;try{operation();}catch(Exception){rejected=true;}Check(rejected,label);}
        Button Button(string label)=>FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()?.text==label);
        void Logged(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors=true;report.Add("ERROR: "+text);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs/Release09");var save=PlayerPrefs.GetString(ProgressStore.SaveKey,"");var backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Logged;bool failed=false;var sequence=Scenarios();
            while(true){bool more;object item=null;try{more=sequence.MoveNext();if(more)item=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return item;}
            ProgressStore.EndMemoryTest();Application.logMessageReceived-=Logged;Check(save==PlayerPrefs.GetString(ProgressStore.SaveKey,"") && backup==PlayerPrefs.GetString(ProgressStore.BackupKey,""),"Real save and backup unchanged");Time.timeScale=1;failed|=errors;report.Add(failed ? "RELEASE09 CHECKS FAILED" : "ALL RELEASE09 CHECKS PASSED");File.WriteAllLines("Logs/Release09/Checks.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        IEnumerator Scenarios()
        {
            var draft=LevelCodec.Starter();Check(!EditorTestSession.Passed(draft),"Fixture has never been completed");
            string hash=EditorTestSession.Fingerprint(draft);string catalog=LevelModeration.Merge(LevelModeration.Empty,draft,hash);
            Check(LevelModeration.ReadCurrentCatalog(catalog).levels.Single().id==draft.id,"Owner can prepare approval of a valid uncompleted draft");
            Check(LevelModeration.ReadCurrentCatalog(LevelModeration.Withdraw(catalog,draft.id,hash)).levels.Length==0,"Owner can withdraw uncompleted level without requiring playthrough");
            Reject(()=>LevelModeration.Merge(catalog,draft,hash),"Duplicate approved revision rejected");
            draft.revision++;Reject(()=>LevelModeration.Merge(catalog,draft,hash),"Concurrent draft change rejected");hash=EditorTestSession.Fingerprint(draft);
            catalog=LevelModeration.Merge(catalog,draft,hash);Check(LevelModeration.ReadCurrentCatalog(catalog).levels.Single().revision==2,"New revision replaces prior approval");
            Reject(()=>LevelModeration.Withdraw(catalog,draft.id,"stale"),"Withdrawal cannot erase a changed approval");
            foreach(var old in new[]{"{\"version\":1,\"levels\":[]}",LevelModeration.Empty.Replace("0.9.0","0.8.0"),LevelModeration.Empty.Replace("\"moderationProtocol\":4","\"moderationProtocol\":3")})
            {Reject(()=>LevelModeration.ReadCurrentCatalog(old),"New game rejects old moderation contract");Reject(()=>LevelModeration.Merge(old,draft,hash),"New tool cannot write legacy contract");}
            Check(LevelModeration.CatalogPaths.SequenceEqual(new[]{"community/0.9.0/catalog.json"}),"New game reads only exact-version catalog outside all old tool paths");
            Check(LevelModeration.IsOwner("hahawasistlos1") && LevelModeration.IsOwner("HAHAWASISTLOS1") && !LevelModeration.IsOwner(null) && !LevelModeration.IsOwner("someplayer"),"Only configured owner login accepted for moderation");
            var missingGoal=LevelCodec.Decode(LevelCodec.Encode(draft));missingGoal.tiles.RemoveAll(t=>t.kind==LevelTile.Goal);Reject(()=>LevelModeration.Merge(LevelModeration.Empty,missingGoal,EditorTestSession.Fingerprint(missingGoal)),"Structural validation still rejects missing goal");
            Check(LevelCodec.Validate(LevelCodec.Decode(File.ReadAllText("Documentation/UserLevel-Issue-1.trl")),out _),"User submission remains importable without conversion");
            LevelRun.StartCustom(LevelCodec.Starter());yield return null;yield return null;yield return null;LevelRun.Instance.Menu();yield return null;yield return null;yield return null;
            LevelEditorScreen.Working=draft;LevelEditorScreen.Open();yield return null;Button("Einreichen").onClick.Invoke();yield return null;
            Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>()?.text=="GitHub-Einreichung öffnen"),"Real editor opens submission dialog without a completed test");Button("Zurück").onClick.Invoke();yield return null;
            Check(!FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>()?.text=="Für alle freigeben …"),"Creator editor does not offer public approval");
            var administration=new GameObject("Admin UI test").AddComponent<LevelModerationScreen>();administration.Build();administration.StopAllCoroutines();
            typeof(LevelModerationScreen).GetField("level",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,draft);
            typeof(LevelModerationScreen).GetField("sessionToken",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,"fixture-not-a-token");
            typeof(LevelModerationScreen).GetField("busy",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(administration,false);
            administration.SendMessage("Details");administration.SendMessage("Confirm");yield return null;
            Check(FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b=>b.GetComponentInChildren<Text>()?.text=="Jetzt in den Katalog übernehmen"),"Owner confirmation is available without a completed test; no approval request sent");Button("Abbrechen").onClick.Invoke();
            typeof(LevelModerationScreen).GetField("sessionToken",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,"");
            Check(JsonUtility.FromJson<IdentityConfig>(Resources.Load<TextAsset>("IdentityConfig").text).Ready,"Owner Google client configuration is present before release");
            Check(Application.version=="0.9.0" && PlayerSettings.Android.bundleVersionCode==10,"Explicitly authorized version is 0.9.0 / code 10");
        }
    }
}
#endif
