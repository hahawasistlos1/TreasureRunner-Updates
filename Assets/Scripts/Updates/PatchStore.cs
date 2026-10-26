using System;
using System.IO;
using System.Text;
using UnityEngine;
namespace TreasureRunner
{
    [Serializable] public class PatchJournal
    {public int schema=1,blockedThroughRevision;public string appVersion;public PatchPackage active,previous;}
    // Atomic verified data and recovery copy. There is no player-controlled rollback.
    public class PatchStore
    {
        public readonly string DirectoryPath,AppVersion;public readonly string StatePath;
        PatchJournal journal;public DataPatch Active {get;private set;}
        public PatchManifest Manifest=>journal.active?.manifest;
        public int Revision=>Active?.revision??0;public int BlockedThrough=>journal.blockedThroughRevision;
        public PatchStore(string directory,string version){DirectoryPath=Path.GetFullPath(directory);StatePath=Path.Combine(DirectoryPath,"state.json");AppVersion=version;journal=Empty();}
        PatchJournal Empty()=>new PatchJournal{appVersion=AppVersion};
        bool Read(string path,out PatchJournal value)
        {
            value=null;try{if(!File.Exists(path) || new FileInfo(path).Length>1048576)return false;value=JsonUtility.FromJson<PatchJournal>(File.ReadAllText(path));return value!=null && value.schema==1 && value.appVersion==AppVersion && value.blockedThroughRevision>=0 && value.blockedThroughRevision<=1000000;}catch(Exception){return false;}
        }
        public void Load()
        {
            Active=null;journal=Empty();
            if(!Read(StatePath,out var loaded) && !Read(StatePath+".backup",out loaded))return;
            journal=loaded;if(journal.active==null)return;
            if(PatchCodec.Package(journal.active,AppVersion,out var current,out _)){Active=current;return;}
            journal.blockedThroughRevision=Mathf.Max(journal.blockedThroughRevision,Mathf.Clamp(journal.active.manifest?.revision??0,0,1000000));
            if(PatchCodec.Package(journal.previous,AppVersion,out current,out _)){journal.active=journal.previous;journal.previous=null;Active=current;}
            else{journal.active=null;journal.previous=null;}
        }
        public bool Install(PatchPackage package,out string reason)
        {
            if(!PatchCodec.Package(package,AppVersion,out var patch,out reason))return false;
            if(patch.revision<=Mathf.Max(Revision,BlockedThrough)){reason="Dieser Patch ist bereits vorhanden, älter oder als beschädigt gesperrt.";return false;}
            // Clone caller-owned data. No later mutation can alter verified active settings.
            var owned=JsonUtility.FromJson<PatchPackage>(JsonUtility.ToJson(package));
            var next=new PatchJournal{appVersion=AppVersion,blockedThroughRevision=BlockedThrough,active=owned,previous=journal.active};
            if(!Write(next,out reason))return false;journal=next;Active=patch;return true;
        }
        bool Write(PatchJournal next,out string reason)
        {
            reason="";string temporary=StatePath+".tmp";
            try{Directory.CreateDirectory(DirectoryPath);File.WriteAllText(temporary,JsonUtility.ToJson(next),new UTF8Encoding(false));if(!Read(temporary,out _))throw new IOException("Journal validation");if(File.Exists(StatePath))File.Replace(temporary,StatePath,StatePath+".backup");else File.Move(temporary,StatePath);return true;}
            catch(Exception){reason="Spielpatch konnte nicht gespeichert werden. Der bisherige Stand bleibt aktiv.";return false;}
            finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(Exception){}}
        }
    }
}
