using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
namespace TreasureRunner
{
    public enum LevelTile { Ground, Coin, Enemy, Shooter, Hopper, Charger, Crumble, Lift, Checkpoint, Goal, Flame, Cannon, FallingRock, Spring }
    [Serializable] public class LevelBlock { public int x,y;public LevelTile kind;public string asset=""; }
    [Serializable] public class CommunityLevel
    {
        public int format=1,world,width=64,height=12,spawnX=2,spawnY=0,revision=1;
        public string id="",title="Mein Abenteuer",author="Schatzbauer";
        public List<LevelBlock> tiles=new List<LevelBlock>();
    }
    [Serializable] public class CommunityCatalog { public int version=1,moderationProtocol;public string gameVersion="";public CommunityLevel[] levels; }
    public static class LevelCodec
    {
        public const int MaxWidth=384,MaxHeight=32,MaxTiles=2400,MaxCodeLength=400000,MaxDecodedBytes=524288;
        public static int Cell(int x,int y)=>y*MaxWidth+x;
        public static int RequiredFormat(CommunityLevel level)=>level.width>96 || level.height>16 || level.tiles.Count>650 ? 5 : level.tiles.Where(t=>!string.IsNullOrEmpty(t.asset)).Select(t=>BuilderPalette.Get(t.asset)?.format??3).DefaultIfEmpty(1).Max();
        public static bool Floor(LevelTile kind)=>kind!=LevelTile.Coin && kind!=LevelTile.FallingRock;
        public static bool Validate(CommunityLevel l,out string reason)
        {
            reason="";
            if(l==null || (l.format<1 || l.format>5)){reason="Unbekanntes Level-Format.";return false;}
            if(l.world<0 || l.world>7 || l.width<16 || l.width>MaxWidth || l.height<8 || l.height>MaxHeight || l.tiles==null || l.tiles.Count>MaxTiles || (l.format<5 && (l.width>96 || l.height>16 || l.tiles.Count>650)) ||
                string.IsNullOrWhiteSpace(l.title) || l.title.Length>40 || string.IsNullOrWhiteSpace(l.author) || l.author.Length>24 || l.revision<1 || l.revision>10000 || l.id==null || l.id.Length>64)
            {reason="Level-Daten liegen außerhalb der erlaubten Grenzen.";return false;}
            var cells=new HashSet<int>();var floors=new List<LevelBlock>();LevelBlock goal=null;
            foreach(var tile in l.tiles)
            {
                if(tile==null || tile.x<0 || tile.x>=l.width || tile.y<0 || tile.y>=l.height || !Enum.IsDefined(typeof(LevelTile),tile.kind) || !cells.Add(Cell(tile.x,tile.y)))
                {reason="Ungültiges oder doppelt belegtes Feld.";return false;}
                if(!string.IsNullOrEmpty(tile.asset))
                {
                    var definition=BuilderPalette.Get(tile.asset);
                    if(tile.asset.Length>120 || definition==null || l.format<definition.format || definition.kind!=tile.kind){reason="Unbekannte oder unpassende Bauteilvariante.";return false;}
                }
                if(Floor(tile.kind))floors.Add(tile);
                if(tile.kind==LevelTile.Goal){if(goal!=null){reason="Nur ein Ziel ist erlaubt.";return false;}goal=tile;}
            }
            if(l.spawnX<0 || l.spawnX>=l.width || l.spawnY<0 || l.spawnY>=l.height || !floors.Exists(t=>t.x==l.spawnX && t.y==l.spawnY))
            {reason="Am Start fehlt ein Bodenfeld.";return false;}
            if(goal==null){reason="Setze ein Zielportal.";return false;}
            // Conservative static reachability: does not promise timing/skill, but rejects disconnected goals.
            var reached=new HashSet<int>{Cell(l.spawnX,l.spawnY)};var pending=new Queue<Vector2Int>();pending.Enqueue(new Vector2Int(l.spawnX,l.spawnY));
            while(pending.Count>0)
            {
                var other=pending.Dequeue();
                foreach(var tile in floors)
                {
                    int key=Cell(tile.x,tile.y);if(reached.Contains(key))continue;
                    if(JumpProfile.CanCross(Mathf.Max(0,Mathf.Abs(tile.x-other.x)-1),tile.y-other.y))
                    {reached.Add(key);pending.Enqueue(new Vector2Int(tile.x,tile.y));}
                }
            }
            if(!reached.Contains(Cell(goal.x,goal.y))){reason="Das Ziel ist vom Start aus nicht über Boden und Sprünge erreichbar.";return false;}
            return true;
        }
        public static string Encode(CommunityLevel level)
        {
            byte[] raw=Encoding.UTF8.GetBytes(JsonUtility.ToJson(level));
            using(var data=new MemoryStream())
            {
                using(var zip=new DeflateStream(data,System.IO.Compression.CompressionLevel.Optimal,true))zip.Write(raw,0,raw.Length);
                return "TRL1:"+Convert.ToBase64String(data.ToArray());
            }
        }
        public static CommunityLevel Decode(string code)
        {
            if(string.IsNullOrWhiteSpace(code) || code.Length>MaxCodeLength)throw new FormatException("Level-Code fehlt oder ist zu groß.");
            code=code.Trim();int start=code.IndexOf("TRL1:",StringComparison.Ordinal);
            if(start<0)throw new FormatException("Level-Code muss mit TRL1: beginnen.");
            string payload=code.Substring(start+5).Trim();int end=payload.IndexOfAny(new[]{'\r','\n',' ','`'});if(end>=0)payload=payload.Substring(0,end);
            byte[] packed=Convert.FromBase64String(payload);
            using(var source=new MemoryStream(packed))using(var zip=new DeflateStream(source,CompressionMode.Decompress))using(var target=new MemoryStream())
            {
                var buffer=new byte[2048];int n;
                while((n=zip.Read(buffer,0,buffer.Length))>0){if(target.Length+n>MaxDecodedBytes)throw new FormatException("Level entpackt zu groß.");target.Write(buffer,0,n);}
                return JsonUtility.FromJson<CommunityLevel>(Encoding.UTF8.GetString(target.ToArray()));
            }
        }
        public static CommunityLevel Empty(int world=0)
        {
            return new CommunityLevel{format=5,width=MaxWidth,height=MaxHeight,world=Mathf.Clamp(world,0,7),id=Guid.NewGuid().ToString("N"),author=ProgressStore.Data.playerName};
        }
        public static CommunityLevel Starter(int world=0)
        {
            var l=new CommunityLevel{world=world,id=Guid.NewGuid().ToString("N"),author=ProgressStore.Data.playerName};
            for(int x=0;x<64;x++)l.tiles.Add(new LevelBlock{x=x,y=0,kind=x==61 ? LevelTile.Goal : x==28 ? LevelTile.Checkpoint : LevelTile.Ground});
            for(int x=7;x<58;x+=7)l.tiles.Add(new LevelBlock{x=x,y=2,kind=LevelTile.Coin});
            return l;
        }
    }
    public static class LocalLevels
    {
        static string DirectoryPath=>Path.Combine(DesktopTools.DataPath,"my-levels");
        public static void SaveDraft(CommunityLevel level){Directory.CreateDirectory(DirectoryPath);File.WriteAllText(Path.Combine(DirectoryPath,"editor-draft.trl"),LevelCodec.Encode(level));}
        public static CommunityLevel LoadDraft(){try{return LevelCodec.Decode(File.ReadAllText(Path.Combine(DirectoryPath,"editor-draft.trl")));}catch(Exception){return null;}}
        public static void Save(CommunityLevel level,int slot)
        {
            if(slot<0 || slot>7)throw new ArgumentOutOfRangeException(nameof(slot));
            Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,"slot-"+slot+".trl"),temp=path+".tmp";
            File.WriteAllText(temp,LevelCodec.Encode(level));if(File.Exists(path))File.Copy(path,path+".backup",true);File.Copy(temp,path,true);File.Delete(temp);
        }
        public static CommunityLevel Load(int slot)
        {
            try{return LevelCodec.Decode(File.ReadAllText(Path.Combine(DirectoryPath,"slot-"+slot+".trl")));}catch(Exception){return null;}
        }
        public static void Share(CommunityLevel level)
        {
            string code=LevelCodec.Encode(level);GUIUtility.systemCopyBuffer=code;
#if UNITY_ANDROID && !UNITY_EDITOR
            using(var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=unity.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var bridge=new AndroidJavaClass("com.treasurerunner.updates.UpdateBridge"))bridge.CallStatic("shareLevel",activity,level.title+"\n"+code);
#endif
        }
    }
}
