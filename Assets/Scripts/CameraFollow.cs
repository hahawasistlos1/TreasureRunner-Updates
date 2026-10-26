using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public Vector3 offset = new Vector3(0, 5, -10);
    [Min(0)] public float smoothing;

    void LateUpdate()
    {
        if (player == null) return;
        Vector3 destination = player.position + offset;
        transform.position = smoothing <= 0 ? destination : Vector3.Lerp(transform.position,destination,1 - Mathf.Exp(-smoothing * Time.deltaTime));
    }
}
