using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(TouchingDirections), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float chaseSpeed = 3.5f;
    
    [Header("Wander Behavior")]
    public float directionChangeTime = 3f;  // Time between random direction changes when idle
    private float flipCooldown = 0.5f; // Cooldown to prevent rapid flipping at ledges/walls
    private float lastFlipTime = 0f;

    [Header("Detection")]
    public DetectionZone detectionZone;
    public DetectionZone attackZone;
    public float losePlayerDistance = 10f;

    [Header("Attack")]
    public float attackCooldown = 2f;
    private bool canAttack = true;

    [Header("Ledge Detection")]
    [SerializeField] private float ledgeCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Chase Behavior")]
    public float minDistanceFromPlayer = 1.2f; // Distance to maintain from player
    public float backupSpeed = 1.5f;           // Speed to move away if too close

    // Components
    private Rigidbody2D rb;
    private TouchingDirections touchingDirections;
    private Animator animator;
    private Transform playerTransform;
    [SerializeField] private Attack attackComponent; // Add this field

    // State tracking
    private enum EnemyState { Wander, Chase, Attack }
    private EnemyState currentState = EnemyState.Wander;
    private bool movingRight = true;
    private Vector2 moveDirection = Vector2.right;
    private Vector3 initialScale;

    public bool HasTarget { get; private set; }
    public bool IsPlayerInAttackRange => attackZone != null && attackZone.detectedColliders.Count > 0 && playerTransform != null && attackZone.detectedColliders.Contains(playerTransform.GetComponent<Collider2D>());

    private bool CanMove => animator != null && animator.GetBool(AnimationStrings.canMove);

    // Add this at the class level with other private variables
    private bool isAttacking = false; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        touchingDirections = GetComponent<TouchingDirections>();
        animator = GetComponent<Animator>();
        initialScale = transform.localScale;

        if (GameObject.FindGameObjectWithTag("Player") != null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }
        
        // Randomly choose initial direction
        movingRight = Random.value > 0.5f;
        UpdateFacingDirection();
        moveDirection = movingRight ? Vector2.right : Vector2.left;

        // Find the attack component
        attackComponent = GetComponentInChildren<Attack>();
        
        // FIXED VERSION - Only make the detection zone itself ignore walls
        if (detectionZone != null)
        {
            Collider2D detectionCollider = detectionZone.GetComponent<Collider2D>();
            if (detectionCollider != null)
            {
                // CRITICAL: Only ignore collisions for the detection zone GameObject specifically
                // NOT for the entire layer!
                int groundLayer = LayerMask.NameToLayer("Ground");
                
                // This ignores collisions between ONLY these two specific objects
                foreach (Collider2D groundCollider in GameObject.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
                {
                    if (groundCollider.gameObject.layer == groundLayer)
                    {
                        Physics2D.IgnoreCollision(detectionCollider, groundCollider, true);
                    }
                }
            }
        }

        // Add this code after your existing detection zone setup
        if (detectionZone != null)
        {
            // Force the detection layer to include the Player layer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                detectionZone.detectionLayer |= (1 << playerLayer);
            }
        }
    }

    private void Update()
    {
        HandleDetectionAndState();
        UpdateAnimatorParameters();
    }

    private void HandleDetectionAndState()
    {
        // Don't change states if we're in the middle of an attack
        if (isAttacking)
            return;
        
        // Add this direct detection backup
        bool directPlayerDetection = playerTransform != null && 
                            Vector2.Distance(transform.position, playerTransform.position) < 3f;
    
        bool playerInDetectionZone = (detectionZone != null && 
                                 detectionZone.detectedColliders.Count > 0 && 
                                 playerTransform != null && 
                                 detectionZone.detectedColliders.Contains(playerTransform.GetComponent<Collider2D>())) ||
                                 directPlayerDetection;  // Fallback direct detection
    
        // Every second, log detection state for debugging
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[{gameObject.name}] State: {currentState}, " +
                      $"Player detected: {playerInDetectionZone}, " +
                      $"Attack range: {IsPlayerInAttackRange}, " +
                      $"Can attack: {canAttack}");
        }
        
        switch (currentState)
        {
            case EnemyState.Wander:
                if (playerInDetectionZone)
                {
                    currentState = EnemyState.Chase;
                    HasTarget = true;
                    Debug.Log($"[{gameObject.name}] Detected player - switching to Chase!");
                }
                break;

            case EnemyState.Chase:
                if (playerTransform == null || !playerInDetectionZone || Vector2.Distance(transform.position, playerTransform.position) > losePlayerDistance)
                {
                    currentState = EnemyState.Wander;
                    HasTarget = false;
                }
                else if (IsPlayerInAttackRange && canAttack)
                {
                    currentState = EnemyState.Attack;
                    StartCoroutine(AttackRoutine());
                }
                break;

            case EnemyState.Attack:
                // Don't make any state transitions here
                // Let AttackRoutine handle returning to Chase state when done
                break;
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null) return;
        animator.SetFloat(AnimationStrings.xVelocity, Mathf.Abs(rb.linearVelocity.x)); // Changed to rb.velocity
        animator.SetBool(AnimationStrings.IsGrounded, touchingDirections.IsGrounded);
        animator.SetBool(AnimationStrings.hasTarget, HasTarget);
    }

    private void FixedUpdate()
    {
        if (!CanMove && currentState != EnemyState.Attack)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Changed to rb.velocity
            return;
        }

        switch (currentState)
        {
            case EnemyState.Wander:
                WanderBehavior();
                break;
            case EnemyState.Chase:
                ChaseBehavior();
                break;
            case EnemyState.Attack:
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Changed to rb.velocity
                break;
        }
    }

    private void WanderBehavior()
    {
        // WALL DETECTION: Keep this - immediate response when hitting a wall
        if (touchingDirections.IsOnWall && Time.time > lastFlipTime + flipCooldown)
        {
            FlipDirection();
            lastFlipTime = Time.time;
            return;
        }

        // LEDGE DETECTION: Keep this - immediate response when detecting a ledge
        if (!IsGroundAhead() && Time.time > lastFlipTime + flipCooldown)
        {
            FlipDirection();
            lastFlipTime = Time.time;
            return;
        }

        // Move in current direction
        rb.linearVelocity = new Vector2(moveDirection.x * walkSpeed, rb.linearVelocity.y);
    }

    private void ChaseBehavior()
    {
        if (playerTransform == null)
        {
            return;
        }

        // Calculate direction and distance to player
        float directionToPlayer = playerTransform.position.x - transform.position.x;
        float distanceToPlayer = Mathf.Abs(directionToPlayer);

        // Always face the player
        if (directionToPlayer > 0 && !movingRight) FlipDirection();
        else if (directionToPlayer < 0 && movingRight) FlipDirection();

        // Wall detection check
        if (touchingDirections.IsOnWall && 
            ((movingRight && directionToPlayer > 0) || (!movingRight && directionToPlayer < 0)))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // Ledge detection check
        bool ledgeAhead = IsLedgeAheadIgnoringPlayer();
        if (ledgeAhead && 
            ((movingRight && directionToPlayer > 0) || (!movingRight && directionToPlayer < 0)))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // NEW: Distance management logic
        if (distanceToPlayer < minDistanceFromPlayer)
        {
            // Too close to player - stop or back up slightly
            if (distanceToPlayer < minDistanceFromPlayer * 0.8f)
            {
                // Back up slightly when way too close
                float backupDirection = -Mathf.Sign(directionToPlayer);
                rb.linearVelocity = new Vector2(backupDirection * backupSpeed, rb.linearVelocity.y);
            }
            else
            {
                // Just stop when at good distance
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else
        {
            // Not close enough - move toward player
            rb.linearVelocity = new Vector2(moveDirection.x * chaseSpeed, rb.linearVelocity.y);
        }
    }

    // New method specifically for chase behavior
    private bool IsLedgeAheadIgnoringPlayer()
    {
        Vector2 raycastOrigin = (Vector2)transform.position +
                            new Vector2(GetComponent<Collider2D>().offset.x + (moveDirection.x * (GetComponent<Collider2D>().bounds.extents.x + 0.3f)),
                                        GetComponent<Collider2D>().offset.y - GetComponent<Collider2D>().bounds.extents.y + 0.05f);

        // Explicitly exclude the Player layer
        LayerMask playerLayer = 1 << LayerMask.NameToLayer("Player");
        LayerMask mask = groundLayer & ~playerLayer;
        
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, ledgeCheckDistance, mask);
        Debug.DrawRay(raycastOrigin, Vector2.down * ledgeCheckDistance, hit.collider != null ? Color.blue : Color.red);
        
        return hit.collider == null; // Return true if we DID NOT hit ground (there's a ledge)
    }

    private IEnumerator AttackRoutine()
    {
        Debug.Log($"[{gameObject.name}] Starting attack routine");
        
        canAttack = false;
        isAttacking = true; // Set the flag to true when attack starts
        animator.SetBool(AnimationStrings.canMove, false);
        rb.linearVelocity = Vector2.zero;

        // Store original RigidBody type to restore it later
        RigidbodyType2D originalType = rb.bodyType;
        rb.bodyType = RigidbodyType2D.Kinematic; // Prevent any physics from moving the enemy

        if (playerTransform != null)
        {
            // Face the player before attacking (only change direction, not position)
            bool playerIsToTheRight = playerTransform.position.x > transform.position.x;
            if (playerIsToTheRight != movingRight)
            {
                FlipDirection();
            }
        }

        // Trigger attack animation
        animator.SetTrigger(AnimationStrings.attack);
        
        // Execute the attack
        if (attackComponent != null)
        {
            attackComponent.ExecuteAttack(0.3f); // Increased attack duration to 0.3 seconds
        }

        float attackAnimLength = 0.5f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.length > 0)
        {
            attackAnimLength = stateInfo.length;
        }
        yield return new WaitForSeconds(attackAnimLength);

        // Restore original RigidBody type
        rb.bodyType = originalType;
        
        isAttacking = false; // Reset the flag when attack is complete
        animator.SetBool(AnimationStrings.canMove, true);
        currentState = EnemyState.Chase;

        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void FlipDirection()
    {
        movingRight = !movingRight;
        UpdateFacingDirection();
        moveDirection = movingRight ? Vector2.right : Vector2.left;
    }

    private void UpdateFacingDirection()
    {
        Vector3 newScale = initialScale;
        newScale.x = Mathf.Abs(initialScale.x) * (movingRight ? 1 : -1);
        transform.localScale = newScale;
    }

    private bool IsGroundAhead()
    {
        Vector2 raycastOrigin = (Vector2)transform.position +
                      new Vector2(GetComponent<Collider2D>().offset.x + (moveDirection.x * (GetComponent<Collider2D>().bounds.extents.x + 0.3f)),
                                  GetComponent<Collider2D>().offset.y - GetComponent<Collider2D>().bounds.extents.y + 0.05f);

        // Exclude the player layer
        int playerLayerMask = LayerMask.NameToLayer("Player");
        int playerLayerBit = (1 << playerLayerMask);
        int groundLayerBit = groundLayer.value;
        int layerMask = groundLayerBit & ~playerLayerBit;
        
        RaycastHit2D hit = Physics2D.Raycast(raycastOrigin, Vector2.down, ledgeCheckDistance, layerMask);
        Debug.DrawRay(raycastOrigin, Vector2.down * ledgeCheckDistance, hit.collider != null ? Color.green : Color.red);
        
        return hit.collider != null;
    }

    public void ForceChasePlayer()
    {
        if (playerTransform != null)
        {
            currentState = EnemyState.Chase;
            HasTarget = true;
        }
    }
}