using System;
using System.Linq;
using UnityEngine;
namespace TreasureRunner
{
    public class CharacterDefinition
    {
        public readonly string id,title,description,prefab,abilityId;
        public CharacterDefinition(string id,string title,string description,string prefab=null,string ability=null){this.id=id;this.title=title;this.description=description;this.prefab=prefab;abilityId=ability??id;}
        public GameObject Model=>prefab==null ? GameConfig.Current.characterVisual : prefab.StartsWith("AssetLibrary/") ? Resources.Load<GameObject>(prefab) : OpenWorldArt.Asset(prefab);
    }
    public static class CharacterCatalog
    {
        static readonly CharacterDefinition[] Original={
            new CharacterDefinition("goblin","Goblin-Abenteurer","Dein bisheriger Schatzsucher."),
            new CharacterDefinition("skeleton-knight","Skelett-Ritter","Ein knöcherner Held im Ritterstil.","Skeleton_Warrior"),
            new CharacterDefinition("skeleton-scout","Skelett-Späher","Ein Skelett im Stil eines Schatzspähers.","Skeleton_Rogue"),
            new CharacterDefinition("skeleton-mage","Skelett-Magier","Ein knöcherner Runenkundiger.","Skeleton_Mage")
        };
        static CharacterDefinition[] imported;
        public static void Refresh(){imported=null;}
        public static CharacterDefinition[] All=>imported??(imported=Original.Concat(AssetLibrary.Data.characters.Select(c=>new CharacterDefinition(c.id,c.title,"Kostenlose KayKit-Figur · nutzt die Verbesserungen ihrer bestehenden Charakterklasse.",c.resource,c.ability))).ToArray());
        public static CharacterDefinition Get(string id)=>All.FirstOrDefault(c=>c.id==id)??All[0];
        public static CharacterDefinition Current {get{var choice=Get(ProgressStore.Data.characterId);return choice.Model!=null ? choice : All[0];}}
        public static bool Select(string id)
        {var choice=All.FirstOrDefault(c=>c.id==id);if(choice==null || choice.Model==null || ProgressStore.IsReadOnly)return false;ProgressStore.Data.characterId=id;if(!ArsenalProgression.CanUse(ProgressStore.Data.equippedWeapon,id))ProgressStore.Data.equippedWeapon="sword";ProgressStore.Save();return true;}
        public static Transform Build(Transform parent,float height,float feetY,string id=null)
        {
            var choice=id==null ? Current : Get(id);if(choice.Model==null)choice=All[0];var visual=LevelFactory.AddVisual(choice.Model,parent,height,feetY,true);if(visual==null)return null;
            foreach(var animation in visual.GetComponentsInChildren<Animation>())animation.enabled=false;
            visual.gameObject.AddComponent<CharacterVisualId>().id=choice.id;return visual;
        }
    }
    public class CharacterVisualId:MonoBehaviour {public string id;}
    public static class CharacterRig
    {
        public static bool Humanoid(Transform visual){var animator=visual.GetComponentInChildren<Animator>();return animator!=null && animator.avatar!=null && animator.isHuman;}
        public static Transform Bone(Transform visual,HumanBodyBones bone)
        {
            var animator=visual.GetComponentInChildren<Animator>();if(animator!=null && animator.avatar!=null && animator.isHuman)return animator.GetBoneTransform(bone);
            string name=bone==HumanBodyBones.LeftUpperLeg ? "upperleg.l" : bone==HumanBodyBones.RightUpperLeg ? "upperleg.r" : bone==HumanBodyBones.LeftUpperArm ? "upperarm.l" : bone==HumanBodyBones.RightUpperArm ? "upperarm.r" : bone==HumanBodyBones.LeftLowerArm ? "lowerarm.l" : bone==HumanBodyBones.RightLowerArm ? "lowerarm.r" : bone==HumanBodyBones.LeftHand ? "hand.l" : bone==HumanBodyBones.RightHand ? "hand.r" : "";
            return visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name==name);
        }
        public static void RelaxArms(Transform visual)
        {
            foreach(bool right in new[]{false,true})
            {var upper=Bone(visual,right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);var lower=Bone(visual,right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);var hand=Bone(visual,right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);if(upper!=null && lower!=null && hand!=null)ForwardArmPose.Reach(upper,lower,hand,upper.position+Vector3.down*.45f+visual.forward*.05f);}
        }
    }
}
