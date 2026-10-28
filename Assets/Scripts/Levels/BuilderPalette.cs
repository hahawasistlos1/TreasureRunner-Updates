using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public enum BuilderRole { Default,Block,Coin,Enemy,Decoration,Spikes,Saw,Weapon,Health,Shield,Cannon,Flame,Spring,FallingRock }
    public class BuilderAsset
    {
        public string id,title,category,prefab,weapon;public LevelTile kind;public BuilderRole role;
        public int enemyWorld=-1,coins=1,format=2;public bool heavy,boss,retreat;public GameObject source;
        public GameObject Model=>source!=null ? source : string.IsNullOrEmpty(prefab) ? null : Resources.Load<GameObject>(prefab);
    }
    public static class BuilderPalette
    {
        static List<BuilderAsset> entries;
        public static void Refresh(){entries=null;}
        public static IReadOnlyList<BuilderAsset> Entries=>entries??(entries=Create());
        public static BuilderAsset Get(string id)=>string.IsNullOrEmpty(id) ? null : Entries.FirstOrDefault(a=>a.id==id);
        public static bool IsDecoration(LevelBlock tile)=>Get(tile.asset)?.role==BuilderRole.Decoration;
        public static string[] Categories=>new[]{"Boden","Aufzüge","Gegner","Fallen","Beute","Natur","Dekoration","Spielziele"};
        public static BuilderAsset Basic(LevelTile kind)=>Entries.First(a=>a.id=="base."+(int)kind);
        static List<BuilderAsset> Create()
        {
            var result=new List<BuilderAsset>();string[] labels={"Boden","Münze","Gegner","Schütze","Springer","Stürmer","Bruchboden","Aufzug","Checkpoint","Zielportal","Flammenwerfer","Kanone","Fallstein","Sprungfeder"};
            for(int i=0;i<14;i++)result.Add(new BuilderAsset{id="base."+i,title=labels[i],category=i==0 || i==6 ? "Boden" : i==7 ? "Aufzüge" : i>=10 ? "Fallen" : i==8 || i==9 ? "Spielziele" : i==1 ? "Beute" : "Gegner",kind=(LevelTile)i});
            foreach(var model in Resources.LoadAll<GameObject>("OpenArt/Prefabs"))
            {
                string n=model.name;var item=new BuilderAsset{id="art."+n,title=Title(n),category="Dekoration",kind=LevelTile.Coin,role=BuilderRole.Decoration,prefab="OpenArt/Prefabs/"+n};
                if(n.StartsWith("floor_") && !n.Contains("spikes")){item.category="Boden";item.kind=LevelTile.Ground;item.role=BuilderRole.Block;}
                if(n.StartsWith("Platformer_"))
                {
                    string part=n.Substring(11);item.title=Title(part);
                    if(part.StartsWith("block-") || part=="brick" || part.StartsWith("platform")){item.category="Boden";item.kind=LevelTile.Ground;item.role=BuilderRole.Block;}
                    if(part=="block-moving"){item.category="Aufzüge";item.kind=LevelTile.Lift;}
                    if(part.StartsWith("coin-")){item.category="Beute";item.role=BuilderRole.Coin;}
                    if(part=="trap-spikes" || part=="spike-block"){item.category="Fallen";item.kind=LevelTile.Flame;item.role=BuilderRole.Spikes;}
                    if(part=="spring"){item.category="Fallen";item.kind=LevelTile.Spring;item.role=BuilderRole.Default;}
                }
                if(n=="coin" || n.StartsWith("coin_stack")){item.category="Beute";item.role=BuilderRole.Coin;item.coins=n.Contains("large") ? 10 : n.Contains("medium") ? 5 : n.Contains("small") ? 3 : 1;}
                if(n=="Skeleton_Mage" || n=="Skeleton_Minion" || n=="Skeleton_Rogue" || n=="Skeleton_Warrior")
                {item.category="Gegner";item.role=BuilderRole.Enemy;item.kind=n=="Skeleton_Mage" ? LevelTile.Shooter : LevelTile.Enemy;}
                if(n=="floor_tile_big_spikes"){item.category="Fallen";item.role=BuilderRole.Spikes;item.kind=LevelTile.Flame;item.title="Ausfahrbare Bodenstacheln";}
                if(n.Contains("saw") || n.Contains("Saw")){item.category="Fallen";item.role=BuilderRole.Saw;item.kind=LevelTile.Flame;item.title="Kreissäge";}
                if(n=="Skeleton_Blade" || n=="Skeleton_Crossbow" || n=="Skeleton_Staff")
                {item.category="Beute";item.role=BuilderRole.Weapon;item.weapon=n=="Skeleton_Blade" ? "sword" : n=="Skeleton_Crossbow" ? "bow" : "staff";}
                if(n.StartsWith("tree_") || n.StartsWith("plant_") || n.StartsWith("rock_") || n.Contains("mushroom") || n.Contains("palm"))item.category="Natur";
                result.Add(item);
            }
            result.Add(new BuilderAsset{id="pickup.heart",title="Heilherz · +1 Herz",category="Beute",kind=LevelTile.Coin,role=BuilderRole.Health,prefab="OpenArt/Prefabs/Platformer_heart",format=3});
            foreach(var item in AssetLibrary.Data.entries)
                result.Add(new BuilderAsset{id=item.id,title=item.title,category=item.category,prefab=item.resource,weapon=item.weapon,kind=(LevelTile)item.kind,role=(BuilderRole)item.role,format=4});
            result.Add(new BuilderAsset{id="pickup.ward",title="Schutzschild · ein Treffer",category="Beute",kind=LevelTile.Coin,role=BuilderRole.Shield,prefab="OpenArt/Prefabs/sword_shield",format=3});
            result.Add(new BuilderAsset{id="enemy.skirmisher.cave",title="Kavernenspäher · weicht zurück",category="Gegner",kind=LevelTile.Shooter,role=BuilderRole.Enemy,prefab="OpenArt/Prefabs/Skeleton_Rogue",enemyWorld=4,retreat=true,format=3});
            result.Add(new BuilderAsset{id="enemy.skirmisher.castle",title="Zinnenschütze · weicht zurück",category="Gegner",kind=LevelTile.Shooter,role=BuilderRole.Enemy,prefab="OpenArt/Prefabs/Skeleton_Mage",enemyWorld=6,retreat=true,format=3});
            foreach(var weapon in GameConfig.Current.weapons)result.Add(new BuilderAsset{id="weapon."+weapon.id,title="Waffenfund: "+weapon.title,category="Beute",kind=LevelTile.Coin,role=BuilderRole.Weapon,weapon=weapon.id});
            if(GameConfig.Current.grass!=null)result.Add(new BuilderAsset{id="nature.legacy.grass",title="Grasbüschel",category="Natur",kind=LevelTile.Coin,role=BuilderRole.Decoration,source=GameConfig.Current.grass});
            for(int w=0;w<8;w++)for(int h=0;h<2;h++)result.Add(new BuilderAsset{id="enemy.world."+w+"."+h,title=WorldEnemyVisual.Name(w,h==1)+" · "+GameConfig.WorldNames[w],category="Gegner",kind=LevelTile.Enemy,role=BuilderRole.Enemy,enemyWorld=w,heavy=h==1});
            result.Add(new BuilderAsset{id="enemy.goblin",title="Goblin (Originalmodell)",category="Gegner",kind=LevelTile.Enemy,role=BuilderRole.Enemy,source=GameConfig.Current.goblinVisual});
            result.Add(new BuilderAsset{id="enemy.cactus",title="Kaktuswächter (Originalmodell)",category="Gegner",kind=LevelTile.Enemy,role=BuilderRole.Enemy,source=GameConfig.Current.monsterVisual,heavy=true});
            for(int w=0;w<8;w++)result.Add(new BuilderAsset{id="enemy.boss."+w,title="Weltwächter · "+GameConfig.WorldNames[w],category="Gegner",kind=LevelTile.Enemy,role=BuilderRole.Enemy,enemyWorld=w,heavy=true,boss=true});
            foreach(var model in Resources.LoadAll<GameObject>("KenneyNature"))result.Add(new BuilderAsset{id="nature.old."+model.name,title=Title(model.name),category="Natur",kind=LevelTile.Coin,role=BuilderRole.Decoration,source=model});
            foreach(var model in GameConfig.Current.trees??Array.Empty<GameObject>())if(model!=null)result.Add(new BuilderAsset{id="nature.legacy."+model.name,title=model.name,category="Natur",kind=LevelTile.Coin,role=BuilderRole.Decoration,source=model});
            if(GameConfig.Current.rock!=null)result.Add(new BuilderAsset{id="nature.legacy.rock",title="Fels (bisheriges Naturpaket)",category="Natur",kind=LevelTile.Coin,role=BuilderRole.Decoration,source=GameConfig.Current.rock});
            return result.OrderBy(a=>Array.IndexOf(Categories,a.category)).ThenBy(a=>a.id,StringComparer.Ordinal).ToList();
        }
        static string Title(string n)
        {
            var names=new Dictionary<string,string>{{"floor_tile_grate","Steingitterboden"},{"block-grass","Grasblock"},{"block-snow","Schneeblock"},{"block-moving","Beweglicher Block"},{"brick","Ziegelblock"},{"platform","Holzplattform"},{"platform-fortified","Verstärkte Plattform"},{"coin-gold","Goldmünze"},{"coin-silver","Silbermünze"},{"coin-bronze","Bronzemünze"},{"trap-spikes","Bodenstacheln"},{"spike-block","Stachelblock"},{"spring","Sprungfeder"},{"pipe","Rohr (Deko)"},{"bomb","Bombe (Deko)"},{"crate","Holzkiste (Deko)"},{"crate-item","Belohnungskiste (Deko)"},{"barrel","Fass (Deko)"},{"lever","Hebel (Deko)"},{"heart","Herz (Deko)"},{"chest","Truhe (Deko)"},{"coin","Goldmünze"},{"floor_dirt_small_A","Erdblock"},{"floor_tile_small","Steinblock"},{"floor_tile_small_broken_A","Gebrochener Steinblock"},{"floor_tile_small_decorated","Verzierter Steinblock"},{"floor_wood_small","Holzblock"},{"floor_wood_small_dark","Dunkler Holzblock"},{"floor_foundation_allsides","Mauerblock"},{"coin_stack_small","Münzstapel · 3"},{"coin_stack_medium","Münzstapel · 5"},{"coin_stack_large","Münzhaufen · 10"},{"Skeleton_Mage","Skelettmagier"},{"Skeleton_Minion","Skelettdiener"},{"Skeleton_Rogue","Skelettspäher"},{"Skeleton_Warrior","Skelettkrieger"},{"Skeleton_Blade","Waffenfund: Schwert"},{"Skeleton_Crossbow","Waffenfund: Bogen"},{"Skeleton_Staff","Waffenfund: Runenstab"},{"key","Goldschlüssel (Deko)"},{"chest_gold","Schatztruhe (Deko)"},{"wall","Burgwand (Hintergrund)"},{"wall_arched","Torbogen (Hintergrund)"},{"wall_cracked","Rissige Wand (Hintergrund)"},{"stairs","Steintreppe (Deko)"},{"barrier","Absperrung (Deko)"}};
            return names.TryGetValue(n,out var title) ? title : n.Replace('_',' ');
        }
    }
}
