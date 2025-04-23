using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections))]
public class Knight : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float walkStopRate = 0.05f;
    public DetectionZone attackZone;
    // Add these new fields
    [Header("Ledge Detection")]
    [SerializeField] private float ledgeCheckDistance = 0.5f; // How far ahead to check for ground
    [SerializeField] private LayerMask groundLayer; // Set this in the inspector to your ground layer
    
    Rigidbody2D rb;
    TouchingDirections touchingDirections;
    Animator animator;


    public enum WalkableDirection { Right, Left }

    private WalkableDirection _walkDirection;
    private Vector2 WalkDirectionVector = Vector2.right;

    public WalkableDirection WalkDirection{
        get {
            return _walkDirection;
        }
        set {
            if(_walkDirection != value)
            {
                gameObject.transform.localScale = new Vector2(gameObject.transform.localScale.x * -1, gameObject.transform.localScale.y);

                if(value == WalkableDirection.Right)
                {
                    WalkDirectionVector = Vector2.right;
                } else if(value == WalkableDirection.Left)
                {
                    WalkDirectionVector = Vector2.left;
                }
            }

            _walkDirection = value;
        }
    }

    public bool _hasTarget = false;

    public bool HasTarget { get 
    {
        return _hasTarget;
    } private set
    {
        _hasTarget = value;
        animator.SetBool(AnimationStrings.hasTarget, value);
    } }

    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
    }

        void Update()
    {
        HasTarget = attackZone.detectedColliders.Count > 0;
    }

    private float lastFlipTime = 0f;
    private float flipCooldown = 0.5f; // Wait at least half a second between flips

    private void FixedUpdate()
    {
        // First check for ledges - if no ground ahead, flip direction
        if (touchingDirections.IsGrounded && !IsGroundAhead() && 
            Time.time >= lastFlipTime + flipCooldown)
        {
            FlipDirection();
            lastFlipTime = Time.time;
            return; // Skip the rest of the logic for this frame
        }

        // Existing wall check logic
        if(touchingDirections.IsOnWall && touchingDirections.IsGrounded && 
           Time.time >= lastFlipTime + flipCooldown)
        {
            FlipDirection();
            lastFlipTime = Time.time;
        }
        
        if (CanMove)
        {
            rb.linearVelocity = new Vector2(walkSpeed * WalkDirectionVector.x, rb.linearVelocity.y);
        }
        else {
            rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, 0, walkStopRate), rb.linearVelocity.y);
        }
        
    }

    private bool IsGroundAhead()
    {
        // Calculate the position to check from (at the knight's feet)
        Vector2 rayStart = new Vector2(
            transform.position.x + (WalkDirectionVector.x * 0.5f), // Start slightly ahead
            transform.position.y - GetComponent<Collider2D>().bounds.extents.y + 0.1f); // From the bottom of collider
        
        // Cast a ray downward from ahead of the knight
        RaycastHit2D hit = Physics2D.Raycast(
            rayStart, 
            Vector2.down, 
            ledgeCheckDistance, 
            groundLayer);
        
        // Visualize the ray in the Scene view
        Debug.DrawRay(rayStart, Vector2.down * ledgeCheckDistance, hit ? Color.green : Color.red);
        
        return hit.collider != null;
    }

    private void FlipDirection()
    {
        if(WalkDirection == WalkableDirection.Right)
        {
            WalkDirection = WalkableDirection.Left;
        } 
        else if(WalkDirection == WalkableDirection.Left)
        {
            WalkDirection = WalkableDirection.Right;
        }
        else
        {
            Debug.LogError("Current walkable direction is not set to legal values of left or right");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame

}
