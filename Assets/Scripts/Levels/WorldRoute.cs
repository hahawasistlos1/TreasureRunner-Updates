using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public enum RouteFeature { Clearing, Canopy, RootBridge, IceSteps, FrozenLake, Glacier, Oasis, Ruins, Canyon, Forge, Basalt, LavaCrossing, Tunnel, CrystalHall, Mine, MushroomGrove, HollowTree, ThornPass, Courtyard, Battlements, Tower, Drift, Spiral, CrystalGarden, Arena }
    public enum EncounterRoom { Run, Steps, Bridges, SplitPath, Tunnel, Collapse, Lift, Cannons, SwitchGate, Rest }
    public struct RouteSection
    {
        public float x,width,height,gap;public int pattern,zone;public RouteFeature feature;public EncounterRoom room;
        public float Center=>x+width*.5f;
    }
    public static class WorldRoute
    {
        // Each world has authored encounter orders, rather than a shared six-step rhythm.
        private static readonly RouteFeature[][] Sequences={
            new[]{RouteFeature.Clearing,RouteFeature.RootBridge,RouteFeature.Canopy,RouteFeature.Clearing,RouteFeature.Canopy,RouteFeature.RootBridge},
            new[]{RouteFeature.IceSteps,RouteFeature.FrozenLake,RouteFeature.Glacier,RouteFeature.IceSteps,RouteFeature.Glacier,RouteFeature.FrozenLake},
            new[]{RouteFeature.Oasis,RouteFeature.Ruins,RouteFeature.Canyon,RouteFeature.Ruins,RouteFeature.Oasis,RouteFeature.Canyon},
            new[]{RouteFeature.Forge,RouteFeature.Basalt,RouteFeature.LavaCrossing,RouteFeature.Basalt,RouteFeature.Forge,RouteFeature.LavaCrossing},
            new[]{RouteFeature.Tunnel,RouteFeature.Mine,RouteFeature.CrystalHall,RouteFeature.Tunnel,RouteFeature.CrystalHall,RouteFeature.Mine},
            new[]{RouteFeature.MushroomGrove,RouteFeature.HollowTree,RouteFeature.ThornPass,RouteFeature.HollowTree,RouteFeature.MushroomGrove,RouteFeature.ThornPass},
            new[]{RouteFeature.Courtyard,RouteFeature.Tower,RouteFeature.Battlements,RouteFeature.Courtyard,RouteFeature.Battlements,RouteFeature.Tower},
            new[]{RouteFeature.CrystalGarden,RouteFeature.Spiral,RouteFeature.Drift,RouteFeature.Spiral,RouteFeature.CrystalGarden,RouteFeature.Drift}
        };
        private static readonly int[][] Orders={new[]{0,1,2,3,4,5},new[]{2,0,4,1,5,3},new[]{1,4,0,5,2,3}};
        public static string Title(RouteFeature f)
        {
            string[] names={"Sonnenlichtung","Baumkronenpfad","Wurzelbrücken","Eistreppe","Gefrorener See","Gletscherspalten","Oase","Versunkene Ruinen","Schlucht","Glühende Schmiede","Basaltsäulen","Lavaübergang","Felstunnel","Kristallhalle","Alte Mine","Pilzgarten","Hohle Baumriesen","Dornenpass","Burghof","Zinnenweg","Turmaufstieg","Schwebende Inseln","Sternenspirale","Kristallgarten","Portalplatz"};
            return names[(int)f];
        }
        public static List<RouteSection> Create(int world,int stage,int count)
        {
            return PatchRuntime.Route(AuthoredRoute(world,stage,count),Mathf.Clamp(world,0,7),Mathf.Clamp(stage,0,9));
        }
        public static List<RouteSection> CreateBase(int world,int stage,int count)=>AuthoredRoute(world,stage,count);
        // Whole rooms have distinct rules: flat running, staircases, split paths, ceilings,
        // bridge timing, lifts and gates. World decorations are independent of that topology.
        private static readonly int[][] Rooms={
            new[]{0,3,1,9,2,4,3,8,0}, new[]{1,3,1,6,9,1,4,6,8},
            new[]{0,7,8,4,9,3,7,1,8}, new[]{2,0,2,5,9,6,2,3,2},
            new[]{3,4,3,1,9,8,3,4,3}, new[]{4,8,0,4,9,5,4,7,4},
            new[]{0,5,2,5,9,1,5,6,5}, new[]{1,6,3,6,9,2,6,8,6},
            new[]{7,0,8,7,9,4,7,3,8}, new[]{1,7,8,6,9,5,4,7,8}
        };
        private static readonly int[][] WorldRooms={
            new[]{0,3,1,9,2,4,3,8,0},
            new[]{1,5,6,9,2,1,4,6,8},
            new[]{0,7,8,9,4,3,7,1,8},
            new[]{2,7,5,9,6,2,4,1,5},
            new[]{4,3,6,9,8,4,3,1,0},
            new[]{4,8,5,9,0,3,7,4,1},
            new[]{0,8,2,9,1,5,6,7,4},
            new[]{6,1,3,9,2,6,8,5,1}
        };
        public static EncounterRoom RoomFor(int world,int stage,int zone)=>
            (EncounterRoom)WorldRooms[Mathf.Clamp(world,0,7)][(zone*(stage%3==0 ? 1 : stage%3==1 ? 2 : 4)+stage/3)%9];
        private static readonly float[] IntroWidths={14,10,18,7,9,12,16,6.5f};
        private static readonly EncounterRoom[] IntroRooms={EncounterRoom.Rest,EncounterRoom.Steps,EncounterRoom.Run,EncounterRoom.Bridges,EncounterRoom.Tunnel,EncounterRoom.SplitPath,EncounterRoom.Run,EncounterRoom.Lift};
        public static int SectionCount(int world,int stage)=>44+stage*3+(world*2+stage)%5;
        static List<RouteSection> AuthoredRoute(int world,int stage,int count)
        {
            stage=Mathf.Clamp(stage,0,9);world=Mathf.Clamp(world,0,7);
            var route=new List<RouteSection>();float x=0,y=0;
            
            // Each world has a different large-scale elevation profile; stages alter the
            // cadence of islands, long encounter spaces, rests and climbing sequences.
            for(int i=0;i<count;i++)
            {
                float progress=i/(float)(count-1);int zone=Mathf.Min(8,i*9/count);
                int local=i-(zone*count+8)/9;
                var room=RoomFor(world,stage,zone);
                float target=0;
                switch(world)
                {
                    case 0:target=progress<.28f ? 0 : progress<.52f ? 3 : progress<.7f ? 1 : 4;break;
                    case 1:target=zone%3==0 ? 0 : zone%3==1 ? -2 : 1;break;
                    case 2:target=progress<.2f ? 1 : progress<.55f ? -4 : 2;break;
                    case 3:target=zone%2==0 ? 3 : 0;break;
                    case 4:target=progress<.35f ? -2 : progress<.7f ? 1 : -1;break;
                    case 5:target=zone%3==0 ? 2 : -1;break;
                    case 6:target=progress<.75f ? progress*9 : 6;break;
                    case 7:target=2+Mathf.Sin(progress*Mathf.PI*(2+stage%3))*3;break;
                }
                target+=stage%3==1 && zone%2==0 ? .8f : 0;
                y=Mathf.MoveTowards(y,target,1f);
                bool islands=room==EncounterRoom.Bridges || room==EncounterRoom.Collapse || room==EncounterRoom.Steps || world==7;
                float width=islands ? 3.5f+(local+stage)%3*1.1f : 10+(local*3+world+stage)%7;
                if(room==EncounterRoom.Rest)width=11;
                if(room==EncounterRoom.Tunnel)width=12+(stage+local)%3*2;
                // No repeated triangular stepping stones laid over an unbroken floor.
                if(i==0){y=0;width=IntroWidths[world];room=IntroRooms[world];}
                var feature=Sequences[world][(zone+stage)%6];
                if(i==count-1){width=20;feature=RouteFeature.Arena;room=EncounterRoom.Rest;}
                route.Add(new RouteSection{x=x,width=width,height=y,pattern=local,zone=zone,feature=feature,room=room});
                x+=width;
            }
            // Gaps are authored at the edges of the actual main path, then limited by
            // the exact unupgraded jump arc. Breathing spaces can remain contiguous.
            x=0;
            for(int i=0;i<route.Count;i++)
            {
                var section=route[i];section.x=x;
                if(i<route.Count-1)
                {
                    float rise=route[i+1].height-section.height;
                    bool continuous=section.room==EncounterRoom.Tunnel && route[i+1].room==EncounterRoom.Tunnel && Mathf.Abs(rise)<.01f;
                    float desired=2.7f+(stage+world+i)%4*.2f;
                    section.gap=continuous ? 0 : Mathf.Min(desired,JumpProfile.Speed*JumpProfile.FlightTime(rise)-.8f);
                    // A clearly visible initial practice jump; upgrades never required.
                    if(i==0)section.gap=2.5f;
                }
                route[i]=section;x+=section.width+section.gap;
            }
            return route;
        }
        private static List<RouteSection> LegacyRoute(int world,int stage,int count)
        {
            var result=new List<RouteSection>();var rng=new System.Random(31871+world*1913+stage*7919);
            float x=0,y=0;int zone=-1,remaining=0;
            RouteFeature kind=Sequences[world][Orders[stage][0]];
            for(int i=0;i<count;i++)
            {
                if(remaining--<=0){zone++;kind=Sequences[world][Orders[stage][zone%6]];remaining=2+rng.Next(3);}
                float width=Width(kind)+(float)rng.NextDouble()*1.4f;
                float target=Elevation(kind,zone,stage);
                if(kind==RouteFeature.IceSteps || kind==RouteFeature.Tower || kind==RouteFeature.Spiral) target+=((i+zone)%4)*.8f;
                if(kind==RouteFeature.Glacier || kind==RouteFeature.Basalt)target+=(i%2==0 ? .5f : -.5f);
                y=Mathf.MoveTowards(y,target,1.15f);
                if(i==0){y=0;width=10;}
                if(i==count-1){kind=RouteFeature.Arena;width=14;}
                float gap=Gap(kind)+(float)rng.NextDouble()*.25f;
                if(i==0)gap=1.1f;
                result.Add(new RouteSection{x=x,width=width,height=y,gap=gap,pattern=i%6,zone=zone,feature=kind});x+=width+gap;
            }
            for(int i=1;i<result.Count;i++)
            {
                var prev=result[i-1];var current=result[i];
                if(current.height-prev.height>.55f)
                {
                    float reduce=Mathf.Max(0,prev.gap-1.15f);
                    for(int j=i;j<result.Count;j++){var next=result[j];next.x-=reduce;result[j]=next;}
                    prev.gap-=reduce;result[i-1]=prev;
                }
            }
            return result;
        }
        private static float Width(RouteFeature f)
        {
            switch(f){case RouteFeature.Clearing:case RouteFeature.Oasis:case RouteFeature.Courtyard:case RouteFeature.Forge:return 13;
            case RouteFeature.Canopy:case RouteFeature.Glacier:case RouteFeature.Basalt:case RouteFeature.Spiral:return 4.5f;
            case RouteFeature.RootBridge:case RouteFeature.LavaCrossing:case RouteFeature.ThornPass:return 5.8f;
            case RouteFeature.IceSteps:case RouteFeature.Tower:return 5.2f;
            case RouteFeature.FrozenLake:return 14;
            case RouteFeature.Canyon:case RouteFeature.Drift:return 6;
            case RouteFeature.CrystalHall:case RouteFeature.CrystalGarden:return 10;
            default:return 8.5f;}
        }
        private static float Gap(RouteFeature f)
        {
            switch(f){case RouteFeature.Canopy:case RouteFeature.Drift:return 2.7f;
            case RouteFeature.Glacier:case RouteFeature.LavaCrossing:return 2.4f;
            case RouteFeature.Canyon:case RouteFeature.Basalt:return 2;
            default:return 1.1f;}
        }
        private static float Elevation(RouteFeature f,int zone,int stage)
        {
            switch(f){case RouteFeature.Canopy:return 5;
            case RouteFeature.Glacier:return 3.4f;
            case RouteFeature.IceSteps:return 1;
            case RouteFeature.FrozenLake:return -.8f;
            case RouteFeature.Canyon:return -1.5f;
            case RouteFeature.Ruins:return 2;
            case RouteFeature.Basalt:return 2.5f;
            case RouteFeature.LavaCrossing:return .4f;
            case RouteFeature.Tunnel:case RouteFeature.Mine:return -1.5f;
            case RouteFeature.CrystalHall:return 2;
            case RouteFeature.HollowTree:return 3.5f;
            case RouteFeature.ThornPass:return 1.2f;
            case RouteFeature.Battlements:return 5.5f;
            case RouteFeature.Tower:return 3;
            case RouteFeature.Spiral:return 3.5f+stage*.4f;
            case RouteFeature.Drift:return 1.8f;
            default:return 0;}
        }
    }
}
