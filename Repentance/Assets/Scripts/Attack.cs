using System.Collections;
using System.Collections.Generic; // Add this line
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Attack : MonoBehaviour
{
    [Header("Attack Properties")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private bool enableKnockback = true;
    [SerializeField] private float attackCooldown = 0.3f;
    
    [Header("Hit Effects")]
    [SerializeField] private bool showHitEffect = true;
    [SerializeField] private GameObject hitEffectPrefab;
    
    // Private variables
    private Collider2D attackCollider;
    private bool canAttack = true;
    private Transform attacker;
    
    // Cache hit targets to prevent multiple hits in same attack
    private List<GameObject> hitTargets = new List<GameObject>();

    // Add debug flag
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        attackCollider = GetComponent<Collider2D>();
        attacker = transform.parent;
        
        // Force collider to be a trigger
        if (!attackCollider.isTrigger)
        {
            attackCollider.isTrigger = true;
            Debug.Log("Attack collider set to trigger mode");
        }
        
        // Initially disable the attack collider
        attackCollider.enabled = false;
    }
    
    // Call this method to activate the attack
    public void ExecuteAttack(float duration = 0.2f)
    {
        if (!canAttack) return;
        
        StartCoroutine(AttackRoutine(duration));
    }
    
    // Modify the attack routine
    private IEnumerator AttackRoutine(float duration)
    {
        canAttack = false;
        hitTargets.Clear();
        
        // Enable attack hitbox
        attackCollider.enabled = true;
        
        if (debugMode)
            Debug.Log($"[{gameObject.name}] Attack collider enabled. Position: {transform.position}, " +
                     $"Parent: {transform.parent?.name}, Collider enabled: {attackCollider.enabled}");
        
        // Keep attack active for duration
        yield return new WaitForSeconds(duration);
        
        // Disable attack hitbox
        attackCollider.enabled = false;
        
        if (debugMode)
            Debug.Log($"[{gameObject.name}] Attack finished. Hit targets: {hitTargets.Count}");
        
        // Apply cooldown
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    // Modify trigger enter
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (debugMode)
            Debug.Log($"[{gameObject.name}] Trigger detected with: {collision.name} on layer {LayerMask.LayerToName(collision.gameObject.layer)}");
            
        // REMOVE THE collision.isTrigger CHECK - this is the key fix!
        // Only ignore parent objects and already hit targets
        if ((attacker != null && collision.transform.IsChildOf(attacker)) ||
            hitTargets.Contains(collision.gameObject))
        {
            if (debugMode)
                Debug.Log($"[{gameObject.name}] Ignoring collision: " +
                         $"isChild={attacker != null && collision.transform.IsChildOf(attacker)}, " +
                         $"alreadyHit={hitTargets.Contains(collision.gameObject)}");
            return;
        }
        
        // Add to hit targets to prevent multiple hits
        hitTargets.Add(collision.gameObject);
        
        // Try to apply damage
        Damageable damageable = collision.GetComponent<Damageable>();
        
        // CRITICAL: Also try getting Damageable from parent if not found
        if (damageable == null)
            damageable = collision.GetComponentInParent<Damageable>();
            
        if (damageable != null)
        {
            Debug.Log($"[{gameObject.name}] HIT SUCCESS! Damaging {collision.name} for {attackDamage}");
            damageable.Hit(attackDamage);
            
            // Apply knockback if enabled
            if (enableKnockback)
            {
                ApplySafeKnockback(collision);
            }
            
            // Show hit effect if enabled
            if (showHitEffect && hitEffectPrefab != null)
            {
                ShowHitEffect(collision.bounds.center);
            }
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] No Damageable component found on {collision.name} or its parents");
        }
    }
    
    private void ApplySafeKnockback(Collider2D target)
    {
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb == null) return;
        
        // Calculate direction from attacker to target
        Vector2 knockbackDirection = Vector2.zero;
        
        if (attacker != null)
        {
            knockbackDirection = (target.transform.position - attacker.position).normalized;
        }
        else
        {
            // If no attacker reference, use right/left based on local scale
            knockbackDirection = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;
        }
        
        // Apply horizontal knockback while preserving vertical velocity
        float currentYVelocity = targetRb.linearVelocity.y;
        targetRb.linearVelocity = new Vector2(knockbackDirection.x * knockbackForce, currentYVelocity);
        
        // Optional: Add a small upward boost to prevent ground clipping
        targetRb.AddForce(Vector2.up * 2f, ForceMode2D.Impulse);
    }
    
    private void ShowHitEffect(Vector3 position)
    {
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effect, 1f); // Auto-destroy after 1 second
        }
    }
}
