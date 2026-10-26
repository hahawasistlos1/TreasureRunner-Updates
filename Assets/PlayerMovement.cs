using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = TreasureRunner.JumpProfile.Speed;
    public float jumpForce = TreasureRunner.JumpProfile.LaunchSpeed;
    private Rigidbody rb;
    private PhysicsMaterial movementMaterial;
    private float move;
    private bool jumpRequested;
    private readonly HashSet<Collider> groundContacts = new HashSet<Collider>();
    private float ignoreGroundUntil;
    private bool touchLeft;
    private bool touchRight;
    private float touchAxis;
    public bool ControlLocked { get; set; }
    public int Facing { get; private set; } = 1;
    public bool IsGrounded => groundContacts.Count > 0 && Time.fixedTime >= ignoreGroundUntil;
    public TreasureRunner.MovingRide SupportRide
    {
        get
        {
            if(!IsGrounded)return null;
            var feet=GetComponent<Collider>().bounds;
            foreach(var collider in groundContacts)
            {
                if(collider==null || !collider.enabled || !collider.gameObject.activeInHierarchy)continue;
                var ride=collider.GetComponentInParent<TreasureRunner.MovingRide>();if(ride==null || !ride.isActiveAndEnabled)continue;
                var surface=collider.bounds;
                if(feet.max.x>surface.min.x && feet.min.x<surface.max.x && Mathf.Abs(feet.min.y-surface.max.y)<.3f)return ride;
            }
            return null;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Collider bodyCollider = GetComponent<Collider>();
        if (bodyCollider != null)
        {
            movementMaterial = new PhysicsMaterial("Player slide")
            {
                dynamicFriction = 0,
                staticFriction = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0
            };
            bodyCollider.material = movementMaterial;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        move = 0;
        if(ControlLocked) { jumpRequested=false;return; }
        if (Time.timeScale == 0f) { jumpRequested = false; return; }
        move = touchAxis + (touchRight ? 1 : 0) - (touchLeft ? 1 : 0);
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move += 1;
            jumpRequested |= keyboard.spaceKey.wasPressedThisFrame;
        }
        move = Mathf.Clamp(move,-1,1);
        if (move != 0) Facing = move > 0 ? 1 : -1;
    }

    private void FixedUpdate()
    {
        groundContacts.RemoveWhere(c=>c==null || !c.enabled || !c.gameObject.activeInHierarchy);
        Vector3 velocity = rb.linearVelocity;
        if(ControlLocked) move=0;
        var ride=SupportRide;var carried=ride!=null ? ride.Velocity : Vector3.zero;
        // Keep ordinary Rigidbody collision resolution: never teleport a rider through walls or floors.
        velocity.x = move * moveSpeed+carried.x;
        if(ride!=null)velocity.y=carried.y;
        velocity.z = 0;
        velocity.y = Mathf.Max(-TreasureRunner.JumpProfile.TerminalSpeed,velocity.y);
        rb.linearVelocity = velocity;
        if(rb.useGravity)rb.AddForce(Vector3.down * Mathf.Max(0,TreasureRunner.JumpProfile.Gravity+Physics.gravity.y),ForceMode.Acceleration);
        if (jumpRequested && IsGrounded) Bounce(jumpForce);
        jumpRequested = false;
    }

    public void Bounce(float upwardSpeed)
    {
        Vector3 velocity = rb.linearVelocity;
        velocity.y = upwardSpeed;
        rb.linearVelocity = velocity;
        ignoreGroundUntil = Time.fixedTime + 0.1f;
        groundContacts.Clear();
        jumpRequested = false;
        TreasureRunner.GameAudio.Instance?.Play("jump");
    }

    public void RequestJump() { if (Time.timeScale > 0) jumpRequested = true; }
    public void Face(int direction) { if (direction != 0) Facing = direction > 0 ? 1 : -1; }
    public void SetTouchDirection(int direction,bool pressed)
    {
        if (direction < 0) touchLeft = pressed; else touchRight = pressed;
    }
    public void SetTouchAxis(float value) { touchAxis=Mathf.Clamp(value,-1,1); }
    public void ClearInput() { touchLeft = touchRight = jumpRequested = false; touchAxis=move=0; }
    private void OnDisable() => ClearInput();
    private void OnDestroy() { if (movementMaterial != null) Destroy(movementMaterial); }
    private void OnApplicationFocus(bool focused) { if (!focused) ClearInput(); }

    private void OnCollisionEnter(Collision collision) => CheckGround(collision);
    private void OnCollisionStay(Collision collision) => CheckGround(collision);
    private void OnCollisionExit(Collision collision) => groundContacts.Remove(collision.collider);

    private void CheckGround(Collision collision)
    {
        var ride=collision.collider.GetComponentInParent<TreasureRunner.MovingRide>();
        float surfaceY=ride!=null ? ride.Velocity.y : 0;
        if (Time.fixedTime < ignoreGroundUntil || rb.linearVelocity.y-surfaceY > 0.3f){groundContacts.Remove(collision.collider);return;}
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y > 0.6f)
            { groundContacts.Add(collision.collider); return; }
        groundContacts.Remove(collision.collider);
    }
}
