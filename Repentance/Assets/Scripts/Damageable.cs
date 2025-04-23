using NUnit.Framework;
using UnityEngine;

public class Damageable : MonoBehaviour
{

    Animator animator;
    [SerializeField]
    private int _maxHealth;
    public int MaxHealth 
    {
        get
        {
            return _maxHealth;
        }
        set
        {
            _maxHealth = value;
        }
    }

    [SerializeField]
    private int _health = 100;

    public int Health
    {
        get{
            return _health;
        }
        set
        {
            _health = value;

            if(_health <= 0)
            {
                IsAlive = false;
            }
        }
    }

    [SerializeField]
    private bool _isAlive = true;
    private bool isInvincible = false;
    private float timeSinceHit = 0;
    public float invincibilityTime = 0.25f;

    public bool IsAlive 
    { 
        get
        {
            return _isAlive;
        }
        set
        {
           _isAlive = value;
            animator.SetBool(AnimationStrings.isAlive, value);
        }
      }

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if(isInvincible)
        {
            if(timeSinceHit > invincibilityTime)
            {
                isInvincible = false;
                timeSinceHit = 0;
            }

            timeSinceHit +=Time.deltaTime;
        }
    }

    public void Hit(int damage) 
    {
        if(IsAlive && !isInvincible)
        {
            Health -= damage;
            isInvincible = true;

            animator.SetTrigger(AnimationStrings.hurt);
        }
    }

}
