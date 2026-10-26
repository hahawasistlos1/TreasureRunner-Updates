using UnityEngine;
namespace TreasureRunner
{
    public class OpenWorldArt:MonoBehaviour
    {
        public static GameObject Asset(string name)=>Resources.Load<GameObject>("OpenArt/Prefabs/"+name);
        static readonly string[][] Props={
            new[]{"tree_oak","plant_bushLarge","log_stack","tree_pineTallA"},
            new[]{"tree_pineTallA","rock_largeB","rock_tallA","tree_pineTallA"},
            new[]{"tree_palmTall","statue_obelisk","tree_palm","pillar_decorated"},
            new[]{"rock_tallA","pillar_decorated","rubble_large","campfire_stones"},
            new[]{"crates_stacked","pillar_decorated","barrel_large","rubble_large"},
            new[]{"mushroom_red","plant_bushLarge","mushroom_red","tree_oak"},
            new[]{"pillar_decorated","banner_red","banner_blue","crates_stacked"},
            new[]{"statue_obelisk","pillar_decorated","rock_tallA","mushroom_red"}};
        public void Section(RouteSection s,int index,int world)
        {
            if(index%2!=0)return;
            var source=Asset(Props[world][index/2%4]);if(source==null)return;
            // All props remain behind the playable lane and have no collision.
            var prop=LevelFactory.AddVisual(source,transform,(world==5 ? 2.1f : 2.7f)+(index%3)*.35f,0);
            prop.position+=new Vector3(s.Center,s.height,3.7f+index%3);prop.localRotation=Quaternion.Euler(0,25+index*47,0);
            prop.name="Kulisse · "+source.name;
            if(world==1 || world==3 || world==7)
            {
                var block=new MaterialPropertyBlock();
                block.SetColor("_BaseColor",world==1 ? new Color(.75f,.9f,1) : world==3 ? new Color(.65f,.29f,.16f) : new Color(.65f,.45f,1));
                foreach(var renderer in prop.GetComponentsInChildren<Renderer>())renderer.SetPropertyBlock(block);
            }
        }
    }
}
