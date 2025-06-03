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
        get
        {
            return _health;
        }
        set
        {
            _health = value;

            if (_health <= 0)
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

    private bool isHit = false;
    [SerializeField] private float hurtStateTime = 0.2f; // How long the hurt state stays active

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

    //Deathscreen
    [SerializeField]
    private GameObject deathScreen;

    public void Update()
    {
        // Handle invincibility timer
        if (isInvincible)
        {
            if (timeSinceHit > invincibilityTime)
            {
                isInvincible = false;
                timeSinceHit = 0;
            }

            timeSinceHit += Time.deltaTime;
        }

        // Handle hurt state timer
        if (isHit)
        {
            if (timeSinceHit > hurtStateTime)
            {
                isHit = false;
                animator.SetBool(AnimationStrings.isHit, false);
                Debug.Log($"[{gameObject.name}] Hurt state ended");
            }
        }

        if (!IsAlive && deathScreen != null)
        {
            deathScreen.SetActive(true);
        }
    }

    public void Hit(int damage)
    {
        if (IsAlive && !isInvincible)
        {
            Health -= damage;
            isInvincible = true;
            timeSinceHit = 0;
            AudioManager.Instance.PlayHitSound();

            // Set hurt state

            if (Health > 0)
            {
                isHit = true;
                animator.SetBool(AnimationStrings.isHit, true);
                Debug.Log($"[{gameObject.name}] Hit! Health: {Health}, Damage: {damage}");
            }
        }
    }
}
