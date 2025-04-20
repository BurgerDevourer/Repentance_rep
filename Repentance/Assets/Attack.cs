using UnityEngine;

public class Attack : MonoBehaviour
{
    Collider2D attackColider;
    public int attackDamage = 10;

    void Awake()
    {
        attackColider = GetComponent<Collider2D>();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Damageable damageable = collision.GetComponent<Damageable>();

        if(damageable != null)
        {
            damageable.Hit(attackDamage);
            Debug.Log(collision.name + "hit for" + attackDamage);
        }
    }
}
