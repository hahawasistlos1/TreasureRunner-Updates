using UnityEngine;
using UnityEngine.UI;
namespace TreasureRunner
{
    public class CharacterPreview : MonoBehaviour
    {
        public string characterId;public bool staticImage;public float zoom=1;
        static int nextStage;
        private GameObject stage;
        private RenderTexture texture;
        private Transform visual;
        private void Start()
        {
            stage = new GameObject("Preview stage");
            stage.transform.position = new Vector3(++nextStage*6,0,18000);
            visual = CharacterCatalog.Build(stage.transform,1.8f,0,characterId);
            if(visual!=null)foreach(var animator in visual.GetComponentsInChildren<Animator>())animator.enabled=false;
            if(visual!=null)visual.localRotation=Quaternion.Euler(0,162,0);
            VisualStyle.Apply(visual);
            if (visual != null){CharacterRig.RelaxArms(visual);VisualStyle.Weapon(visual);}
            if (visual != null)
                foreach (Transform child in visual.GetComponentsInChildren<Transform>()) child.gameObject.layer = 30;
            var cameraObject = new GameObject("Preview camera",typeof(Camera));
            cameraObject.transform.SetParent(stage.transform);
            cameraObject.transform.localPosition = new Vector3(0,zoom>1 ? .9f : 1,-3.8f/Mathf.Clamp(zoom,.75f,1.5f));
            Camera camera = cameraObject.GetComponent<Camera>();
            cameraObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing=false;
            camera.cullingMask = 1 << 30; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f,.12f,.15f);
            camera.fieldOfView = 35;
            texture = new RenderTexture(256,384,16);
            camera.targetTexture = texture;
            GetComponent<RawImage>().texture = texture;
            var light = new GameObject("Preview light",typeof(Light));
            light.transform.SetParent(stage.transform); light.transform.localRotation = Quaternion.Euler(30,-30,0);
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().cullingMask = 1 << 30;
            if(staticImage){camera.Render();camera.Render();stage.SetActive(false);}
        }
        private void Update() { if (!staticImage && visual != null) visual.localRotation=Quaternion.Euler(0,162+Mathf.Sin(Time.unscaledTime*.7f)*6,0); }
        private void OnDestroy()
        {
            if (stage != null) Destroy(stage);
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
