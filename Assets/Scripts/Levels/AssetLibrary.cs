using System;
using System.Linq;
using UnityEngine;

namespace TreasureRunner
{
    [Serializable] public class LibraryPack
    {
        public string id,title,author,url,license,licenseResource;
    }
    [Serializable] public class LibraryEntry
    {
        public string id,title,pack,category,resource,weapon;
        public int kind,role;
        public float height=2;
        public bool animated;
        public string[] frames;
    }
    [Serializable] public class LibraryCharacter {public string id,title,resource,ability;}
    [Serializable] public class LibraryAudio
    {
        public string resource,pack,title,use;
    }
    [Serializable] public class LibraryImage
    {
        public string resource,pack,title,use,id;
    }
    [Serializable] public class LibraryData
    {
        public int schema=1;
        public LibraryPack[] packs=Array.Empty<LibraryPack>();
        public LibraryEntry[] entries=Array.Empty<LibraryEntry>();
        public LibraryAudio[] audio=Array.Empty<LibraryAudio>();
        public LibraryImage[] images=Array.Empty<LibraryImage>();
        public LibraryCharacter[] characters=Array.Empty<LibraryCharacter>();
    }
    // The index contains paths only. Opening a palette never loads the whole model library.
    public static class AssetLibrary
    {
        static LibraryData data;
        public static LibraryData Data
        {
            get
            {
                if(data!=null)return data;
                var json=Resources.Load<TextAsset>("AssetLibrary/catalog");
                data=json!=null ? JsonUtility.FromJson<LibraryData>(json.text) : new LibraryData();
                if(data==null || data.schema!=1)data=new LibraryData();
                data.packs=data.packs??Array.Empty<LibraryPack>();data.entries=data.entries??Array.Empty<LibraryEntry>();data.audio=data.audio??Array.Empty<LibraryAudio>();
                data.images=data.images??Array.Empty<LibraryImage>();
                data.characters=data.characters??Array.Empty<LibraryCharacter>();
                return data;
            }
        }
        public static LibraryEntry Get(string id)=>Data.entries.FirstOrDefault(e=>e.id==id);
        public static LibraryEntry[] Pack(string id)=>Data.entries.Where(e=>e.pack==id).ToArray();
        public static GameObject Model(LibraryEntry entry)=>entry==null || string.IsNullOrEmpty(entry.resource) ? null : Resources.Load<GameObject>(entry.resource);
        public static GameObject First(string pack,params string[] words)
        {
            var entries=Pack(pack);
            var candidate=entries.FirstOrDefault(e=>words.Any(w=>e.title.IndexOf(w,StringComparison.OrdinalIgnoreCase)>=0));
            return Model(candidate??entries.FirstOrDefault());
        }
        public static bool Available=>Data.entries.Length>0;
        public static void Refresh(){data=null;BuilderPalette.Refresh();CharacterCatalog.Refresh();LibraryWorldArt.Refresh();}
    }
}
