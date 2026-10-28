using UnityEngine;
namespace TreasureRunner
{
    // 2D library graphics remain readable in the 3D gameplay lane when actors turn.
    [DefaultExecutionOrder(250)] public class LibraryPlaneVisual:MonoBehaviour
    {
        public string[] frames;
        Vector3 scale;Texture2D[] textures;Renderer graphic;MaterialPropertyBlock block;int facing=1;
        void Start(){scale=transform.localScale;graphic=GetComponent<Renderer>();block=new MaterialPropertyBlock();if(frames!=null)textures=System.Array.ConvertAll(frames,p=>Resources.Load<Texture2D>(p));}
        void LateUpdate()
        {
            var actor=GetComponentInParent<PlayerMovement>();var patrol=GetComponentInParent<EnemyPatrol>();
            if(actor==null && patrol==null)return;
            float angle=transform.localEulerAngles.y;if(angle>45 && angle<315)facing=angle>180 ? -1 : 1;
            transform.rotation=Quaternion.identity;
            transform.localScale=new Vector3(Mathf.Abs(scale.x)*(actor!=null ? actor.Facing : facing),scale.y,scale.z);
            if(graphic!=null && textures!=null && textures.Length>0 && (actor!=null || patrol.IsMoving))
            {var texture=textures[Mathf.FloorToInt(Time.time*7)%textures.Length];if(texture!=null){graphic.GetPropertyBlock(block);block.SetTexture("_MainTex",texture);graphic.SetPropertyBlock(block);}}
        }
    }
}
