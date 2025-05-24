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
                foreach (Collider2D groundCollider in Object.FindObjectsByType<Collider2D>(FindObjectsSortMode.None))
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
                Debug.Log($"[{gameObject.name}] FORCED detection zone to detect Player layer");
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
        bool playerInDetectionZone = detectionZone != null && 
                                     detectionZone.detectedColliders.Count > 0 && 
                                     playerTransform != null && 
                                     detectionZone.detectedColliders.Contains(playerTransform.GetComponent<Collider2D>());

        // More detailed debug - log every 30 frames instead of 60
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[{gameObject.name}] Detection: {playerInDetectionZone}, Attack: {IsPlayerInAttackRange}, " +
                      $"State: {currentState}, Player Transform: {playerTransform != null}, " +
                      $"Detection Zone: {detectionZone != null}, Attack Zone: {attackZone != null}, " +
                      $"Colliders in detection: {(detectionZone != null ? detectionZone.detectedColliders.Count : 0)}, " +
                      $"Player Collider: {(playerTransform != null ? playerTransform.GetComponent<Collider2D>() != null : false)}");
            
            // Print the detected colliders for debugging
            if (detectionZone != null && detectionZone.detectedColliders.Count > 0) 
            {
                foreach (var col in detectionZone.detectedColliders)
                {
                    Debug.Log($"[{gameObject.name}] Detected: {col.name} on layer {LayerMask.LayerToName(col.gameObject.layer)}");
                }
            }
        }
        
        switch (currentState)
        {
            case EnemyState.Wander:
                if (playerInDetectionZone)
                {
                    currentState = EnemyState.Chase;
                    HasTarget = true;
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
                // AttackRoutine handles transition back to Chase
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

        float directionToPlayer = playerTransform.position.x - transform.position.x;

        if (Mathf.Abs(directionToPlayer) > 0.1f)
        {
            if (directionToPlayer > 0 && !movingRight) FlipDirection();
            else if (directionToPlayer < 0 && movingRight) FlipDirection();
        }

        // MODIFIED: Only check for walls, not ground ahead, when chasing directly toward player
        if (touchingDirections.IsOnWall && 
            ((movingRight && directionToPlayer > 0) || (!movingRight && directionToPlayer < 0)))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // ADDED: Special ledge check that explicitly ignores the player
        bool ledgeAhead = IsLedgeAheadIgnoringPlayer();
        if (ledgeAhead && 
            ((movingRight && directionToPlayer > 0) || (!movingRight && directionToPlayer < 0)))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }
        
        // Move toward player
        rb.linearVelocity = new Vector2(moveDirection.x * chaseSpeed, rb.linearVelocity.y);
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
        animator.SetBool(AnimationStrings.canMove, false);
        rb.linearVelocity = Vector2.zero;

        if (playerTransform != null)
        {
            bool playerIsToTheRight = playerTransform.position.x > transform.position.x;
            if (playerIsToTheRight != movingRight)
            {
                FlipDirection();
            }
        }

        animator.SetTrigger(AnimationStrings.attack);
        
        // Execute the attack
        if (attackComponent != null)
        {
            Debug.Log($"[{gameObject.name}] Executing attack via AttackComponent");
            attackComponent.ExecuteAttack(0.3f); // Increased attack duration to 0.3 seconds
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Attack component is null!");
        }

        float attackAnimLength = 0.5f;
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.length > 0)
        {
            attackAnimLength = stateInfo.length;
        }
        yield return new WaitForSeconds(attackAnimLength);

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

    private void OnDrawGizmosSelected()
    {
        if (detectionZone != null)
        {
            Gizmos.color = Color.yellow;
            CircleCollider2D detCollider = detectionZone.GetComponent<CircleCollider2D>();
            if (detCollider != null) Gizmos.DrawWireSphere(detectionZone.transform.position + (Vector3)detCollider.offset, detCollider.radius);
        }
        
        if (attackZone != null)
        {
            Gizmos.color = Color.red;
            CircleCollider2D atkCollider = attackZone.GetComponent<CircleCollider2D>();
            if (atkCollider != null) Gizmos.DrawWireSphere(attackZone.transform.position + (Vector3)atkCollider.offset, atkCollider.radius);
        }
        
        // Ledge check Gizmo
        if (Application.isPlaying && rb != null && GetComponent<Collider2D>() != null)
        {
            Vector2 raycastOrigin = (Vector2)transform.position +
                                new Vector2(GetComponent<Collider2D>().offset.x + (moveDirection.x * (GetComponent<Collider2D>().bounds.extents.x + 0.3f)),
                                            GetComponent<Collider2D>().offset.y - GetComponent<Collider2D>().bounds.extents.y + 0.05f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(raycastOrigin, raycastOrigin + Vector2.down * ledgeCheckDistance);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[{gameObject.name}] OnTriggerEnter2D with {collision.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}. " +
              $"Is on detection layer: {((1 << collision.gameObject.layer) & detectionZone.detectionLayer) != 0}");
        
        // Rest of your existing OnTriggerEnter2D code...
    }
}