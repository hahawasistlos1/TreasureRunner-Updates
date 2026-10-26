using UnityEngine;
using UnityEngine.Rendering;

namespace TreasureRunner
{
    public class LevelFactory : MonoBehaviour
    {
        private GameConfig config;
        private Material terrain;
        private Material stone;
        private Material gold;
        private Material accent;
        private Material bark;
        private int id;
        private int world;
        private int difficulty;
        private int segments;
        private System.Random random;
        private ComicWorldArt scenery;private WorldTerrainSurface surface;private OpenWorldArt openArt;private bool secretBuilt;
        public System.Collections.Generic.List<RouteSection> Route { get; private set; }
        public int SegmentCount => segments;
        public void Build(LevelRun run)
        {
            config = GameConfig.Current; id = LevelRun.SelectedLevel;
            world = LevelRun.IsCustom ? LevelRun.CustomLevel.world : CampaignCatalog.World(id); difficulty = LevelRun.IsCustom ? 0 : CampaignCatalog.Stage(id);
            random = new System.Random(731 + id);
            scenery=gameObject.AddComponent<ComicWorldArt>(); scenery.Initialize(world,id);surface=gameObject.AddComponent<WorldTerrainSurface>();surface.Initialize(world);
            openArt=gameObject.AddComponent<OpenWorldArt>();
            terrain = Material("Terrain", ComicPalette.Ground[world]);
            stone = Material("Stone",ComicPalette.Earth[world]);
            gold = Material("Gold",GameUI.Gold);
            accent = Material("Portal",new Color(.25f,.88f,.73f));
            bark = Material("Wood",new Color(.65f,.37f,.22f));
            RenderSettings.ambientLight = Color.Lerp(new Color(.5f,.63f,.7f),ComicPalette.Ground[world],.25f);
            RenderSettings.fog = true; RenderSettings.fogColor = ComicPalette.Sky[world];
            RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=28;RenderSettings.fogEndDistance=95;
            var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional;
            light.intensity = 1.35f; light.transform.rotation = Quaternion.Euler(45,-30,0);
            light.shadows = ProgressStore.Data.graphicsQuality == 0 ? LightShadows.None : LightShadows.Soft;
            light.shadowStrength = .65f;light.shadowBias=.06f;light.shadowNormalBias=.3f;
            Camera camera = new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            camera.tag = "MainCamera"; camera.fieldOfView = 42;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = ComicPalette.Sky[world];
            camera.farClipPlane = 110;
            run.Spawn = new Vector3(1.5f,1.2f,0);
            PlayerHealth player = BuildPlayer(run.Spawn);
            run.Player = player;
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.player = player.transform; follow.offset = new Vector3(2.1f,2.7f,-12);
            follow.smoothing = 7;
            camera.transform.position = player.transform.position + follow.offset;
            camera.transform.rotation = Quaternion.Euler(8,0,0);
            new GameObject("Coins").AddComponent<CoinManager>();
            new GameObject("Score").AddComponent<ScoreManager>();
            if(LevelRun.IsCustom){BuildCustom(run);var customActors=gameObject.AddComponent<ComicActorMaterials>();customActors.Apply(transform);customActors.Apply(player.transform);return;}
            segments = WorldRoute.SectionCount(world,difficulty);
            Route=WorldRoute.Create(world,difficulty,segments);
            for (int i = 0; i < segments; i++) Segment(i,run);
            var last=Route[segments-1];float end=last.x+last.width-2;
            if (difficulty == 2 || difficulty==9) SpawnEnemy(new Vector3(end - 5,last.height+1.1f,0),true,true,run);
            Portal(new Vector3(end,last.height+1.3f,0));
            var cues=gameObject.AddComponent<WorldAreaCue>();cues.route=Route;
            var actors=gameObject.AddComponent<ComicActorMaterials>();actors.Apply(transform);actors.Apply(player.transform);
        }
        private PlayerHealth BuildPlayer(Vector3 position)
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player"; player.tag = "Player"; player.SetActive(false);
            player.transform.position = position;
            player.GetComponent<Renderer>().enabled = false;
            player.AddComponent<Rigidbody>().mass = 1;
            var health = player.AddComponent<PlayerHealth>();
            health.health = LevelRun.IsCustom ? 3 : 3 + Mathf.RoundToInt(config.Stat(0));
            var movement = player.AddComponent<PlayerMovement>();
            movement.moveSpeed = JumpProfile.Speed + (LevelRun.IsCustom ? 0 : Mathf.Min(.75f,config.Stat(1)));
            movement.jumpForce = JumpProfile.LaunchSpeed + (LevelRun.IsCustom ? 0 : Mathf.Min(.6f,config.Stat(2)));
            player.AddComponent<PlayerCombat>();player.AddComponent<PlayerWard>();player.AddComponent<CharacterAbility>();
            var visual = CharacterCatalog.Build(player.transform,1.8f,-1);
            var presentation = player.AddComponent<CharacterPresentation>(); presentation.visual = visual;
            player.SetActive(true);
            return health;
        }
        private void Segment(int index,LevelRun run)
        {
            var s=Route[index];float x=s.x,y=s.height,w=s.width;
            bool suspended=s.room==EncounterRoom.Bridges || s.room==EncounterRoom.Collapse || world==7 || (world==0 && s.feature==RouteFeature.Canopy);
            var floor=Platform(s.Center,w,y,suspended ? .4f : Mathf.Max(2.5f,y+6),index);
            if(s.room==EncounterRoom.Collapse){var t=floor.AddComponent<WorldTrap>();t.kind=TrapKind.CrumblingFloor;t.floorY=y;}
            if(s.room==EncounterRoom.Rest && index>3 && index<segments-2 && s.pattern==0)
                Powerup(new Vector3(s.Center,y+.75f,0),BuilderPalette.Get((s.zone+world)%2==0 ? "pickup.heart" : "pickup.ward"));
            if(index==segments-2 && (difficulty==2 || difficulty==9))Powerup(new Vector3(x+1,y+.75f,0),BuilderPalette.Get("pickup.ward"));
            for(int c=0;c<(w>12 ? 5 : 2);c++)CoinAt(new Vector3(x+1+c*(w-2)/(w>12 ? 5 : 2),y+.85f,0));
            bool combat=s.room==EncounterRoom.Run || s.room==EncounterRoom.Cannons || s.room==EncounterRoom.SwitchGate || s.room==EncounterRoom.Tunnel || (w>=5 && index%2==0 && s.room!=EncounterRoom.SplitPath && s.room!=EncounterRoom.Collapse);
            if(index>1 && index<segments-1 && combat)
            {
                var enemy=SpawnEnemy(new Vector3(x+w*.65f,y+.75f,0),s.pattern%3==2,false,run,s.room==EncounterRoom.Cannons || s.pattern%3==1);
                if(s.pattern%3==0)AddTactic(enemy,(index+world)%2==0 ? EnemyTactic.Hopper : EnemyTactic.Charger);
                if(w>=13 && (difficulty>=2 || world>=4) && index%3==1)SpawnEnemy(new Vector3(x+w*.38f,y+.65f,0),false,false,run,s.room==EncounterRoom.Cannons);
            }
            if(index>0 && s.zone!=Route[index-1].zone && (s.zone%2==0 || s.room==EncounterRoom.Rest))Checkpoint(new Vector3(x+1.7f,y+1.1f,0),w-1);
            if(s.room==EncounterRoom.SplitPath)
            {
                // A single optional balcony has a reachable step and a clear reward.
                var ledge=Platform(x+w*.3f,Mathf.Min(3,w*.26f),y+1.05f,.28f,index);
                ledge.name="Schatzbalkon";ledge.AddComponent<OneWayLedge>();
                CoinAt(new Vector3(x+w*.3f,y+1.85f,0),8);
                var chest=SpawnEnemy(new Vector3(x+w*.6f,y+.65f,0),false,false,run);AddTactic(chest,EnemyTactic.Hopper);
            }
            if(s.room==EncounterRoom.Tunnel)
            {
                Platform(s.Center,w-3,y+4.2f,.65f,index).name="Tunnel ceiling";
                if(s.pattern%2==1)AddTrap(TrapKind.FallingRock,new Vector3(x+w*.7f,y+3.8f,0),y);
            }
            if(s.room==EncounterRoom.Lift)
            {
                var lift=Platform(x+w*.55f,3,y+1,.35f,index);lift.name="Beweglicher Aufzug";lift.AddComponent<MovingRide>().travel=new Vector3(0,2.4f,0);
                CoinAt(new Vector3(x+w*.55f,y+2.3f,0),8);
                if(s.pattern%2==0)SpawnEnemy(new Vector3(x+w*.85f,y+.65f,0),false,false,run,true);
            }
            if(s.room==EncounterRoom.Cannons)
            {
                AddTrap(TrapKind.Cannon,new Vector3(x+w*.8f,y+.6f,0),y);
                // The cannon itself is a solid, jumpable piece of cover. No decorative staircase.
            }
            if(s.room==EncounterRoom.SwitchGate)
            {
                var gate=Primitive("Schaltertor",PrimitiveType.Cube,new Vector3(x+w-1,y+1.5f,0),new Vector3(.4f,3,3),accent,true);
                var pad=Primitive("Druckschalter",PrimitiveType.Cube,new Vector3(x+3,y+.14f,0),new Vector3(1.8f,.28f,2.5f),gold,true);pad.GetComponent<Collider>().isTrigger=true;pad.AddComponent<PressureGate>().gate=gate;
            }
            if(s.room==EncounterRoom.Bridges && s.pattern%2==1)
            {
                // The bridge IS the route. A second low slab would cut through the guard's head.
                SpawnEnemy(new Vector3(x+w*.65f,y+.65f,0),false,false,run);
            }
            if(s.room==EncounterRoom.Collapse && s.pattern%3==0)
            {
                Platform(s.Center,2.4f,y+1.1f,.3f,index).name="Brückenwache";
                var guard=SpawnEnemy(new Vector3(s.Center,y+1.8f,0),false,false,run,true);guard.name+=" · Höhenposten";
            }
            if(s.room==EncounterRoom.Run && s.pattern%2==0)Primitive("Schatzblock",PrimitiveType.Cube,new Vector3(x+5,y+3.0f,0),Vector3.one*.85f,gold,true).AddComponent<TreasureBlock>();
            if(world>=2 && s.room==EncounterRoom.Run && s.pattern%2==1)AddTrap(TrapKind.Flame,new Vector3(x+w*.45f,y+.3f,0),y);
            if(s.room==EncounterRoom.Steps && s.pattern==1)
            {
                Spring(new Vector3(x+1,y,0));
            }
            if(s.gap>0)CoinAt(new Vector3(x+w+s.gap*.5f,y+1.6f,0));
            scenery.Section(s,index);
            openArt.Section(s,index,world);
            if(!secretBuilt && s.room==EncounterRoom.Rest && index>3 && index<segments-3 && index%3==0){SecretRoom.Build(transform,s,world,id);secretBuilt=true;}
        }
        private void Checkpoint(Vector3 point,float width)
        {
            var flag=Primitive("Checkpoint",PrimitiveType.Cylinder,point-Vector3.up*.3f,new Vector3(.16f,.8f,.16f),gold,false);
            var go=new GameObject("Checkpoint activation zone");go.transform.SetParent(transform);go.transform.position=point+new Vector3(width*.5f-1,1,0);
            var box=go.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(Mathf.Max(1,width),4,2.5f);
            var cp=go.AddComponent<CheckpointFlag>();cp.flagRenderer=flag.GetComponent<Renderer>();cp.useExplicitRespawn=true;cp.respawnPosition=point;
        }
        private void AddTrap(TrapKind kind,Vector3 position,float y)
        {
            TrapModel.Build(transform,kind,new Vector3(position.x,kind==TrapKind.FallingRock ? position.y : y,0),y,stone,gold);
        }

