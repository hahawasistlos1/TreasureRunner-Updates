using UnityEngine;
namespace TreasureRunner
{
    public class SecretRoom:MonoBehaviour
    {
        public bool HasKey {get;private set;}
        public bool Claimed {get;private set;}
        GameObject gate;Transform key;Transform returnPoint;int levelId;Vector3 interiorSpawn;
        public static SecretRoom Build(Transform parent,RouteSection section,int world,int levelId)
        {
            var root=new GameObject("Geheimraum mit Schlüssel");root.transform.SetParent(parent);var room=root.AddComponent<SecretRoom>();room.levelId=levelId;
            Vector3 entry=new Vector3(section.x+Mathf.Min(3,section.width*.3f),section.height+1.05f,0);
            room.returnPoint=new GameObject("Rückkehr zum Hauptweg").transform;room.returnPoint.SetParent(root.transform);room.returnPoint.position=entry;
            room.key=Prop(root.transform,"key",entry+Vector3.right*2+Vector3.up*.7f,.65f);
            room.key.gameObject.AddComponent<SecretDoorTrigger>().Setup(room,0);
            // The door is optional. The interior is outside the route's camera range, so trees cannot obscure it.
            var door=Cube(root.transform,"Schlüsseltür",entry+Vector3.forward*1.1f,new Vector3(1.1f,2, .3f),GameUI.Gold,false);
            var entryZone=new GameObject("Schatzkammer betreten");entryZone.transform.SetParent(root.transform);entryZone.transform.position=entry;entryZone.transform.localScale=new Vector3(1,2,1);entryZone.AddComponent<SecretDoorTrigger>().Setup(room,1);
            Vector3 inside=new Vector3(-200-section.x*2,section.height+1.05f,0);
            room.interiorSpawn=inside;
            Cube(root.transform,"Schatzkammerboden",inside-Vector3.up*1.4f,new Vector3(20,.7f,4),ComicPalette.Ground[world],true);
            Cube(root.transform,"Schatzkammerwand",inside+new Vector3(0,2,2),new Vector3(20,7,.4f),ComicPalette.Earth[world],true);
            Cube(root.transform,"Schatzkammer links",inside+new Vector3(-10,2,0),new Vector3(.5f,7,4),ComicPalette.Earth[world],true);
            Cube(root.transform,"Schatzkammer rechts",inside+new Vector3(10,2,0),new Vector3(.5f,7,4),ComicPalette.Earth[world],true);
            Prop(root.transform,"pillar_decorated",inside+new Vector3(-6,-1,1.3f),3);
            Prop(root.transform,"pillar_decorated",inside+new Vector3(6,-1,1.3f),3);
            Prop(root.transform,"torch_mounted",inside+new Vector3(0,1.4f,1.6f),.9f);
            var chest=Prop(root.transform,"chest_gold",inside+Vector3.right*2-Vector3.up,1.2f);chest.gameObject.AddComponent<SecretDoorTrigger>().Setup(room,2);
            var chestBody=Cube(root.transform,"Schatzkammer Truhenkörper",inside+new Vector3(2,-.4f,0),new Vector3(1.25f,1.2f,.8f),GameUI.Gold,true);chestBody.GetComponent<Renderer>().enabled=false;
            var exit=Cube(root.transform,"Rückweg",inside-Vector3.right*2, new Vector3(1.1f,2,.4f),new Color(.25f,.9f,.8f),false);exit.AddComponent<SecretDoorTrigger>().Setup(room,3);
            room.gate=door;room.transform.position=Vector3.zero;return room;
        }
        static Transform Prop(Transform parent,string name,Vector3 position,float height)
        {
            var source=OpenWorldArt.Asset(name);if(source!=null){var visual=LevelFactory.AddVisual(source,parent,height,0);visual.position+=position;return visual;}
            return Cube(parent,name,position,Vector3.one*.6f,GameUI.Gold,false).transform;
        }
        static GameObject Cube(Transform parent,string name,Vector3 position,Vector3 size,Color color,bool solid)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(parent);o.transform.position=position;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=ComicPalette.Material(name,color);if(!solid)o.GetComponent<Collider>().enabled=false;return o;
        }
        public void Interact(int kind,PlayerHealth player)
        {
            var run=LevelRun.Instance;if(run==null || !run.IsPlaying || run.Player!=player)return;
            if(kind==0){if(HasKey)return;HasKey=true;key.gameObject.SetActive(false);run.HUD?.Toast("Schlüssel gefunden! Die goldene Tür führt zur Schatzkammer.");GameEffects.Spark(player.transform.position);}
            else if(kind==1){if(!HasKey){run.HUD?.Toast("Für diese Schatzkammer brauchst du den goldenen Schlüssel.");return;}Teleport(player,interiorSpawn);run.HUD?.Toast("Geheimraum entdeckt! Türkises Tor: zurück zum Hauptweg.");}
            else if(kind==2){if(Claimed)return;Claimed=true;int reward=SpecialTreasure.Claim("secret."+levelId);if(reward>0)CoinManager.Instance?.AddCoins(reward);run.HUD?.Toast(reward>0 ? "Seltener Schatz! +"+reward+" Münzen und ein Sammlerabzeichen." : "Schatz bereits gesammelt – dein Abzeichen bleibt erhalten.");GameEffects.Spark(player.transform.position);}
            else if(kind==3)Teleport(player,returnPoint.position+Vector3.right*1.8f);
        }
        static void Teleport(PlayerHealth player,Vector3 point)
        {
            var movement=player.GetComponent<PlayerMovement>();movement.ClearInput();var body=player.GetComponent<Rigidbody>();body.position=point;body.linearVelocity=Vector3.zero;player.GrantInvulnerability(1);
            var camera=Camera.main;if(camera!=null){var follow=camera.GetComponent<CameraFollow>();if(follow!=null)camera.transform.position=point+follow.offset;}
        }
        void Update(){if(key!=null && key.gameObject.activeSelf)key.Rotate(0,80*Time.deltaTime,0);}
        void OnDestroy(){foreach(var renderer in GetComponentsInChildren<Renderer>())if(renderer.sharedMaterial!=null && (renderer.sharedMaterial.name.StartsWith("Schatzkammer") || renderer.sharedMaterial.name=="Rückweg" || renderer.sharedMaterial.name=="Schlüsseltür"))Destroy(renderer.sharedMaterial);}
    }
    public class SecretDoorTrigger:MonoBehaviour
    {
        SecretRoom room;int kind;float next;
        public int Action=>kind;
        public void Setup(SecretRoom target,int action)
        {
            room=target;kind=action;var box=gameObject.AddComponent<BoxCollider>();box.isTrigger=true;
            Vector3 scale=transform.lossyScale;box.size=new Vector3((action==2 ? 2 : 1)/Mathf.Max(.001f,Mathf.Abs(scale.x)),1.8f/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
            if(action==0 || action==2)box.center=transform.InverseTransformVector(Vector3.up*.5f);
        }
        void OnTriggerEnter(Collider other){if(Time.time<next)return;var player=other.GetComponentInParent<PlayerHealth>();if(player==null)return;next=Time.time+1;room.Interact(kind,player);}
    }
    public static class SpecialTreasure
    {
        public static int Claim(string id)
        {
            if(LevelRun.IsCustom || ProgressStore.Data.achievements.Contains("Schatz · "+id))return 0;
            ProgressStore.Data.achievements.Add("Schatz · "+id);ProgressStore.Save();return id.StartsWith("boss.") ? 75 : 30;
        }
    }
}
