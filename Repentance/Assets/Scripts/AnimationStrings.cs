using UnityEngine;

internal static class AnimationStrings
{
    // Movement-related animation parameters
    public const string IsMoving = "isMoving";
    public const string IsRunning = "isRunning"; // Keep for backward compatibility
    public const string IsDashing = "isDashing"; // Add new parameter
    public const string IsGrounded = "isGrounded";
    public const string yVelocity = "yVelocity";
    public const string jump = "jump";
    public const string isOnWall = "isOnWall";
    public const string isOnCeiling = "isOnCeiling";
    public const string attack = "attack";
    public const string canMove = "canMove";
    public const string hasTarget = "hasTarget";
    public const string isAlive = "isAlive";
}