using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class EnemyPatrol : MonoBehaviour
{
    [Tooltip("Distances from the starting position, in world units.")]
    [Min(0)] public float leftDistance = 1.5f;
    [Min(0)] public float rightDistance = 1.5f;
    public bool startMovingRight = true;
    public bool avoidLedges = true;
    public LayerMask environmentLayers = ~0;
    public Transform visual;
    public float visualYawOffset;
    public bool Suspended { get; set; }
    public bool IsMoving { get; private set; }
    public bool EditorPreview {get;set;}
    private Rigidbody body;
    private BoxCollider hitbox;
    private Enemy enemy;
    private TreasureRunner.EnemyAwareness awareness;
    private Vector3 origin;
    private int direction;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        hitbox = GetComponent<BoxCollider>();
        enemy = GetComponent<Enemy>();
        awareness=GetComponent<TreasureRunner.EnemyAwareness>();
        origin = body.position;
        direction = startMovingRight ? 1 : -1;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        IsMoving=false;
        if (Suspended || enemy == null || enemy.IsDefeated || enemy.speed <= 0) return;
        bool reacting=awareness!=null && awareness.State!=TreasureRunner.EnemyAwarenessState.Patrolling;
        int wanted=awareness!=null ? awareness.MoveDirection(direction) : direction;
        if(reacting) Face(awareness.HasTarget ? awareness.TargetFacing : wanted);
        if(wanted==0) return;
        direction=wanted;
        float step = enemy.speed * (awareness!=null ? awareness.SpeedMultiplier : 1) * Time.fixedDeltaTime;
        Vector3 next = body.position + Vector3.right * (direction * step);
        Bounds bounds = EditorPreview ? new Bounds(body.position+transform.TransformVector(hitbox.center),Vector3.Scale(hitbox.size,transform.lossyScale)) : hitbox.bounds;
        float front = bounds.center.x + direction * (bounds.extents.x + step + 0.03f);
        bool blocked = HasEnvironment(bounds.center, Vector3.right * direction,
            bounds.extents.x + step + 0.03f);
        bool hasFloor = !avoidLedges || HasEnvironment(
            new Vector3(front, bounds.min.y + 0.1f, bounds.center.z), Vector3.down, 0.5f);
        float min=reacting ? awareness.MinX : origin.x-leftDistance;
        float max=reacting ? awareness.MaxX : origin.x+rightDistance;
        if (next.x < min || next.x > max || blocked || !hasFloor)
        {
            if(!reacting) direction *= -1;
            return;
        }
        next.z = origin.z;
        body.MovePosition(next);IsMoving=true;
        Face(reacting && awareness.HasTarget ? awareness.TargetFacing : direction);
    }
    private void Face(int facing)
    {
        if(visual!=null && facing!=0) visual.localRotation=Quaternion.Euler(0,(facing>0 ? 90 : -90)+visualYawOffset,0);
    }

    private bool HasEnvironment(Vector3 position, Vector3 directionVector, float distance)
    {
        int count = Physics.RaycastNonAlloc(position, directionVector, hits, distance,
            environmentLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i].collider;
            if (c.attachedRigidbody == body || c.GetComponentInParent<Enemy>() != null ||
                c.GetComponentInParent<PlayerMovement>() != null) continue;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? origin : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - Vector3.right * leftDistance, center + Vector3.right * rightDistance);
        Gizmos.DrawWireSphere(center - Vector3.right * leftDistance, 0.1f);
        Gizmos.DrawWireSphere(center + Vector3.right * rightDistance, 0.1f);
    }
}
