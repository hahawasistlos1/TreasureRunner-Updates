using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    [Serializable] public class LevelIssue {public int number;public string title,body,html_url,updated_at;public IssuePullRequest pull_request;}
    [Serializable] public class IssuePullRequest {public string url;}
    [Serializable] public class LevelIssueList {public LevelIssue[] items;}
    [Serializable] public class RepositoryFile {public string sha,content,encoding;}
    [Serializable] public class CatalogWrite {public string message,content,sha,branch="TennoFarm";}
    [Serializable] public class CatalogCreate {public string message,content,branch="TennoFarm";}
    public static class LevelModeration
    {
        public const string Api="https://api.github.com/repos/hahawasistlos1/TreasureRunner-Updates";
        public static bool IsOwner(string login)=>string.Equals(login,"hahawasistlos1",StringComparison.OrdinalIgnoreCase);
        public const string LegacyPath="community/catalog.json", AssetPath="community/catalog-v2.json", NextPath="community/catalog-v3.json";
        public const int Protocol=4;
        public const int MaxCatalogBytes=2097152;
        public const string CurrentPath="community/"+ReleaseInfo.Version+"/catalog.json";
        public const string Empty="{\"version\":1,\"moderationProtocol\":4,\"gameVersion\":\""+ReleaseInfo.Version+"\",\"levels\":[]}";
        public static string WriteBody(string message,string json,string sha)
        {
            string content=Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            return string.IsNullOrEmpty(sha) ? JsonUtility.ToJson(new CatalogCreate{message=message,content=content}) : JsonUtility.ToJson(new CatalogWrite{message=message,content=content,sha=sha});
        }
        public static readonly string[] CatalogPaths={CurrentPath};
        public static bool CanPublish(CommunityLevel level)=>LevelCodec.Validate(level,out _) && (level.format<=3 || ReleaseInfo.Version!="0.9.0");
        public static string PathFor(CommunityLevel l)=>CurrentPath;
        public static bool Compatible(CommunityCatalog catalog)=>catalog!=null && catalog.moderationProtocol==Protocol && catalog.gameVersion==ReleaseInfo.Version;
        public static CommunityCatalog ReadCurrentCatalog(string json)
        {
            var catalog=ReadCatalog(json);
            if(!Compatible(catalog))throw new FormatException("Dieser Community-Katalog gehört zu einer anderen Spiel-/Verwaltungsversion.");
            return catalog;
        }
        public static string Withdraw(string json,string id,string expectedHash)
        {
            var catalog=ReadCurrentCatalog(json);var item=catalog.levels.FirstOrDefault(l=>l.id==id);
            if(item==null)throw new InvalidOperationException("Das Level ist bereits entfernt. Liste aktualisieren.");
            if(EditorTestSession.Fingerprint(item)!=expectedHash)throw new InvalidOperationException("Die freigegebene Fassung wurde geändert. Liste aktualisieren.");
            catalog.levels=catalog.levels.Where(l=>l.id!=id).ToArray();return JsonUtility.ToJson(catalog,true);
        }
        public static CommunityCatalog Combine(params CommunityCatalog[] catalogs)
        {
            if(catalogs.Any(c=>!Compatible(c)))throw new FormatException("Katalogversion nicht kompatibel.");
            return new CommunityCatalog{moderationProtocol=Protocol,gameVersion=ReleaseInfo.Version,levels=catalogs.SelectMany(c=>c.levels).GroupBy(l=>l.id).Select(g=>g.OrderByDescending(l=>l.revision).ThenByDescending(l=>l.format).First()).ToArray()};
        }
        public static CommunityLevel FromIssue(LevelIssue issue)
        {
            if(issue==null || issue.number<1 || issue.pull_request?.url!=null || string.IsNullOrEmpty(issue.body))throw new FormatException("Keine Level-Einreichung.");
            var level=LevelCodec.Decode(issue.body);
            if(!LevelCodec.Validate(level,out var reason))throw new FormatException(reason);
            if(string.IsNullOrWhiteSpace(level.id))throw new FormatException("Die Einreichung benötigt eine eindeutige Level-ID.");
            return level;
        }
        public static CommunityCatalog ReadCatalog(string json)
        {
            if(json==null || System.Text.Encoding.UTF8.GetByteCount(json)>MaxCatalogBytes)throw new FormatException("Katalog zu groß.");
            var catalog=JsonUtility.FromJson<CommunityCatalog>(json);
            if(catalog==null || catalog.version!=1 || catalog.levels==null || catalog.levels.Length>30)throw new FormatException("Ungültiger Katalog.");
            var ids=new HashSet<string>();
            foreach(var l in catalog.levels)
                if(!LevelCodec.Validate(l,out _) || string.IsNullOrWhiteSpace(l.id) || !ids.Add(l.id))throw new FormatException("Katalog enthält ungültige oder doppelte Level.");
            return catalog;
        }
        public static string Merge(string json,CommunityLevel level,string expectedHash)
        {
            if(!LevelCodec.Validate(level,out var why))throw new FormatException(why);
            if(!CanPublish(level))throw new InvalidOperationException("Neue Asset-Level sind vor Veröffentlichung einer passenden Spielversion nur lokal testbar.");
            if(string.IsNullOrWhiteSpace(level.id))throw new FormatException("Level-ID fehlt.");
            if(EditorTestSession.Fingerprint(level)!=expectedHash)throw new InvalidOperationException("Diese Fassung wurde inzwischen geändert. Neu laden.");
            var catalog=ReadCurrentCatalog(json);var list=catalog.levels.ToList();int old=list.FindIndex(l=>l.id==level.id);
            if(old>=0 && list[old].revision>=level.revision)throw new InvalidOperationException("Diese oder eine neuere Fassung ist bereits freigegeben.");
            if(old>=0)list[old]=level;else list.Add(level);
            if(list.Count>30)throw new InvalidOperationException("Der Katalog enthält bereits 30 Level.");
            catalog.levels=list.ToArray();string result=JsonUtility.ToJson(catalog,true);
            ReadCurrentCatalog(result);return result;
        }
    }
}