        private void AddTactic(GameObject enemy,EnemyTactic tactic)
        {
            if(enemy.GetComponent<EnemyRangedAttack>()!=null)return;
            enemy.AddComponent<EnemyTactics>().tactic=tactic;enemy.name+=" · "+(tactic==EnemyTactic.Hopper ? "Springer" : "Stürmer");
            // Readable silhouette: springers have winglets; chargers a broad forehead plate.
            var visual=enemy.GetComponent<EnemyPatrol>().visual;
            for(int n=0;n<(tactic==EnemyTactic.Hopper ? 2 : 1);n++)
            {
                var part=Primitive("Tactic marker",PrimitiveType.Cube,enemy.transform.position+new Vector3(n==0 ? -.4f : .4f,.4f,0),new Vector3(.5f,.16f,.6f),gold,false);part.transform.SetParent(visual,true);
            }
        }
        private void BuildCustom(LevelRun run)
        {
            var level=LevelRun.CustomLevel;run.Spawn=new Vector3(level.spawnX,level.spawnY+1.05f,0);run.Player.GetComponent<Rigidbody>().position=run.Spawn;
            foreach(var tile in level.tiles)BuildTile(tile,run);
            for(int x=0;x<level.width;x+=10){var section=new RouteSection{x=x,width=10,height=-.5f,feature=RouteFeature.Clearing};scenery.Section(section,x);openArt.Section(section,x/10,world);}
        }
        private void BuildTile(LevelBlock tile,LevelRun run)
        {
                var asset=BuilderPalette.Get(tile.asset);
                if(asset!=null && BuildAsset(tile,asset,run))return;
                Vector3 p=new Vector3(tile.x,tile.y,0);
                if(LevelCodec.Floor(tile.kind) && tile.kind!=LevelTile.Ground && tile.kind!=LevelTile.Crumble && tile.kind!=LevelTile.Lift)Platform(tile.x,1,tile.y,.65f,0);
                switch(tile.kind)
                {
                    case LevelTile.Ground:Platform(tile.x,1,tile.y,.65f,0);break;
                    case LevelTile.Crumble:var floor=Platform(tile.x,1,tile.y,.35f,0);var trap=floor.AddComponent<WorldTrap>();trap.kind=TrapKind.CrumblingFloor;trap.floorY=tile.y;break;
                    case LevelTile.Coin:CoinAt(p+Vector3.up*.55f);break;
                    case LevelTile.Enemy:case LevelTile.Shooter:case LevelTile.Hopper:case LevelTile.Charger:
                        var e=SpawnEnemy(p+Vector3.up*.7f,false,false,run,tile.kind==LevelTile.Shooter);if(tile.kind==LevelTile.Hopper || tile.kind==LevelTile.Charger)AddTactic(e,tile.kind==LevelTile.Hopper ? EnemyTactic.Hopper : EnemyTactic.Charger);break;
                    case LevelTile.Checkpoint:Checkpoint(p+Vector3.up*1.1f,1.5f);break;
                    case LevelTile.Goal:Portal(p+Vector3.up*1.3f);break;
                    case LevelTile.Lift:var lift=Platform(tile.x,2,tile.y,.3f,0);lift.AddComponent<MovingRide>();break;
                    case LevelTile.Flame:AddTrap(TrapKind.Flame,p+Vector3.up*.3f,tile.y);break;
                    case LevelTile.Cannon:AddTrap(TrapKind.Cannon,p+Vector3.up*.5f,tile.y);break;
                    case LevelTile.FallingRock:
                        float ground=tile.y-4;
                        if(run!=null){foreach(var candidate in LevelRun.CustomLevel.tiles)if(candidate.x==tile.x && candidate.y<tile.y && LevelCodec.Floor(candidate.kind))ground=Mathf.Max(ground,candidate.y);}
                        AddTrap(TrapKind.FallingRock,p,ground);break;
                    case LevelTile.Spring:Spring(p);break;
                }
        }
        public void BuildPreview(LevelTile kind,int biome)=>BuildPreview(new LevelBlock{kind=kind},biome);
        public void BuildPreview(LevelBlock tile,int biome)
        {
            config=GameConfig.Current;world=biome;difficulty=0;
            surface=gameObject.AddComponent<WorldTerrainSurface>();surface.Initialize(world);
            terrain=Material("Preview terrain",ComicPalette.Ground[world]);stone=Material("Preview stone",ComicPalette.Earth[world]);
            gold=Material("Preview gold",GameUI.Gold);accent=Material("Preview accent",ComicPalette.Accent[world]);
            BuildTile(new LevelBlock{x=0,y=0,kind=tile.kind,asset=tile.asset},null);
        }
        private GameObject Platform(float center,float width,float y,float depth,int index)
        {
            var platform=Primitive("Platform "+index,PrimitiveType.Cube,new Vector3(center,y-depth*.5f,0),new Vector3(width,depth,world==7 ? 2.4f : 3),terrain,true);
            surface.Apply(platform);return platform;
        }
        private GameObject SpawnEnemy(Vector3 position,bool monster,bool boss,LevelRun run,bool ranged=false,BuilderAsset variant=null)
        {
            var go = new GameObject(boss ? "Weltwächter" : monster ? "Steinwächter (Cactus)" : "Goblin");
            go.transform.SetParent(transform); go.transform.position = position; go.SetActive(false);
            var enemy = go.AddComponent<Enemy>();
            var box = go.GetComponent<BoxCollider>();
            float height = boss ? 2.2f : monster ? 1.4f : 1.2f;
            box.size = new Vector3(boss ? 1.2f : .65f,height,.7f);
            if (boss && run!=null) run.RegisterBoss();
            enemy.maxHealth = boss ? 18 + world * 3 : monster ? 6 + world : 3 + world / 2;
            enemy.speed = boss ? .6f : monster ? .7f : 1.3f + difficulty * .15f;
            enemy.contactDamage = 1;
            enemy.canBeStomped = !boss;
            enemy.coinReward = boss ? 25 : monster ? 5 : 1;
            enemy.scoreReward = boss ? 1000 : monster ? 300 : 100;
            enemy.deathDelay = .45f;
            if (boss && run!=null) enemy.onDefeated.AddListener(run.DefeatBoss);
            var patrol = go.GetComponent<EnemyPatrol>();
            patrol.leftDistance = boss ? .7f : 1.1f; patrol.rightDistance = boss ? .7f : 1.1f;
            bool skeleton=!monster && (world==2 || world==4 || world==6);
            var skeletonSource=variant!=null ? variant.Model : skeleton ? OpenWorldArt.Asset(ranged ? "Skeleton_Mage" : world==6 ? "Skeleton_Warrior" : world==4 ? "Skeleton_Minion" : "Skeleton_Rogue") : null;
            var visual = skeletonSource!=null ? AddVisual(skeletonSource,go.transform,height,-height/2) : !monster && ((run!=null ? run.DefeatedEnemies : 0)+Mathf.RoundToInt(position.x))%5==0
                ? AddVisual(config.goblinVisual,go.transform,height,-height/2)
                : WorldEnemyVisual.Build(go.transform,world,height,monster);
            foreach(var a in visual.GetComponentsInChildren<Animator>()) a.enabled=false;
            if(!boss) go.name=WorldEnemyVisual.Name(world,monster);
            if(run!=null && !LevelRun.IsCustom && !boss && world>=1 && Mathf.RoundToInt(position.x)%3==0)
            {
                var guard=go.AddComponent<EnemyWeaponGuard>();guard.kind=world==7 ? WeaponGuardKind.Magic : world==1 || world==3 ? WeaponGuardKind.Spellproof : WeaponGuardKind.Armor;enemy.canBeStomped=false;
                go.name+=world==7 ? " · Magieschild" : world==1 || world==3 ? " · Magieresistent" : " · Panzerung";
                for(int mark=0;mark<3;mark++) Primitive("Shield marker",PrimitiveType.Sphere,position+new Vector3((mark-1)*.22f,height*.5f+.2f,0),Vector3.one*.14f,world==7 ? accent : gold,false).transform.SetParent(go.transform,true);
            }
            patrol.visual = visual;
            go.AddComponent<CharacterPresentation>().visual = visual;
            if(skeletonSource!=null && skeletonSource.name.StartsWith("Skeleton_")){go.AddComponent<SkeletonMotion>();go.name=ranged ? "Skelettmagier" : world==6 ? "Skelettkrieger" : "Skelettspäher";}
            go.AddComponent<EnemyPresentation>();
            if(ranged) { go.name=WorldEnemyVisual.Name(world,false)+"-Schütze";go.AddComponent<EnemyRangedAttack>(); }
            var awareness=go.AddComponent<EnemyAwareness>();
            awareness.detectionRange=ranged ? 8 : monster ? 5 : 6;awareness.keepDistance=ranged && (variant!=null ? variant.retreat : world==4 || world==6);
            if(boss){go.AddComponent<WorldBoss>().world=world;}
            if(run!=null && !LevelRun.IsCustom)PatchRuntime.ApplyEnemy(go,world,boss ? "boss" : monster ? "monster" : ranged ? "shooter" : "normal");
            go.SetActive(true);return go;
        }
        private void CoinAt(Vector3 position,int value=1,GameObject model=null)
        {
            model=model??OpenWorldArt.Asset("Platformer_coin-gold")??OpenWorldArt.Asset("coin");
            var go=new GameObject("Coin");go.transform.SetParent(transform,false);go.transform.position=position;
            var collider=go.AddComponent<SphereCollider>();collider.radius=.3f;collider.isTrigger=true;
            if(model!=null)AddVisual(model,go.transform,.48f,-.24f);
            else {var fallback=Primitive("Coin visual",PrimitiveType.Cylinder,position,new Vector3(.32f,.06f,.32f),gold,false);fallback.transform.rotation=Quaternion.Euler(90,0,0);fallback.transform.SetParent(go.transform,true);}
            go.AddComponent<Coin>().value=value;go.AddComponent<CoinSpin>();
        }
        private bool BuildAsset(LevelBlock tile,BuilderAsset a,LevelRun run)
        {
            Vector3 p=new Vector3(tile.x,tile.y,0);GameObject model=a.Model;
            switch(a.role)
            {
                case BuilderRole.Block:
                    var block=Platform(tile.x,tile.kind==LevelTile.Lift ? 2 : 1,tile.y,.65f,0);block.name=a.title;
                    AssetBlockSurface.Attach(block,model,stone);
                    if(tile.kind==LevelTile.Lift){block.AddComponent<MovingRide>();}return true;
                case BuilderRole.Health:case BuilderRole.Shield:Powerup(p+Vector3.up*.7f,a);return true;
                case BuilderRole.Coin:CoinAt(p+Vector3.up*.55f,a.coins,model);return true;
                case BuilderRole.Decoration:
                    var decor=new GameObject(a.title+" (Dekoration)");decor.transform.SetParent(transform,false);decor.transform.position=p+new Vector3(0,0,2.1f);
                    AddVisual(model,decor.transform,model!=null && (a.category=="Natur" || model.name.StartsWith("wall")) ? 2.8f : 1.2f,0);return true;
                case BuilderRole.Enemy:
                    Platform(tile.x,1,tile.y,.65f,0);int oldWorld=world;if(a.enemyWorld>=0)world=a.enemyWorld;
                    var enemy=SpawnEnemy(p+Vector3.up*(a.boss ? 1.1f : a.heavy ? .7f : .6f),a.heavy,a.boss,run,tile.kind==LevelTile.Shooter,a);
                    enemy.name=a.title;world=oldWorld;return true;
                case BuilderRole.Spikes:case BuilderRole.Saw:
                    Platform(tile.x,1,tile.y,.65f,0);var hazard=new GameObject(a.title);hazard.transform.SetParent(transform,false);hazard.transform.position=p;
                    var visual=AssetBlockSurface.Fit(model,hazard.transform,new Vector3(0,a.role==BuilderRole.Saw ? .4f : .2f,0),a.role==BuilderRole.Saw ? new Vector3(.8f,.8f,.2f) : new Vector3(.9f,.4f,1));
                    var h=hazard.AddComponent<AssetHazard>();h.saw=a.role==BuilderRole.Saw;h.visual=visual;return true;
                case BuilderRole.Weapon:
                    var pickup=new GameObject(a.title);pickup.transform.SetParent(transform,false);pickup.transform.position=p+Vector3.up*.7f;
                    if(model!=null)AddVisual(model,pickup.transform,.85f,-.4f);else{var weaponModel=WeaponModel.Build(pickup.transform,a.weapon);weaponModel.transform.localPosition=Vector3.down*.4f;}var box=pickup.AddComponent<BoxCollider>();box.size=Vector3.one;box.isTrigger=true;
                    pickup.AddComponent<BuilderWeaponPickup>().weapon=a.weapon;pickup.AddComponent<CoinSpin>();return true;
                default:return false;
            }
        }
        private void Powerup(Vector3 p,BuilderAsset asset)
        {
            var root=new GameObject(asset.title);root.transform.SetParent(transform,false);root.transform.position=p;
            AddVisual(asset.Model,root.transform,asset.role==BuilderRole.Health ? .55f : .7f,asset.role==BuilderRole.Health ? -.275f : -.35f);
            var collider=root.AddComponent<SphereCollider>();collider.radius=.4f;collider.isTrigger=true;
            if(asset.role==BuilderRole.Health)root.AddComponent<HealthPickup>();else root.AddComponent<WardPickup>();root.AddComponent<CoinSpin>();
        }
        private void Spring(Vector3 p)
        {
            var spring=new GameObject("Sprungfeder");spring.transform.SetParent(transform,false);spring.transform.position=p;
            AssetBlockSurface.Fit(OpenWorldArt.Asset("Platformer_spring"),spring.transform,new Vector3(0,.15f,0),new Vector3(.8f,.3f,.8f));
            var box=spring.AddComponent<BoxCollider>();box.center=Vector3.up*.15f;box.size=new Vector3(.8f,.3f,.8f);box.isTrigger=true;spring.AddComponent<SpringPad>();
        }
        private void Portal(Vector3 position)
        {
            var root = new GameObject("Goal Portal"); root.transform.SetParent(transform); root.transform.position = position;
            var collider = root.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.size = new Vector3(1,2.6f,1.5f);
            root.AddComponent<Goal>();
            Primitive("Portal left",PrimitiveType.Cube,position + Vector3.left * .65f,new Vector3(.3f,2.8f,.5f),stone,false);
            Primitive("Portal right",PrimitiveType.Cube,position + Vector3.right * .65f,new Vector3(.3f,2.8f,.5f),stone,false);
            Primitive("Portal arch",PrimitiveType.Cube,position + Vector3.up * 1.4f,new Vector3(1.6f,.3f,.5f),stone,false);
            Primitive("Portal light",PrimitiveType.Cube,position + Vector3.forward * .2f,new Vector3(1.1f,2.5f,.08f),accent,false);
        }
        private void Decoration(GameObject source,Vector3 position,float scale)
        {
            var instance = Instantiate(source,position,Quaternion.Euler(0,random.Next(360),0),transform);
            instance.transform.localScale *= scale;
            foreach (Collider c in instance.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (Rigidbody b in instance.GetComponentsInChildren<Rigidbody>()) b.isKinematic = true;
        }
        public static Transform AddVisual(GameObject source,Transform parent,float height,float feetY)
        {
            if (source == null) return null;
            var instance = Instantiate(source,parent); instance.name = "Visual";
            instance.transform.localPosition = Vector3.zero; instance.transform.localRotation = Quaternion.identity;
            foreach (Collider c in instance.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>()) Destroy(body);
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>()) animator.applyRootMotion = false;
            Bounds bounds = VisualBounds(instance);
            instance.transform.localScale *= height / Mathf.Max(.01f,bounds.size.y);
            bounds = VisualBounds(instance);
            instance.transform.position += new Vector3(parent.position.x - bounds.center.x,
                parent.position.y + feetY - bounds.min.y,parent.position.z - bounds.center.z);
            return instance.transform;
        }
        private static Bounds VisualBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(instance.transform.position,Vector3.one);
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
        private Material Material(string title,Color color)
        {
            var material = new Material(ComicPalette.Shader);
            material.name = title; material.SetColor("_BaseColor",color);
            return material;
        }
        private GameObject Primitive(string title,PrimitiveType type,Vector3 position,Vector3 scale,Material material,bool collide)
        {
            var go = GameObject.CreatePrimitive(type); go.name = title; go.transform.SetParent(transform);
            go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collide) Destroy(go.GetComponent<Collider>());
            return go;
        }
        private void OnDestroy()
        {
            // Runtime-created materials are shared by level geometry, then released with the level.
            var materials = new System.Collections.Generic.HashSet<Material>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial != null && (r.sharedMaterial == terrain || r.sharedMaterial == stone ||
                    r.sharedMaterial == gold || r.sharedMaterial == accent || r.sharedMaterial == bark ||
                    r.sharedMaterial.name == "Thorns")) materials.Add(r.sharedMaterial);
            foreach (var material in materials) Destroy(material);
        }
    }
}
