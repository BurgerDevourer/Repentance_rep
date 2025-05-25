using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    public List<Collider2D> detectedColliders = new List<Collider2D>();
    
    [Tooltip("What layers should this detection zone detect")]
    public LayerMask detectionLayer;
    
    [Tooltip("Should this detection zone detect through walls")]
    public bool detectThroughWalls = true;
    
    // This is the physics layer where your walls are defined
    public LayerMask wallLayer;
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // IMPORTANT: Add this line to see all trigger events regardless of layer
        Debug.Log($"[{gameObject.name}] OnTriggerEnter2D with {collision.name} " +
                  $"on layer {LayerMask.LayerToName(collision.gameObject.layer)}. " +
                  $"Is on detection layer: {((1 << collision.gameObject.layer) & detectionLayer) != 0}");
        
        // Check if the object is on a detection layer
        if (((1 << collision.gameObject.layer) & detectionLayer) != 0)
        {
            // If we're ignoring walls, add the collision directly
            if (detectThroughWalls)
            {
                detectedColliders.Add(collision);
                return;
            }
            
            // Otherwise, check if a wall is blocking line of sight
            Vector2 direction = collision.transform.position - transform.position;
            float distance = direction.magnitude;
            
            // Cast a ray to check for walls between us and the target
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction.normalized,
                distance,
                wallLayer
            );
            
            // If no wall was hit, add the collision
            if (hit.collider == null)
            {
                detectedColliders.Add(collision);
            }
        }
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (detectedColliders.Contains(collision))
        {
            detectedColliders.Remove(collision);
        }
    }
    
    // Add this method to continuously update detection for objects behind walls
    private void Update()
    {
        // Only needed if we're NOT detecting through walls (for debugging)
        if (!detectThroughWalls) return;
        
        // This ensures objects stay detected even if they move behind walls
        // This can be removed if you just want the basic wall-ignoring functionality
    }

    // Add this debug method to the class
    private void Start()
    {
        Debug.Log($"[{gameObject.name}] Detection zone initialized. Layer mask: {LayerMaskToString(detectionLayer)}");
    }
    
    // Add this helper method to visualize the layer mask
    private string LayerMaskToString(LayerMask mask)
    {
        var layers = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                layers += LayerMask.LayerToName(i) + ", ";
            }
        }
        return layers;
    }
    
    // Add this debug method
    private void OnTriggerStay2D(Collider2D collision)
    {
        // Make sure we keep tracking objects that stay in our zone
        if (((1 << collision.gameObject.layer) & detectionLayer) != 0)
        {
            if (!detectedColliders.Contains(collision))
            {
                Debug.Log($"[{gameObject.name}] Added {collision.name} to detected colliders via OnTriggerStay2D");
                detectedColliders.Add(collision);
            }
        }
    }
}
