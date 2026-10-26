using System.Collections.Generic;
using UnityEngine;
namespace TreasureRunner
{
    public class WorldAreaCue:MonoBehaviour
    {
        public List<RouteSection> route;int previous=-1;float next;
        void Update()
        {
            var run=LevelRun.Instance;if(run==null || !run.IsPlaying || run.Player==null || run.HUD==null || Time.time<next)return;
            next=Time.time+.3f;float x=run.Player.transform.position.x;
            for(int i=route.Count-1;i>=0;i--)if(x>=route[i].x)
            {
                int feature=(int)route[i].feature;if(previous!=feature){run.HUD.Toast(WorldRoute.Title(route[i].feature));previous=feature;}break;
            }
        }
    }
}
