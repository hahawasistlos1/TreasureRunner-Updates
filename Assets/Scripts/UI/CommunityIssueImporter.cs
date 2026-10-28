using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
namespace TreasureRunner
{
    public class CommunityIssueImporter:MonoBehaviour
    {
        [Serializable]class Issue{public string body;}
        public static bool IsReference(string value,out int number)
        {
            number=0;if(!Uri.TryCreate(SharedLinkInput.Extract(value),UriKind.Absolute,out var uri) || uri.Scheme!="https" || uri.Host!="github.com" || !uri.IsDefaultPort || uri.UserInfo!="")return false;
            var parts=uri.AbsolutePath.Trim('/').Split('/');return parts.Length==4 && parts[0]=="hahawasistlos1" && parts[1]=="TreasureRunner-Updates" && parts[2]=="issues" && int.TryParse(parts[3],out number) && number>0;
        }
        public void Fetch(int number,Action<CommunityLevel> accept,Action<string> status)=>StartCoroutine(Download(number,accept,status));
        IEnumerator Download(int number,Action<CommunityLevel> accept,Action<string> status)
        {
            status("Lade Einreichung #"+number+" …");
            using(var request=new UnityWebRequest("https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates/issues/"+number,UnityWebRequest.kHttpVerbGET))
            {
                request.downloadHandler=new BoundedCatalogDownload();request.timeout=20;request.SetRequestHeader("Accept","application/vnd.github+json");
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){status("Einreichung nicht erreichbar. Du kannst auch den Level-Code aus dem GitHub-Beitrag kopieren.");yield break;}
                CommunityLevel level=null;string error=null;
                try{var issue=JsonUtility.FromJson<Issue>(request.downloadHandler.text);level=LevelCodec.Decode(issue.body);if(!LevelCodec.Validate(level,out error))level=null;}
                catch(Exception){error="Die Einreichung enthält keinen gültigen Level-Code.";}
                if(level==null){status(error);yield break;}accept(level);
            }
        }
    }
}
