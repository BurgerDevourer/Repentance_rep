using UnityEngine;

public static class AnimationStrings
{
    // Animation Parameters - Common
    public static readonly string IsGrounded = "isGrounded";
    public static readonly string isOnWall = "isOnWall";
    public static readonly string isOnCeiling = "isOnCeiling";
    public static readonly string canMove = "canMove";
    public static readonly string isAlive = "isAlive";
    public static readonly string isHit = "isHit";
    public static readonly string isMoving = "isMoving";
    public static readonly string yVelocity = "yVelocity";
    public static readonly string xVelocity = "xVelocity"; // Add this line
    public static readonly string hasTarget = "hasTarget";
    
    // Animation Triggers
    public static readonly string attack = "attack";
    public static readonly string jump = "jump";
    public static readonly string hurt = "hurt";
    public static readonly string death = "death";
    
    // Animation States
    public static readonly string IsMoving = "isMoving";
}