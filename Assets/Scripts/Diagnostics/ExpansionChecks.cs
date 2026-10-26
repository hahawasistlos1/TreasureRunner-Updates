#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public static class ExpansionChecks
    {
        public static void Run(Action<bool,string> check)
        {
            check(CampaignCatalog.Count==80,"Expanded campaign contains 80 levels");
            var ids=new HashSet<int>();
            for(int w=0;w<8;w++)for(int s=0;s<10;s++)
            {int id=CampaignCatalog.Id(w,s);check(ids.Add(id) && CampaignCatalog.World(id)==w && CampaignCatalog.Stage(id)==s,"Stable catalog mapping "+w+"/"+s);}
            for(int id=0;id<24;id++)check(CampaignCatalog.World(id)==id/3 && CampaignCatalog.Stage(id)==id%3,"Legacy save ID retained: "+id);
            ProgressStore.Data.highestUnlockedLevel=3;
            check(CampaignCatalog.Unlocked(24) && !CampaignCatalog.Unlocked(25) && !CampaignCatalog.Unlocked(31),"Expansion unlocks by the preceding chapter, not highest numeric ID");
            ProgressStore.Data.levels.Add(new LevelRecord{id=24,completions=1});
            check(CampaignCatalog.Unlocked(25),"Completing expansion chapter unlocks its successor");
            ProgressStore.CompleteLevel(24,100,20,80);
            check(ProgressStore.Data.highestUnlockedLevel==3,"Expansion completion preserves old world progression");
            ProgressStore.Data.levels.Clear();ProgressStore.Data.coins=100000;
            for(int i=0;i<5;i++)
            {
                int coins=ProgressStore.Data.coins;var item=AdventureUpgrades.Items[i];AdventureUpgrades.Buy(i);
                check(AdventureUpgrades.Level(i)==1 && ProgressStore.Data.coins==coins-item.basePrice,"New upgrade purchase commits one wallet transaction: "+item.title);
                ProgressStore.Data.adventureUpgrades[i]=item.maxLevel;coins=ProgressStore.Data.coins;AdventureUpgrades.Buy(i);
                check(ProgressStore.Data.coins==coins,"Upgrade cap prevents repeat spending: "+item.title);
            }
            ProgressStore.Data.adventureUpgrades=new int[5];
            var level=LevelCodec.Starter();check(LevelCodec.Validate(level,out _),"Starter level is reachable");
            var copy=LevelCodec.Decode(LevelCodec.Encode(level));check(copy.tiles.Count==level.tiles.Count && copy.title==level.title,"Compressed level code round trip retains content");
            level.tiles.Add(new LevelBlock{x=2,y=0,kind=LevelTile.Ground});check(!LevelCodec.Validate(level,out _),"Duplicate level cells are rejected");level.tiles.RemoveAt(level.tiles.Count-1);
            level.tiles.Find(t=>t.kind==LevelTile.Goal).y=11;check(!LevelCodec.Validate(level,out _),"Disconnected goal is rejected");
            bool bad=false;try{LevelCodec.Decode("TRL1:broken");}catch(Exception){bad=true;}check(bad,"Corrupt level code is rejected");
            level=LevelCodec.Starter();level.tiles[0].kind=(LevelTile)999;check(!LevelCodec.Validate(level,out _),"Unknown imported object kinds are rejected");
            var catalog=JsonUtility.FromJson<CommunityCatalog>(Resources.Load<TextAsset>("CommunityStarter").text);
            foreach(var demo in catalog.levels)check(LevelCodec.Validate(demo,out _),"Bundled community level validates: "+demo.title);
            check(GameUpdateClient.IsNewer("v0.5.0","0.4.0") && !GameUpdateClient.IsNewer("0.5.0","0.5.0"),"Update notification selects newer releases only");
        }
    }
}
#endif
