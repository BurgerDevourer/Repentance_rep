using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 1f;
    public float dashPower = 10f;
    public float dashDuration = 0.4f;
    public float dashCooldown = 1f;
    public float airWalkSpeed = 3f;
    public float jumpImpulse = 10f;

    Vector2 moveInput;
    TouchingDirections touchingDirections;
    Rigidbody2D rb;
    Animator animator;
    private bool canDash = true;
    private bool isDashing = false; // This remains a bool for dash state logic
    private bool wasGrounded = false;

    public bool IsAlive => animator.GetBool(AnimationStrings.isAlive);

    public float CurrentMoveSpeed =>
        CanMove && Mathf.Abs(IsMoving) > 0.01f
            ? (touchingDirections.IsGrounded ? walkSpeed : airWalkSpeed) // Changed to IsGrounded
            : 0;

    public bool CanMove => animator.GetBool(AnimationStrings.canMove) && IsAlive && !isDashing;

    [SerializeField] private float _isMoving = 0f; // Changed from bool to float
    public float IsMoving // Changed from bool to float
    {
        get => _isMoving;
        private set
        {
            _isMoving = value;
            // Assuming AnimationStrings.IsMoving is the name of your float parameter in the Animator
            // We usually pass the absolute value for speed/blend tree control
            animator.SetFloat(AnimationStrings.IsMoving, Mathf.Abs(value));
        }
    }

    [SerializeField] private bool _isDashing = false; // This is for the dash state, distinct from general movement
    public bool IsActualDashing // Renamed to avoid confusion if you had a bool IsDashing animator param
    {
        get => _isDashing;
        private set
        {
            _isDashing = value;
            // If you have a separate "isDashing" bool parameter in animator for dash animation
            animator.SetBool("isDashing", value);
        }
    }

    public bool _isFacingRight = true;
    public bool IsFacingRight
    {
        get => _isFacingRight;
        private set
        {
            if (_isFacingRight != value)
            {
                _isFacingRight = value;
                transform.localScale = new Vector3(
                    Mathf.Abs(transform.localScale.x) * (_isFacingRight ? 1 : -1),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }
        }
    }

    // Add this field at the top with your other variables
    [SerializeField] private Attack attackComponent;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
        
        // Find the attack component
        attackComponent = GetComponentInChildren<Attack>();
        
        animator.SetBool(AnimationStrings.canMove, true);
        animator.SetBool(AnimationStrings.isAlive, true); // Initialize isAlive
    }

    private void FixedUpdate()
    {
        bool isGrounded = touchingDirections.IsGrounded; // Changed to IsGrounded
        if (isGrounded && !wasGrounded)
        {
            // Landed
            animator.SetBool(AnimationStrings.canMove, true);
            // Small bounce to help unstick
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.1f);
        }
        wasGrounded = isGrounded;

        if (IsActualDashing) return; // Use the renamed property for dash state

        // Wall interaction logic
        if (touchingDirections.IsOnWall && !isGrounded) // More specific wall slide/interaction condition
        {
            bool movingIntoRightWall = moveInput.x > 0 && touchingDirections.IsOnRightWall;
            bool movingIntoLeftWall = moveInput.x < 0 && touchingDirections.IsOnLeftWall;
            if (movingIntoRightWall || movingIntoLeftWall)
            {
                // Prevent pushing into wall, allow slight slide down or hold
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y * 0.5f); // Reduce vertical speed for wall slide
                return;
            }
        }

        rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
    }

    private void Update()
    {
        // Emergency unsticking / state correction
        if (touchingDirections.IsOnWall && !CanMove && !IsActualDashing)
            animator.SetBool(AnimationStrings.canMove, true);

        if (touchingDirections.IsGrounded && !CanMove && !IsActualDashing) // Changed to IsGrounded
            animator.SetBool(AnimationStrings.canMove, true);

        // Ground stick prevention
        if (touchingDirections.IsGrounded && moveInput.x != 0 && Mathf.Abs(rb.linearVelocity.x) < 0.1f && CanMove) // Changed to IsGrounded
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0.1f); // Small bounce

        // Prevent moving into wall if not dashing
        if (!IsActualDashing && touchingDirections.IsOnWall)
        {
            if ((touchingDirections.IsOnRightWall && moveInput.x > 0) ||
                (touchingDirections.IsOnLeftWall && moveInput.x < 0))
                moveInput.x = 0; // Stop input if trying to move into a wall
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        // Prevent moving into wall directly from input
        if ((moveInput.x > 0 && touchingDirections.IsOnRightWall && !touchingDirections.IsGrounded) || // Changed to IsGrounded
            (moveInput.x < 0 && touchingDirections.IsOnLeftWall && !touchingDirections.IsGrounded)) // Changed to IsGrounded
        {
            // Allow some control if grounded, but restrict if airborne and against a wall
            // This check might need refinement based on desired wall interaction feel
        }


        if (IsAlive)
        {
            // Set the IsMoving float property based on horizontal input
            IsMoving = moveInput.x;
            SetFacingDirection(moveInput);
        }
        else
        {
            IsMoving = 0f;
        }
    }

    private void SetFacingDirection(Vector2 currentMoveInput)
    {
        if (currentMoveInput.x != 0) // Use the passed moveInput
        {
            if (currentMoveInput.x > 0 && !IsFacingRight)
                IsFacingRight = true;
            else if (currentMoveInput.x < 0 && IsFacingRight)
                IsFacingRight = false;
        }
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && canDash && IsAlive && !touchingDirections.IsOnWall) // Don't dash if already on a wall
            StartCoroutine(PerformDash()); // Renamed coroutine
    }

    private IEnumerator PerformDash() // Renamed
    {
        canDash = false;
        isDashing = true; // Internal state
        IsActualDashing = true; // Property for animator and external checks
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;

        // Dash direction should prioritize current input, fallback to facing direction
        Vector2 dashDirection;
        if (moveInput.sqrMagnitude > 0.01f) // If there's active input
        {
            dashDirection = moveInput.normalized;
        }
        else // If no input, dash in facing direction (horizontally)
        {
            dashDirection = new Vector2(IsFacingRight ? 1 : -1, 0);
        }


        rb.linearVelocity = dashDirection * dashPower;
        animator.SetBool(AnimationStrings.canMove, false); // Can't control movement during dash

        float dashTimeLeft = dashDuration;
        bool hitWallDuringDash = false;

        while (dashTimeLeft > 0 && !hitWallDuringDash)
        {
            // More robust wall check during dash
            if ((dashDirection.x > 0 && touchingDirections.IsOnRightWall) ||
                (dashDirection.x < 0 && touchingDirections.IsOnLeftWall) ||
                (dashDirection.y > 0 && touchingDirections.IsOnCeiling) || // Check ceiling
                (dashDirection.y < 0 && touchingDirections.IsGrounded && dashTimeLeft < dashDuration * 0.5f)) // Check ground if dashing downwards
            {
                hitWallDuringDash = true;
                // Apply a small bounce off the wall/obstacle
                rb.linearVelocity = -dashDirection * dashPower * 0.3f; // Bounce back slightly
                break;
            }
            dashTimeLeft -= Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = originalGravity;
        isDashing = false;
        IsActualDashing = false;

        animator.SetBool(AnimationStrings.canMove, true);
        // Reset moveInput to prevent continued movement in dash direction if input was released
        // moveInput = Vector2.zero; // Consider if this is desired behavior

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && CanMove)
        {
            if (touchingDirections.IsGrounded) // Changed to IsGrounded
            {
                animator.SetTrigger(AnimationStrings.jump);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpImpulse);
            }
            else if (touchingDirections.IsOnWall) // Wall Jump
            {
                animator.SetTrigger(AnimationStrings.jump);
                // Push away from wall and upwards
                float pushDirection = touchingDirections.IsOnRightWall ? -1f : 1f;
                rb.linearVelocity = new Vector2(pushDirection * airWalkSpeed * 1.2f, jumpImpulse * 0.8f);
                IsFacingRight = !touchingDirections.IsOnRightWall; // Turn away from wall
                // animator.SetBool(AnimationStrings.canMove, true); // Already handled by CanMove check
            }
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && IsAlive && CanMove)
        {
            Vector3 preAttackPosition = transform.position;
            
            // Trigger the animation
            animator.SetTrigger(AnimationStrings.attack);
            
            // Execute the attack
            if (attackComponent != null)
            {
                attackComponent.ExecuteAttack();
            }
            else
            {
                Debug.LogError("Attack component not found!");
            }
            
            // Start a coroutine to ensure we don't phase through walls during attack animation
            StartCoroutine(PreventWallPhasingDuringAttack(preAttackPosition));
        }
    }
    
    private IEnumerator PreventWallPhasingDuringAttack(Vector3 preAttackPosition)
    {
        // Wait a small amount of time for the attack animation to start
        yield return new WaitForSeconds(0.05f);
        
        // Monitor position during attack animation
        float attackCheckDuration = 0.5f; // Adjust based on your attack animation length
        float elapsedTime = 0;
        
        while (elapsedTime < attackCheckDuration)
        {
            // If we're in a wall after moving from our pre-attack position
            if (touchingDirections.IsOnWall)
            {
                // Determine which wall we're in
                bool inRightWall = touchingDirections.IsOnRightWall;
                bool inLeftWall = touchingDirections.IsOnLeftWall;
                
                // Adjust position to prevent phasing through
                if (inRightWall)
                {
                    // Push slightly left away from right wall
                    transform.position = new Vector3(transform.position.x - 0.05f, transform.position.y, transform.position.z);
                }
                else if (inLeftWall)
                {
                    // Push slightly right away from left wall
                    transform.position = new Vector3(transform.position.x + 0.05f, transform.position.y, transform.position.z);
                }
                
                // Apply a small velocity to ensure we're not stuck
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}