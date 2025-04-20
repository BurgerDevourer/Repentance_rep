using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 1f;
    public float runSpeed = 2.5f;
    public float airWalkSpeed = 3f;
    public float jumpImpulse = 10f;
    public float fallMultiplier = 2.5f;  // Makes falling faster

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
                        if (IsRunning)
                        {
                            return runSpeed;
                        }
                        else
                        {
                            return walkSpeed;
                        }
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

    // Check if the player can move in the current direction
    public bool CanMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
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

    [SerializeField]
    private bool _isRunning = false;

    public bool IsRunning
    {
        get { return _isRunning; }
        set
        {
            _isRunning = value;
            animator.SetBool(AnimationStrings.IsRunning, value);
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

    // Add these fields
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
        
        // Wall collision handling
        if (touchingDirections.IsOnWall && moveInput.x != 0)
        {
            // If moving right and hit right wall OR moving left and hit left wall
            if ((moveInput.x > 0 && touchingDirections.IsOnRightWall) || 
                (moveInput.x < 0 && touchingDirections.IsOnLeftWall))
            {
                // Push slightly away from wall to prevent sticking
                float pushDirection = touchingDirections.IsOnRightWall ? -0.05f : 0.05f;
                rb.linearVelocity = new Vector2(pushDirection, rb.linearVelocity.y);
                return; // Skip normal movement
            }
        }

        // Normal movement code
        rb.linearVelocity = new Vector2(moveInput.x * CurrentMoveSpeed, rb.linearVelocity.y);
        animator.SetFloat(AnimationStrings.yVelocity, rb.linearVelocity.y);
    }

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
        } else 
        {
            IsMoving = false;
        }
    }

    private void SetFacingDirection(Vector2 moveInput)
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

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            IsRunning = true;
        }
        else if (context.canceled)
        {
            IsRunning = false;
        }
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
        if (context.started)
        {
            // Only trigger attack on button press, not release
            animator.SetTrigger(AnimationStrings.attack);
            
            // Start a coroutine to ensure we can move again after animation
            StartCoroutine(ResetCanMoveAfterDelay(0.5f)); // Adjust time to match your animation length
        }
    }

    // Add this method for debugging
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
