using NUnit.Framework;
using UnityEngine;

public class TouchingDirections : MonoBehaviour
{
    // Public settings
    public ContactFilter2D castFilter;
    public float groundDistance = 0.05f;
    public float wallDistance = 0.2f;
    public float ceilingDistance = 0.05f;

    // Wall check directions
    public Vector2 rightWallCheckDirection = Vector2.right;
    public Vector2 leftWallCheckDirection = Vector2.left;

    // Private components
    private Animator animator;
    private CapsuleCollider2D touchingCol;
    private Rigidbody2D rb;

    // Raycast hit arrays
    private RaycastHit2D[] groundHits = new RaycastHit2D[5];
    private RaycastHit2D[] rightWallHits = new RaycastHit2D[5];
    private RaycastHit2D[] leftWallHits = new RaycastHit2D[5];
    private RaycastHit2D[] ceilingHits = new RaycastHit2D[5];

    // Grounded property
    [SerializeField]
    private bool _isGrounded;
    public bool IsGrounded
    {
        get { return _isGrounded; }
        private set
        {
            _isGrounded = value;
            animator.SetBool(AnimationStrings.IsGrounded, value);
        }
    }

    // Wall properties
    [SerializeField]
    private bool _isOnWall;
    public bool IsOnWall => _isOnWall;

    [SerializeField]
    private bool _isOnRightWall;
    public bool IsOnRightWall => _isOnRightWall;

    [SerializeField]
    private bool _isOnLeftWall;
    public bool IsOnLeftWall => _isOnLeftWall;

    // Ceiling property
    [SerializeField]
    private bool _isOnCeiling;
    public bool IsOnCeiling
    {
        get { return _isOnCeiling; }
        private set
        {
            _isOnCeiling = value;
            animator.SetBool(AnimationStrings.isOnCeiling, value);
        }
    }

    private void Awake()
    {
        touchingCol = GetComponent<CapsuleCollider2D>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // TEMPORARY FIX - Force grounded for testing


        // Original code (commented out for testing)
        IsGrounded = touchingCol.Cast(Vector2.down, castFilter, groundHits, groundDistance) > 0;

        // Check walls separately for right and left - using more precise detection
        _isOnRightWall = touchingCol.Cast(rightWallCheckDirection, castFilter, rightWallHits, wallDistance) > 0;
        _isOnLeftWall = touchingCol.Cast(leftWallCheckDirection, castFilter, leftWallHits, wallDistance) > 0;

        // Set overall wall state
        bool wasOnWall = _isOnWall;
        _isOnWall = _isOnRightWall || _isOnLeftWall;

        // Extra check - if player is pushing into wall, make sure we detect it
        if (_isOnWall && rb.linearVelocity.x != 0)
        {
            // If velocity is pushing into wall, ensure the wall state is correctly set
            if ((rb.linearVelocity.x > 0 && _isOnRightWall) ||
                (rb.linearVelocity.x < 0 && _isOnLeftWall))
            {
                _isOnWall = true;
            }
        }

        animator.SetBool(AnimationStrings.isOnWall, _isOnWall);

        // Check if on ceiling
        IsOnCeiling = touchingCol.Cast(Vector2.up, castFilter, ceilingHits, ceilingDistance) > 0;
    }

    private void OnDrawGizmos()
    {
        if (touchingCol == null) return;
    }
}