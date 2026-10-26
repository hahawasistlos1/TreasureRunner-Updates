#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
namespace TreasureRunner
{
    public class CharacterChoiceChecks:MonoBehaviour
    {
        readonly List<string> report=new List<string>();bool errors;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);report.Add("PASS: "+text);}
        void Log(string text,string stack,LogType kind){if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert){errors=true;report.Add("UNITY ERROR: "+text+"\n"+stack);}}
        IEnumerator Start()
        {
            DontDestroyOnLoad(gameObject);Application.runInBackground=true;Directory.CreateDirectory("Logs");Directory.CreateDirectory("Builds/CharacterPreview");string saved=PlayerPrefs.GetString(ProgressStore.SaveKey,""),backup=PlayerPrefs.GetString(ProgressStore.BackupKey,"");ProgressStore.BeginMemoryTest();Application.logMessageReceived+=Log;var sequence=Scenarios();bool failed=false;
            while(true){bool more;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){report.Add("FAIL: "+e);failed=true;break;}if(!more)break;yield return current;}
            PatchRuntime.SetTestStore(null);ProgressStore.EndMemoryTest();Time.timeScale=1;Application.logMessageReceived-=Log;if(saved!=PlayerPrefs.GetString(ProgressStore.SaveKey,"") || backup!=PlayerPrefs.GetString(ProgressStore.BackupKey,"")){failed=true;report.Add("FAIL: Real save changed");}else report.Add("PASS: Real player save and backup unchanged");failed|=errors;report.Add(failed ? "CHARACTER TESTS FAILED" : "ALL CHARACTER TESTS PASSED");File.WriteAllLines("Logs/CharacterChoiceTests.txt",report);foreach(var line in report)Debug.Log(line);EditorApplication.Exit(failed ? 1 : 0);
        }
        Enemy Target(Vector3 position)
        {var root=new GameObject("Character weapon test target");root.SetActive(false);root.transform.position=position;var enemy=root.AddComponent<Enemy>();enemy.maxHealth=50;enemy.speed=0;enemy.canBeStomped=false;root.GetComponent<BoxCollider>().size=new Vector3(.65f,1.2f,.7f);root.SetActive(true);return enemy;}
        IEnumerator Scenarios()
        {
            var data=ProgressStore.Data;data.weapons=new List<string>{"sword","axe","bow","staff"};data.coins=753;data.skin="Abenteurer";data.characterId=null;Check(CharacterCatalog.Current.id=="goblin","Old save without a character field retains original hero");data.characterId="unknown";Check(CharacterCatalog.Current.id=="goblin","Unknown saved figure falls back safely");Check(!CharacterCatalog.Select("unknown") && data.characterId=="unknown","Invalid figure selection does not overwrite saved choice");
            Check(CharacterCatalog.All.Length==4 && CharacterCatalog.All.Select(c=>c.Model).Distinct().Count()==4,"Four genuinely different available character models");
            foreach(var character in CharacterCatalog.All)
            {
                Check(character.Model!=null && CharacterCatalog.Select(character.id),"Figure can be selected without a purchase: "+character.title);Check(data.coins==753,"Figure selection does not charge coins: "+character.id);data.equippedWeapon="sword";
                LevelRun.StartLevel(0);yield return null;yield return null;yield return null;var run=LevelRun.Instance;var player=run.Player;var movement=player.GetComponent<PlayerMovement>();var body=player.GetComponent<Rigidbody>();var visual=player.GetComponentInChildren<CharacterVisualId>();
                Check(visual!=null && visual.id==character.id,"Campaign instantiates selected figure: "+character.id);var capsule=player.GetComponent<CapsuleCollider>();Check(capsule.height==2 && capsule.radius==.5f && player.health==3 && player.HeartUnits==6,"Figure preserves original hitbox, lives and hearts: "+character.id);
                foreach(var bone in new[]{HumanBodyBones.LeftUpperLeg,HumanBodyBones.RightUpperLeg,HumanBodyBones.LeftUpperArm,HumanBodyBones.RightUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.RightLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightHand})Check(CharacterRig.Bone(visual.transform,bone)!=null,"Usable weapon/movement bone: "+character.id+" / "+bone);
                Check(visual.GetComponentsInChildren<Animation>().All(a=>!a.enabled),"Legacy clips do not overwrite animated player weapon pose: "+character.id);
                var camera=Camera.main;camera.GetComponent<CameraFollow>().enabled=false;camera.transform.position=player.transform.position+new Vector3(1,1.7f,-6);camera.transform.LookAt(player.transform.position+Vector3.up*.1f);Capture("Spiel-"+character.id,camera);
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(1000,-.5f,0);floor.transform.localScale=new Vector3(60,1,3);body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
                // Let the screenshot canvas restore, then measure a fixed number of physics steps.
                yield return null;yield return null;
                float x=body.position.x;movement.SetTouchDirection(1,true);yield return null;for(int tick=0;tick<8;tick++)yield return new WaitForFixedUpdate();movement.ClearInput();Check(body.position.x>x+.3f,"Figure physically walks forward: "+character.id+" (distance "+(body.position.x-x)+", state "+run.State+")");movement.RequestJump();yield return new WaitForSeconds(.10f);Check(body.linearVelocity.y>0,"Figure uses existing jump physics: "+character.id);yield return new WaitForSeconds(.9f);movement.ControlLocked=true;body.useGravity=false;body.position=new Vector3(1000,1.05f,0);body.linearVelocity=Vector3.zero;
                var combat=player.GetComponent<PlayerCombat>();var presentation=player.GetComponent<CharacterPresentation>();
                foreach(string id in new[]{"sword","axe","bow","staff"})
                {
                    ShopService.Equip(id);presentation.RefreshWeapon();yield return new WaitForSeconds(1.4f);var weapon=visual.GetComponentsInChildren<WeaponModel>().Single(w=>w.gameObject.activeInHierarchy);Check(weapon.name=="Equipped "+id,"Correct visible weapon after switching: "+character.id+" / "+id);
                    var target=Target(body.position+Vector3.right*(id=="bow" || id=="staff" ? 3 : 1.25f));int before=target.CurrentHealth;combat.Attack();yield return new WaitForSeconds(.65f);int expected=id=="axe" ? 3 : id=="staff" ? 2 : 1;Check(target.CurrentHealth==before-expected,"Real animated/projectile attack hits exactly once: "+character.id+" / "+id+" (HP "+target.CurrentHealth+")");target.gameObject.SetActive(false);Destroy(target.gameObject);
                }
                ShopService.Equip("sword");presentation.RefreshWeapon();yield return new WaitForSeconds(1.4f);var far=Target(body.position+Vector3.right*3);combat.Attack();yield return new WaitForSeconds(.5f);Check(far.CurrentHealth==50,"Visible blade cannot hit a distant enemy: "+character.id);Destroy(far.gameObject);
                player.GrantInvulnerability(0);int hearts=player.HeartUnits;player.TakeDamage(1);Check(player.HeartUnits==hearts-1,"Figure takes ordinary half-heart damage: "+character.id);bool blink=false;float until=Time.time+.4f;while(Time.time<until){blink|=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Any(r=>!r.enabled);yield return null;}Check(blink,"Selected figure keeps hit blink feedback: "+character.id);
                // Keep selection tests independent from incidental coins collected at spawn.
                data.coins=753;
            }
            LevelRun.Instance.Menu();yield return null;yield return null;yield return null;var menu=FindFirstObjectByType<MainMenuController>();menu.Show("Charakter");yield return new WaitForSeconds(.4f);var beforeSelection=JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(data));var card=GameObject.Find("Skelett-Späher");card.GetComponentInChildren<Button>().onClick.Invoke();yield return new WaitForSeconds(.4f);beforeSelection.characterId="skeleton-scout";Check(data.characterId=="skeleton-scout" && JsonUtility.ToJson(data)==JsonUtility.ToJson(beforeSelection),"Actual figure card changes only selected character and preserves progress");
            Check(JsonUtility.FromJson<ProgressData>(JsonUtility.ToJson(data)).characterId=="skeleton-scout","Figure choice survives save JSON round-trip");Check(FindObjectsByType<CharacterPreview>(FindObjectsSortMode.None).Count(p=>!p.staticImage)==1 && FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.name=="Preview camera")==1,"Portrait cards leave only one live character camera active");
            var portraits=FindObjectsByType<CharacterPreview>(FindObjectsSortMode.None).Where(p=>p.staticImage).ToArray();var hashes=new HashSet<string>();foreach(var portrait in portraits){var rt=(RenderTexture)portrait.GetComponent<RawImage>().texture;var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();Check(image.GetPixels32().Count(p=>p.r>70 || p.g>80 || p.b>90)>150,"Visible real portrait: "+portrait.characterId);hashes.Add(PatchCodec.Hash(image.EncodeToPNG()));File.WriteAllBytes("Builds/CharacterPreview/Portraet-"+portrait.characterId+".png",image.EncodeToPNG());RenderTexture.active=active;Destroy(image);}Check(hashes.Count==4,"All four portrait images are visually distinct");Capture("Figurenauswahl",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));
            var scroll=FindObjectsByType<ScrollRect>(FindObjectsSortMode.None).First();scroll.verticalNormalizedPosition=.58f;yield return null;Capture("Figurenkarten",FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Menu Camera"));menu.Show("Start");yield return new WaitForSeconds(.3f);Check(FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Skelett-Späher"),"Dashboard displays actual selected character name");
            var custom=LevelCodec.Starter();LevelRun.StartCustom(custom);yield return null;yield return null;yield return null;Check(LevelRun.Instance.Player.GetComponentInChildren<CharacterVisualId>().id=="skeleton-scout","Editor testplay also uses selected figure");Check(Application.version==ReleaseInfo.Version && PlayerSettings.Android.bundleVersionCode==ReleaseInfo.AndroidCode,"Character addition does not change app or Android version");
        }
        void Capture(string name,Camera camera)
        {
            var canvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var rt=new RenderTexture(1600,900,24);var before=camera.targetTexture;camera.targetTexture=rt;foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}Canvas.ForceUpdateCanvases();camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes("Builds/CharacterPreview/"+name+".png",image.EncodeToPNG());RenderTexture.active=active;camera.targetTexture=before;rt.Release();Destroy(rt);Destroy(image);for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];}
        }
    }
}
#endif
