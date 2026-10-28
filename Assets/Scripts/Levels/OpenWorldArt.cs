using UnityEngine;
namespace TreasureRunner
{
    public class OpenWorldArt:MonoBehaviour
    {
        public static GameObject Asset(string name)=>Resources.Load<GameObject>("OpenArt/Prefabs/"+name);
        public void Section(RouteSection s,int index,int world,int stageOverride=-1)
        {
            int quality=ProgressStore.Data.graphicsQuality;if(index%(quality==0 ? 4 : 2)!=0)return;
            int stage=stageOverride>=0 ? stageOverride : LevelRun.IsCustom ? 0 : CampaignCatalog.Stage(LevelRun.SelectedLevel);
            Place(LibraryWorldArt.Landmark(world,stage,s.zone),s,index,world,stage,0);
            if(quality>0)Place(LibraryWorldArt.Detail(world,stage,index),s,index,world,stage,1);
            if(quality>1 && index%4==0)Place(LibraryWorldArt.Detail(world,stage+1,index+1),s,index,world,stage,2);
        }
        void Place(LibraryEntry entry,RouteSection s,int index,int world,int stage,int slot)
        {
            var source=AssetLibrary.Model(entry);if(source==null)return;
            float height=slot==0 ? new[]{5.5f,4.8f,4.5f,4.3f,4.4f,4.8f,5.8f,4.2f}[world] : world==5 ? 1.9f : 1.25f;
            var holder=new GameObject("Kulisse · "+entry.title);holder.transform.SetParent(transform,false);
            var prop=world==6 && slot==0 && entry.title.StartsWith("tower") ? Tower(holder.transform,height) : LevelFactory.AddVisual(source,holder.transform,height,0);
            holder.transform.position=new Vector3(s.Center+(slot==0 ? -s.width*.2f : s.width*.28f),s.height+(world==7 && slot==0 ? .65f : 0),slot==0 ? 7.5f : 4.2f);
            prop.localRotation=Quaternion.Euler(world==7 ? -7 : 0,world==2 && entry.title.StartsWith("ship") ? 75 : (index*37+stage*61+slot*83)%160-80,world==7 ? (index%2==0 ? 9 : -9) : 0);
            var bounds=Bounds(prop);float maximumWidth=Mathf.Max(3,s.width*1.15f);
            if(bounds.size.x>maximumWidth){prop.localScale*=maximumWidth/bounds.size.x;bounds=Bounds(prop);}
            // Keep all imported scenery behind the playable lane, with no physical bodies.
            holder.transform.position+=Vector3.forward*Mathf.Max(0,2.3f-bounds.min.z);
            if(slot==0 && world!=7)
            {
                var ground=AssetBlockSurface.Fit(LibraryWorldArt.Floor(world),holder.transform,Vector3.zero,new Vector3(s.width*1.2f,.1f,10.2f));
                if(ground!=null){ground.name="Hintergrundboden ohne Kollision";ground.position=new Vector3(s.Center,s.height-.07f,7.5f);}
            }
            LibraryWorldArt.ApplyTint(holder.transform,LibraryWorldArt.Tint(world));
            var marker=holder.AddComponent<CampaignSceneryMarker>();marker.assetId=entry.id;marker.world=world;marker.stage=stage;marker.section=index;
        }
        static Bounds Bounds(Transform visual)
        {var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=AssetBlockSurface.GeometryBounds(renderers[0]);for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(AssetBlockSurface.GeometryBounds(renderers[i]));return bounds;}
        static Transform Tower(Transform parent,float height)
        {
            var root=new GameObject("Zusammengesetzter Asset-Turm").transform;root.SetParent(parent,false);float feet=0;
            foreach(string key in new[]{"kenney-castle/tower square base","kenney-castle/tower square mid windows","kenney-castle/tower square top roof high"})
            {
                var model=AssetLibrary.Model(LibraryWorldArt.Resolve(key));if(model==null)continue;var part=Instantiate(model,root);var bounds=Bounds(part.transform);
                part.transform.position+=new Vector3(-bounds.center.x,feet-bounds.min.y,-bounds.center.z);feet+=bounds.size.y;
            }
            root.localScale*=height/Mathf.Max(.01f,feet);return root;
        }
    }
}
