using UnityEngine;
namespace TreasureRunner
{
    public static class ForwardArmPose
    {
        // Solve in world space so imported bone axes cannot turn the slash sideways.
        public static void Reach(Transform upper,Transform lower,Transform hand,Vector3 target)
        {
            if(upper==null || lower==null || hand==null) return;
            Vector3 start=upper.position;
            float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,hand.position);
            if(a<.001f || b<.001f) return;
            Vector3 delta=target-start;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
            Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(Vector3.down,direction).normalized;
            if(bend.sqrMagnitude<.1f) bend=Vector3.back;
            float along=(a*a+distance*distance-b*b)/(2*distance);
            Vector3 elbow=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
            Vector3 end=start+direction*distance;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,end-lower.position)*lower.rotation;
        }
        public static void AimWeapon(Transform weapon,MeshFilter[] meshes,Renderer[] renderers,Vector3 hand,Vector3 bladeDirection)
        {
            if(weapon==null) return;
            MeshFilter longest=null;float largest=0;Vector3 axis=Vector3.up;
            foreach(var mesh in meshes)
            {
                if(mesh.sharedMesh==null) continue;
                Vector3 size=mesh.sharedMesh.bounds.size;
                Vector3 local=size.x>size.y && size.x>size.z ? Vector3.right : size.z>size.y ? Vector3.forward : Vector3.up;
                float length=mesh.transform.TransformVector(Vector3.Scale(local,size)).magnitude;
                if(length>largest) { largest=length;longest=mesh;axis=local; }
            }
            if(longest==null) return;
            Vector3 current=longest.transform.TransformDirection(axis);
            weapon.rotation=Quaternion.FromToRotation(current,bladeDirection)*weapon.rotation;
            if(renderers.Length==0) return;
            Bounds bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++) bounds.Encapsulate(renderers[i].bounds);
            weapon.position+=hand+bladeDirection.normalized*.22f-bounds.center;
        }
    }
}
