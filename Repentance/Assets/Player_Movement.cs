using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    // MOVEMENT SETTINGS
    [Header("Horizontal Movement Settings")]
    private Rigidbody2D rb;
    [SerializeField] private float walkSpeed = 20;
    private float xAxis;

    // JUMPING and GROUND CHECK
    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 9;
    [SerializeField] private float shortJumpMultiplier = 0.6f; // multiplier for short jump
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f; // might be width of the player
    [SerializeField] private LayerMask whatIsGround;
    private bool hasJumped = false;

    // COYTOTE TIME
    [SerializeField] private float coyoteTime = 0.1f;
    private float coyoteTimeCounter;
    //coyote time restriction
    [SerializeField] private float groundedBufferTime = 0.2f;
    private float groundedBufferCounter = 0f;

    // IDLE ANIMATION
    [SerializeField] private float idleTime = 30;
    private float idleCounter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //COYOTE TIME BUFFER
        if(groundedBufferCounter > 0)
        {
            groundedBufferCounter -= Time.deltaTime;
        }
        //COYOTE TIME ( can jump a little after falling off a platform )
        if (Grounded() && groundedBufferCounter <=0)
        {
            coyoteTimeCounter = coyoteTime;
            hasJumped = false;
        }
        else if (!Grounded() && rb.linearVelocity.y < 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        //IDLE ANIMATION
        if (Grounded() && rb.linearVelocity.y == 0 && rb.linearVelocity.x == 0)
        {
            idleCounter += Time.deltaTime;
            if (idleCounter >= idleTime)
            {
                // Trigger idle animation
                // animator.SetTrigger("Idle");
            }
        }
        else
        {
            idleCounter = 0;
        }
        
        // MOVEMENT
        GetInputs();
        Move();
        Jump();
    }

    void GetInputs()
    {
        xAxis = Input.GetAxisRaw("Horizontal");
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(xAxis * walkSpeed, rb.linearVelocity.y);
    }

    public bool Grounded()
    {
        if (groundedBufferCounter <= 0 &&
           Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
        ){
            return true;
        }
        else
        {
            return false;
        }
    }


    void Jump()
    {
        if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.UpArrow)) && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortJumpMultiplier);
        }        
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) && coyoteTimeCounter > 0 && !hasJumped)
        {   
            hasJumped = true;
            groundedBufferCounter = groundedBufferTime;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }
}