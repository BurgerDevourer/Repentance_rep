using UnityEngine;

public class Player_Movement : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    private Rigidbody2D rb;
    [SerializeField] private float walkSpeed = 20;
    private float xAxis;

    [Header("Ground Check Settings")]
    [SerializeField] private float jumpForce = 10;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Obstacle settings")]
    [SerializeField] private LayerMask whatIsObstacle;
    [SerializeField] private Transform whatTouchingObstacle;
    [SerializeField] private float obstacleCheckX = 1f;
    [SerializeField] private float obstacleCheckY = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        Move();
        Jump();
        TouchingObstacle();
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
        if(Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
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
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.7f);
        }        
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow)) && Grounded())
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    public bool TouchingObstacle()
    {
        Debug.DrawRay(whatTouchingObstacle.position, Vector2.right * obstacleCheckX, Color.red);
        Debug.DrawRay(whatTouchingObstacle.position, Vector2.left * obstacleCheckX, Color.red);
        Debug.DrawRay(whatTouchingObstacle.position, Vector2.up * obstacleCheckY, Color.red);
        Debug.DrawRay(whatTouchingObstacle.position, Vector2.down * obstacleCheckY, Color.red);

        if (Physics2D.Raycast(whatTouchingObstacle.position, Vector2.right, obstacleCheckX, whatIsObstacle)
        || Physics2D.Raycast(whatTouchingObstacle.position, Vector2.left, obstacleCheckX, whatIsObstacle)
        || Physics2D.Raycast(whatTouchingObstacle.position, Vector2.up, obstacleCheckY, whatIsObstacle)
        || Physics2D.Raycast(whatTouchingObstacle.position, Vector2.down, obstacleCheckY, whatIsObstacle))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}