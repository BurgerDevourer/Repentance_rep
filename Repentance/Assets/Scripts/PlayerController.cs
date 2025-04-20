using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections))]
public class PlayerController : MonoBehaviour
{
    // Replace runSpeed with dash parameters
    public float walkSpeed = 1f;
    public float dashPower = 10f;      // How powerful the dash is
    public float dashDuration = 0.4f;  // How long the dash lasts
    public float dashCooldown = 1f;  // How long before you can dash again
    public float airWalkSpeed = 3f;
    public float jumpImpulse = 10f;
    public float fallMultiplier = 2.5f;
    
    private bool canDash = true;
    private bool isDashing = false;
    
    Vector2 moveInput;
    TouchingDirections touchingDirections;

    public float CurrentMoveSpeed
    {
        get
        {
            if (CanMove)
            {
                if (IsMoving && !touchingDirections.IsOnWall)
                {
                    if (touchingDirections.IsGrounded)
                    {
                        return walkSpeed;
                    }
                    else
                    {
                        return airWalkSpeed;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
    }

    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove) && IsAlive && !isDashing;
        }
    }

    [SerializeField]
    private bool _isMoving = false;

    public bool IsMoving
    {
        get { return _isMoving; }
        private set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.IsMoving, value);
        }
    }

    // Remove IsRunning property and replace with IsDashing
    [SerializeField]
    private bool _isDashing = false;

    public bool IsDashing
    {
        get { return _isDashing; }
        private set
        {
            _isDashing = value;
            // You might want to create a new animation parameter for dashing
            animator.SetBool("isDashing", value);
        }
    }

    public bool _isFacingRight = true;

    public bool IsFacingRight
    {
        get { return _isFacingRight; }
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

    Rigidbody2D rb;
    Animator animator;
    private bool wasGrounded = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        touchingDirections = GetComponent<TouchingDirections>();
    }

    private void FixedUpdate()
    {
        // Check for landing
        bool isGrounded = touchingDirections.IsGrounded;
        if (isGrounded && !wasGrounded)
        {
            // Just landed - ensure we can move
            animator.SetBool(AnimationStrings.canMove, true);
        }
        wasGrounded = isGrounded;
        
        if (isDashing)
        {
            // During dash, don't apply normal movement
            return;
        }
        
        // Wall collision handling - improved version
        if (touchingDirections.IsOnWall)
        {
            // Check if we're trying to move into the wall
            bool movingIntoRightWall = moveInput.x > 0 && touchingDirections.IsOnRightWall;
            bool movingIntoLeftWall = moveInput.x < 0 && touchingDirections.IsOnLeftWall;
            
            if (movingIntoRightWall || movingIntoLeftWall) 
            {
                // Stop horizontal movement completely and apply small push-off
                float pushDirection = touchingDirections.IsOnRightWall ? -0.1f : 0.1f;
                rb.linearVelocity = new Vector2(pushDirection, rb.linearVelocity.y);
                
                // Allow vertical movement to continue normally
                return;
            }
        }

        // Normal movement code - only executes if we're not stuck on a wall
        rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
    }

    // Update OnMove to ensure facing direction is properly set after dash
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        
        // Prevent trying to move into walls
        if ((moveInput.x > 0 && touchingDirections.IsOnRightWall) || 
            (moveInput.x < 0 && touchingDirections.IsOnLeftWall))
        {
            // Allow vertical input but zero out horizontal input
            moveInput.x = 0;
        }
        
        if(IsAlive)
        {
            IsMoving = moveInput != Vector2.zero;
            SetFacingDirection(moveInput);
        } 
        else 
        {
            IsMoving = false;
        }
    }

    // Update SetFacingDirection to handle post-dash direction properly
    private void SetFacingDirection(Vector2 moveInput)
    {
        if (moveInput.x != 0)
        {
            if (moveInput.x > 0 && !IsFacingRight)
            {
                IsFacingRight = true;
            }
            else if (moveInput.x < 0 && IsFacingRight)
            {
                IsFacingRight = false;
            }
        }
    }

    // Replace OnRun with OnDash
    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && canDash && IsAlive && !touchingDirections.IsOnWall)
        {
            StartCoroutine(Dash());
        }
    }
    
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        IsDashing = true;
        
        // Store initial gravity value
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        
        // Store the current facing direction
        bool wasFacingRight = IsFacingRight;
        
        // Determine dash direction
        Vector2 dashDirection;
        if (moveInput != Vector2.zero)
        {
            // Dash in movement direction
            dashDirection = moveInput.normalized;
            // Update facing direction based on dash direction
            if (moveInput.x > 0 && !IsFacingRight)
            {
                IsFacingRight = true;
            }
            else if (moveInput.x < 0 && IsFacingRight)
            {
                IsFacingRight = false;
            }
        }
        else
        {
            // Dash in facing direction if no movement input
            dashDirection = new Vector2(IsFacingRight ? 1 : -1, 0);
        }
        
        // Apply dash force
        rb.linearVelocity = dashDirection * dashPower;
        
        // Disable movement control during dash
        animator.SetBool(AnimationStrings.canMove, false);
        
        // Use a time-based approach rather than yield waiting
        float dashTimeLeft = dashDuration;
        bool hitWall = false;
        
        // Continue dashing until duration expires or hit wall
        while (dashTimeLeft > 0 && !hitWall)
        {
            // Check if we've hit a wall
            if ((dashDirection.x > 0 && touchingDirections.IsOnRightWall) ||
                (dashDirection.x < 0 && touchingDirections.IsOnLeftWall))
            {
                hitWall = true;
                
                // More powerful bounce effect with upward component
                float bounceX = -dashDirection.x * dashPower * 0.6f;
                float bounceY = 2.0f; // Stronger upward boost to help unstick
                
                rb.linearVelocity = new Vector2(bounceX, bounceY);
                
                // Important: Cancel dash state immediately
                break;
            }
            
            dashTimeLeft -= Time.deltaTime;
            yield return null;
        }
        
        // End dash immediately if we hit a wall
        if (hitWall)
        {
            // End dash with minimal delay
            yield return new WaitForSeconds(0.05f);
        }
        else
        {
            // Regular dash ending
            yield return new WaitForSeconds(0.1f);
        }
        
        // Restore gravity and end dash state
        rb.gravityScale = originalGravity;
        isDashing = false;
        IsDashing = false;
        
        // Make sure we're not still stuck to the wall - more powerful unsticking
        if (touchingDirections.IsOnWall)
        {
            // More powerful push to unstick
            float pushDirection = touchingDirections.IsOnRightWall ? -2f : 2f;
            rb.linearVelocity = new Vector2(pushDirection, 1f);
        }
        
        // Re-enable movement immediately if we hit a wall
        if (hitWall)
        {
            animator.SetBool(AnimationStrings.canMove, true);
        }
        else
        {
            // Regular movement re-enabling with delay
            yield return new WaitForSeconds(0.1f);
            animator.SetBool(AnimationStrings.canMove, true);
        }
        
        // Force a direction check after dash to ensure consistency
        if (moveInput != Vector2.zero)
        {
            SetFacingDirection(moveInput);
        }
        
        // Wait for cooldown
        yield return new WaitForSeconds(dashCooldown);
        
        canDash = true;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && touchingDirections.IsGrounded && CanMove)
        {
            animator.SetTrigger(AnimationStrings.jump);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpImpulse);
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && IsAlive)
        {
            // Only trigger attack on button press, not release
            animator.SetTrigger(AnimationStrings.attack);
            
            // Start a coroutine to ensure we can move again after animation
            StartCoroutine(ResetCanMoveAfterDelay(0.5f)); // Adjust time to match your animation length
        }
    }

    private IEnumerator ResetCanMoveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Force reset canMove to true after animation should be complete
        animator.SetBool(AnimationStrings.canMove, true);
        
        // If we're against a wall, give a small push away from it
        if (touchingDirections.IsOnWall)
        {
            // Push slightly away from wall
            float pushDirection = touchingDirections.IsOnRightWall ? -0.1f : 0.1f;
            rb.linearVelocity = new Vector2(pushDirection, rb.linearVelocity.y);
        }
    }

    public bool IsAlive {
        get
        {
            return animator.GetBool(AnimationStrings.isAlive);
        }
    }
}
