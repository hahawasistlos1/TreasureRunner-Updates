using System;
using System.Collections;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
namespace TreasureRunner
{
    [Serializable] public class LevelReview {public string uid,nickname,comment;public int stars,revision;}
    public static class ReviewPolicy
    {
        public const int PageSize=20,MaxComment=400;
        public static string Key(string id)
        {
            if(string.IsNullOrWhiteSpace(id) || id.Length>64)throw new ArgumentException("Level-ID fehlt.");
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-","").ToLowerInvariant();
        }
        public static bool Valid(int stars,string comment)=>stars>=1 && stars<=5 && comment!=null && comment.Length<=MaxComment && !comment.Any(c=>char.IsControl(c) && c!='\n') && !comment.Contains('\u202e');
        public static string Stars(int value)=>new string('★',Mathf.Clamp(value,0,5))+new string('☆',5-Mathf.Clamp(value,0,5));
    }
    // Public reads work in both tools and Android. Only the native Firebase session can write.
    public sealed class CommunityReviews:MonoBehaviour
    {
        [Serializable] class Value {public string stringValue,integerValue;}
        [Serializable] class Fields {public Value nickname,comment,stars,revision;}
        [Serializable] class Document {public string name;public Fields fields;}
        [Serializable] class Result {public Document document;}
        [Serializable] class Response {public Result[] items;}
        [Serializable] class Reply {public int request;public string state;}
        [Serializable] class Write {public string key,comment;public int stars,revision;}
        UnityWebRequest pending;Action<bool> completion;int sequence;float deadline;
        public bool Busy=>completion!=null;
        public void Fetch(string key,string cursor,Action<LevelReview[],string> complete)=>StartCoroutine(Read(key,cursor,complete));
        IEnumerator Read(string key,string cursor,Action<LevelReview[],string> complete)
        {
            if(!Regex.IsMatch(key??"",@"\A[a-f0-9]{64}\z") || (cursor!="" && !Regex.IsMatch(cursor??"",@"\A[A-Za-z0-9_-]{1,128}\z"))){complete(null,"Ungültige Bewertungsadresse.");yield break;}
            var config=JsonUtility.FromJson<IdentityConfig>(Resources.Load<TextAsset>("IdentityConfig").text);
            string database="projects/"+config.firebaseProjectId+"/databases/(default)/documents";
            string start=string.IsNullOrEmpty(cursor) ? "" : ",\"startAt\":{\"values\":[{\"referenceValue\":\""+database+"/levelReviews/"+key+"/entries/"+cursor+"\"}],\"before\":false}";
            string json="{\"structuredQuery\":{\"from\":[{\"collectionId\":\"entries\"}],\"orderBy\":[{\"field\":{\"fieldPath\":\"__name__\"},\"direction\":\"ASCENDING\"}],\"limit\":20"+start+"}}";
            using(pending=new UnityWebRequest("https://firestore.googleapis.com/v1/"+database+"/levelReviews/"+key+":runQuery","POST"))
            {
                pending.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));pending.downloadHandler=new BoundedCatalogDownload();pending.SetRequestHeader("Content-Type","application/json");pending.timeout=20;
                yield return pending.SendWebRequest();
                if(pending.result!=UnityWebRequest.Result.Success){complete(null,pending.responseCode==403 ? "Bewertungen sind serverseitig noch nicht freigeschaltet." : "Bewertungen gerade nicht erreichbar. Bitte später erneut versuchen.");yield break;}
                LevelReview[] reviews=null;
                try
                {
                    var response=JsonUtility.FromJson<Response>("{\"items\":"+pending.downloadHandler.text+"}");
                    reviews=(response.items??Array.Empty<Result>()).Where(r=>r.document?.fields!=null).Select(r=>{
                        var f=r.document.fields;return new LevelReview{uid=r.document.name.Split('/').Last(),nickname=f.nickname?.stringValue,comment=f.comment?.stringValue??"",stars=int.Parse(f.stars.integerValue),revision=int.Parse(f.revision.integerValue)};
                    }).Where(r=>NicknamePolicy.Valid(r.nickname) && ReviewPolicy.Valid(r.stars,r.comment)).Take(ReviewPolicy.PageSize).ToArray();
                }
                catch(Exception){ }
                complete(reviews,reviews==null ? "Bewertungen konnten nicht gelesen werden." : "");
            }
            pending=null;
        }
        public void Save(string key,int revision,int stars,string comment,Action<bool> done)
        {
            if(!ReviewPolicy.Valid(stars,comment)){done(false);return;}Call("saveReview",JsonUtility.ToJson(new Write{key=key,revision=revision,stars=stars,comment=comment}),done);
        }
        public void Delete(string key,Action<bool> done)=>Call("deleteReview",key,done);
        void Call(string method,string payload,Action<bool> done)
        {
            if(Busy || GoogleIdentity.Current.State!=IdentityState.Ready || ProgressStore.IsReadOnly){done(false);return;}
#if UNITY_ANDROID && !UNITY_EDITOR
            completion=done;sequence++;deadline=Time.unscaledTime+30;
            try{using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))using(var bridge=new AndroidJavaClass("com.treasurerunner.identity.CommunityReviewBridge"))bridge.CallStatic(method,activity,gameObject.name,sequence,payload);}
            catch(AndroidJavaException){Finish(false);}
#else
            done(false);
#endif
        }
        [UnityEngine.Scripting.Preserve] public void OnReviewResult(string json)
        {
            if(!Busy || json==null || json.Length>256)return;
            try{var reply=JsonUtility.FromJson<Reply>(json);if(reply.request==sequence)Finish(reply.state=="ok");}catch(ArgumentException){ }
        }
        void Finish(bool success){var done=completion;completion=null;done?.Invoke(success);}
        void Update(){if(Busy && Time.unscaledTime>deadline){sequence++;Finish(false);}}
        void OnDestroy(){pending?.Abort();completion=null;}
    }
}
