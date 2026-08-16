using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Range(1, 10)]
    [SerializeField] private float speed = 10f;

    [Range(1, 10)]
    [SerializeField] private float lifetime = 3f;

    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField] private string targetTag = "Enemy";

    private Rigidbody2D rb;

    private void Awake()
    {
        if (CompareTag("EnemyBullet"))
        {
            targetTag = "Player";
        }
        else if (CompareTag("Bullet"))
        {
            targetTag = "Enemy";
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        rb.velocity = transform.up * speed;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        TryDealDamage(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDealDamage(other.gameObject);
    }

    private void TryDealDamage(GameObject targetObject)
    {
        bool isExpectedTagTarget = targetObject.CompareTag(targetTag);
        bool canHitWeaponDropDestructible = targetTag == "Enemy" && targetObject.GetComponentInParent<ShooterWeaponDropper>() != null;
        if (!isExpectedTagTarget && !canHitWeaponDropDestructible)
        {
            return;
        }

        Player player = targetObject.GetComponentInParent<Player>();
        if (player != null)
        {
            player.ApplyDamage(damage);
            Destroy(gameObject);
            return;
        }

        Health targetHealth = targetObject.GetComponentInParent<Health>();
        if (targetHealth == null)
        {
            return;
        }

        targetHealth.TakeDamage(damage);
        Destroy(gameObject);
    }
}
