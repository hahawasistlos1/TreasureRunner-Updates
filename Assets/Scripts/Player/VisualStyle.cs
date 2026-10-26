using UnityEngine;
namespace TreasureRunner
{
    public static class VisualStyle
    {
        public static void Apply(Transform visual)
        {
            if (visual == null) return;
            string skin = ProgressStore.Data.skin;
            if (skin == "Abenteurer") return;
            Color tint = skin == "Waldläufer" ? new Color(.65f,1,.65f) : new Color(.6f,.85f,1.2f);
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor",tint);
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>()) renderer.SetPropertyBlock(block);
        }
        public static void Weapon(Animator animator)
        {
            if(animator!=null)Weapon(animator.transform);
        }
        public static void Weapon(Transform visual)
        {
            if(visual==null)return;Transform hand=CharacterRig.Bone(visual,HumanBodyBones.RightHand);
            if (hand == null) return;
            var model=WeaponModel.Build(hand,GameConfig.Current.EquippedWeapon.id);model.Aim(hand.position,Vector3.up);
        }

        private static void FitToHand(Transform model,Transform hand)
        {
            // Imported skeletons can have a large scale on their hand bones.
            // Normalize world bounds for imported and generated weapons alike.
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            Bounds bounds = BoundsOf(renderers);
            float largest = Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));
            model.localScale *= .75f / Mathf.Max(.001f,largest);
            bounds = BoundsOf(renderers);
            model.position += hand.position - bounds.center + Vector3.up * .18f;
        }
        private static Bounds BoundsOf(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
        private static void Part(Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube); part.transform.SetParent(parent,false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Collider>().enabled = false; Object.Destroy(part.GetComponent<Collider>());
            part.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
