using UnityEngine;
namespace TreasureRunner
{
    // IDs 0..23 retain their saved meaning. New chapters are appended, never inserted.
    public static class CampaignCatalog
    {
        public const int StagesPerWorld=10, Count=80;
        public static int World(int id)=>id<24 ? Mathf.Clamp(id/3,0,7) : Mathf.Clamp((id-24)/7,0,7);
        public static int Stage(int id)=>id<24 ? id%3 : (id-24)%7+3;
        public static int Id(int world,int stage)=>stage<3 ? world*3+stage : 24+world*7+stage-3;
        public static int Next(int id)=>Stage(id)<2 ? id+1 : Stage(id)==2 ? (World(id)<7 ? id+1 : Id(0,3)) : Stage(id)<9 ? Id(World(id),Stage(id)+1) : -1;
        public static bool Unlocked(int id)
        {
            if(id<0 || id>=Count)return false;
            if(id<24)return id<=ProgressStore.Data.highestUnlockedLevel;
            int previous=Id(World(id),Stage(id)-1);
            return (previous<24 && previous<ProgressStore.Data.highestUnlockedLevel) || ProgressStore.Data.FindLevel(previous)?.completions>0;
        }
        static readonly string[][] Names={
            new[]{"Sonnenpfad","Kronenaufstieg","Wurzelfestung","Mühlenbach","Versteckte Lichtung","Laubtunnel","Brückenbruch","Wipfelreise","Räuberpass","Waldzitadelle"},
            new[]{"Schneetal","Gletschertreppe","Frostwache","Eisbrücken","Spiegelhöhle","Flockentunnel","Splittersee","Polaraufzug","Frostkanonen","Kristallpalast"},
            new[]{"Palmenstraße","Dünenturm","Oasenwächter","Sandbrücken","Vergessener Schatz","Grabkammern","Versinkender Pfad","Himmelsruinen","Skorpionwall","Sonnenpyramide"},
            new[]{"Glutweg","Basaltaufstieg","Schmiedewache","Lavabrücken","Verborgene Esse","Feuertunnel","Brüchige Schlacke","Vulkanlift","Kanonenkrater","Lavazitadelle"},
            new[]{"Kristallweg","Minenschacht","Höhlenwächter","Grubenbrücken","Geheimer Stollen","Felslabyrinth","Einsturzmine","Förderaufzug","Spinnenbastion","Kristallherz"},
            new[]{"Pilzwiese","Hohlbaumtreppe","Dornenwächter","Nebelbrücken","Feengarten","Wurzeltunnel","Morscher Wald","Sporenaufstieg","Schattenwall","Nachtschloss"},
            new[]{"Burghof","Turmtreppe","Torwächter","Zugbrücken","Königsschatz","Kerkerweg","Zerfallene Zinnen","Burgaufzug","Kanonenbastion","Thronsaal"},
            new[]{"Sternenweg","Runenspirale","Arkanwächter","Wolkenbrücken","Sternenschatz","Zaubertunnel","Splitterinseln","Runenaufzug","Wispfestung","Himmelspalast"}
        };
        public static string Name(int id)=>Names[World(id)][Stage(id)];
        public static string Style(int stage)=>new[]{"Entdecken","Aufstieg","Festung","Brücken","Geheimwege","Tunnel","Zerfall","Aufzüge","Belagerung","Finale"}[stage];
    }
}
