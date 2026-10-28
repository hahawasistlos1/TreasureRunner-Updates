using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public static class LibraryWorldArt
    {
        // Exact authored choices keep each biome distinct.
        static readonly string[][] landmarks={
            new[]{"kenney-nature/tree oak","kenney-nature/tree detailed","kenney-nature/tree tall","kenney-nature/tree oak fall"},
            new[]{"kenney-nature/tree pineTallA","kenney-nature/tree pineTallC","kenney-nature/tree pineRoundD","kenney-nature/stone tallA"},
            new[]{"kenney-pirate/ship pirate medium","kenney-pirate/palm detailed bend","kenney-pirate/ship wreck","kenney-pirate/tower complete small"},
            new[]{"kenney-cave/gate rock","kenney-cave/room small variation","kay-dungeon-v11/wall arched","kay-dungeon-v11/column"},
            new[]{"kenney-cave/corridor wide end","kenney-cave/gate metal bars","kay-dungeon-v11/wall archedwindow gated scaffold","kenney-cave/room wide variation"},
            new[]{"kenney-nature/mushroom redTall","kenney-nature/mushroom tanGroup","kenney-nature/stump oldTall","kenney-nature/mushroom redGroup"},
            new[]{"kenney-castle/tower square","kenney-castle/tower square top roof high","kenney-castle/wall doorway","kenney-castle/siege tower"},
            new[]{"kenney-castle/tower square arch","kay-dungeon-v11/pillar decorated","kay-dungeon-v11/column","kenney-castle/tower hexagon top"}};
        static readonly string[][] details={
            new[]{"kenney-nature/plant bushDetailed","kenney-nature/log stack","kenney-nature/mushroom tan","kenney-nature/stump round"},
            new[]{"kenney-nature/stone largeA","kenney-nature/stone smallFlatA","kenney-nature/tree pineSmallB","kenney-nature/stone tallC"},
            new[]{"kenney-pirate/palm straight","kenney-pirate/rocks sand a","kenney-pirate/boat row small","kenney-pirate/barrel"},
            new[]{"kay-dungeon-v11/rubble large","kenney-nature/stone tallA","kay-dungeon-v11/barrel large decorated","kay-dungeon-v11/pillar"},
            new[]{"kay-dungeon-v11/barrel small stack","kay-dungeon-v11/crates stacked","kay-dungeon-v11/rubble half","kay-dungeon-v11/pillar decorated"},
            new[]{"kenney-nature/mushroom tanTall","kenney-nature/stump old","kenney-nature/mushroom red","kenney-nature/plant bushTriangle"},
            new[]{"kay-dungeon-v11/banner red","kay-dungeon-v11/crates stacked","kay-dungeon-v11/barrel large","kenney-castle/wall narrow gate"},
            new[]{"kay-dungeon-v11/rubble half","kay-dungeon-v11/pillar","kenney-nature/stone tallD","kay-dungeon-v11/chest gold"}};
        static readonly string[] floors={"kenney-platformer/block grass","kenney-platformer/block snow","kenney-pirate/patch sand","kay-dungeon-v11/floor tile big grate","kay-dungeon-v11/floor tile small","kay-dungeon-v11/floor wood small dark","kay-dungeon-v11/floor foundation allsides","kay-dungeon-v11/floor tile large"};
        static readonly Dictionary<string,LibraryEntry> cache=new Dictionary<string,LibraryEntry>();
        public static void Refresh(){cache.Clear();}
        public static LibraryEntry Resolve(string key)
        {
            if(cache.TryGetValue(key,out var found))return found;
            int split=key.IndexOf('/');string pack=key.Substring(0,split),name=key.Substring(split+1);
            found=AssetLibrary.Data.entries.FirstOrDefault(e=>e.pack==pack && e.title.Split('·')[0].Trim().Equals(name,StringComparison.OrdinalIgnoreCase));cache[key]=found;return found;
        }
        public static LibraryEntry Landmark(int world,int stage,int zone)=>Resolve(landmarks[Mathf.Clamp(world,0,7)][Math.Abs(stage+zone)%4]);
        public static LibraryEntry Detail(int world,int stage,int section)=>Resolve(details[Mathf.Clamp(world,0,7)][Math.Abs(stage*3+section)%4]);
        public static bool HasScenery(int world)=>Enumerable.Range(0,4).Any(i=>Landmark(world,0,i)!=null);
        public static GameObject Scenery(int world,int index)=>AssetLibrary.Model(Landmark(world,0,index));
        public static GameObject Floor(int world)=>AssetLibrary.Model(Resolve(floors[Mathf.Clamp(world,0,7)]));
        public static Color Tint(int world)=>new[]{Color.white,new Color(.64f,.88f,1),new Color(1,.94f,.8f),new Color(.6f,.32f,.24f),new Color(.68f,.76f,.95f),new Color(.74f,.62f,1),Color.white,new Color(.7f,.57f,1)}[Mathf.Clamp(world,0,7)];
        public static Color Sky(int world,int stage)=>Color.Lerp(ComicPalette.Sky[world],stage%3==1 ? new Color(1,.65f,.45f) : new Color(.28f,.38f,.65f),stage%3==0 ? 0 : .16f);
        public static void ApplyTint(Transform root,Color tint)
        {
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var material=materials[slot];Color baseColor=material!=null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material!=null && material.HasProperty("_Color") ? material.color : Color.white;
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot);block.SetColor("_BaseColor",baseColor*tint);renderer.SetPropertyBlock(block,slot);
                }
            }
        }
    }
}
