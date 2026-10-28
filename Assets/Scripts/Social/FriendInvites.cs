using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
namespace TreasureRunner
{
    [Serializable] public class FriendContact { public string id; public string name; }
    public static class FriendInvites
    {
        public const string WebRoot="https://hahawasistlos1.github.io/treasurerunner/invite/";
        public const int MaxContacts=200, MaxUrl=1024;
        static readonly Regex IdPattern=new Regex("^[0-9a-f]{32}$",RegexOptions.CultureInvariant);
        public static bool ValidId(string id)=>id!=null && id.Length==32 && IdPattern.IsMatch(id);
        public static string CleanName(string value)
        {
            if(string.IsNullOrWhiteSpace(value))return "Schatzjäger";
            // Restrict display names to plain text; no markup, control or invisible direction characters.
            var chars=value.Where(c=>char.IsLetterOrDigit(c) || c==' ' || c=='-' || c=='_' || c=='.').Take(20).ToArray();
            string result=new string(chars).Trim();return result.Length==0 ? "Schatzjäger" : result;
        }
        public static string Create(string id,string name)
        {
            if(!ValidId(id))throw new ArgumentException("Ungültige Kontakt-ID");
            return WebRoot+"#v=1&id="+id+"&name="+Uri.EscapeDataString(CleanName(name));
        }
        public static bool TryParse(string text,out FriendContact contact,out string reason)
        {
            contact=null;reason="Das ist kein gültiger TreasureRunner-Einladungslink.";text=SharedLinkInput.Extract(text);
            if(string.IsNullOrEmpty(text) || text.Length>MaxUrl || text.Any(char.IsControl) || !Uri.TryCreate(text,UriKind.Absolute,out var uri))return false;
            bool web=uri.Scheme=="https" && uri.Host=="hahawasistlos1.github.io" && uri.IsDefaultPort && uri.AbsolutePath=="/treasurerunner/invite/";
            bool app=uri.Scheme=="treasurerunner" && uri.Host=="invite" && uri.IsDefaultPort && (uri.AbsolutePath=="" || uri.AbsolutePath=="/");
            if((!web && !app) || uri.UserInfo!="" || uri.Query!="" || uri.Fragment.Length<2)return false;
            var fields=new Dictionary<string,string>(StringComparer.Ordinal);
            try{
                foreach(string field in uri.Fragment.Substring(1).Split('&')){
                    int split=field.IndexOf('=');if(split<1)return false;
                    string raw=field.Substring(split+1);if(Regex.IsMatch(raw,"%(?![0-9a-fA-F]{2})"))return false;
                    string key=field.Substring(0,split),value=Uri.UnescapeDataString(raw);
                    if((key!="v" && key!="id" && key!="name") || fields.ContainsKey(key) || value.Contains("%") || value.Any(char.IsControl))return false;
                    fields.Add(key,value);
                }
            }catch(UriFormatException){return false;}
            if(fields.Count!=3 || !fields.TryGetValue("v",out var version) || version!="1" || !fields.TryGetValue("id",out var id) || !ValidId(id) || !fields.TryGetValue("name",out var name) || name.Length<1 || name.Length>80)return false;
            contact=new FriendContact{id=id,name=CleanName(name)};reason=null;return true;
        }
        public static IReadOnlyList<FriendContact> Contacts {get{Ensure();return ProgressStore.Data.friends.AsReadOnly();}}
        public static string OwnId {get{Ensure();return ProgressStore.Data.friendId;}}
        static void Ensure()
        {
            var data=ProgressStore.Data;bool changed=false;
            if(!ValidId(data.friendId)){data.friendId=Guid.NewGuid().ToString("N");changed=true;}
            if(data.friends==null){data.friends=new List<FriendContact>();changed=true;}
            var ids=new HashSet<string>();
            var clean=data.friends.Where(c=>c!=null && ValidId(c.id) && c.id!=data.friendId && ids.Add(c.id)).Take(MaxContacts).ToList();
            if(clean.Count!=data.friends.Count)changed=true;
            foreach(var c in clean){string name=CleanName(c.name);if(name!=c.name){c.name=name;changed=true;}}
            data.friends=clean;if(changed)ProgressStore.Save();
        }
        public static string MyLink()=>Create(OwnId,ProgressStore.Data.playerName);
        public static bool Add(FriendContact contact,out string message)
        {
            Ensure();message="Kontakt konnte nicht gespeichert werden.";
            if(ProgressStore.IsReadOnly)return false;
            if(contact==null || !ValidId(contact.id))return false;
            if(contact.id==OwnId){message="Das ist deine eigene Einladung.";return false;}
            if(Contacts.Any(c=>c.id==contact.id)){message="Dieser Kontakt ist bereits in deiner Liste.";return false;}
            if(Contacts.Count>=MaxContacts){message="Deine Kontaktliste ist voll (200 Kontakte).";return false;}
            ProgressStore.Data.friends.Add(new FriendContact{id=contact.id,name=CleanName(contact.name)});ProgressStore.Save();message="Kontakt auf diesem Gerät gespeichert.";return true;
        }
        public static bool Remove(string id)
        {
            Ensure();if(ProgressStore.IsReadOnly)return false;
            if(ProgressStore.Data.friends.RemoveAll(c=>c.id==id)==0)return false;
            ProgressStore.Save();return true;
        }
        public static string Share()
        {
            string link=MyLink();GUIUtility.systemCopyBuffer=link;
#if UNITY_ANDROID && !UNITY_EDITOR
            try{
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var bridge=new AndroidJavaClass("com.treasurerunner.updates.UpdateBridge"))bridge.CallStatic("shareInvite",activity,"Komm mit auf Schatzsuche! Meine TreasureRunner-Einladung:\n"+link);
                return "Teilen geöffnet. Wähle selbst eine App und einen Empfänger.";
            }catch(AndroidJavaException){return "Einladungslink kopiert. Füge ihn in deiner Nachrichten-App ein.";}
#else
            return "Einladungslink kopiert. Füge ihn in deiner Nachrichten-App ein.";
#endif
        }
    }
}
