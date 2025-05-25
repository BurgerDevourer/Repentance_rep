using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FollowPlayer : MonoBehaviour
{
    void Update()
    {
        FollowPlayerPosition();
    }
    private void FollowPlayerPosition()
{
    // Find the player object in the scene
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    
    if (player != null)
    {
        // Set the position of this object to the player's position
        transform.position = player.transform.position;
    }
    else
    {
        Debug.LogWarning("Player object not found. Make sure it has the 'Player' tag.");
    }
}
}

