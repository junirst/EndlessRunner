using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform target;
    public float speed = 3f;
    public float rotationSpeed = 0.0025f;
    [SerializeField, Min(0f)] private float contactDamage = 25f;
    [SerializeField, Min(0)] private int scoreValue = 1;
    private Rigidbody2D rb;
    private Health health;
    private bool wasMoving;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!health)
        {
            health = gameObject.AddComponent<Health>();
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        health.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (!target)
        {
           GetTarget(); 
        } else {
            RotateTowardsTarget();
        }

        
    }
    private void FixedUpdate()
    {
        rb.velocity = transform.up * speed;
        bool isMoving = rb.velocity.sqrMagnitude > 0.01f;
        if (isMoving != wasMoving)
        {
            ShooterAudioManager.Instance?.SetEnemyMovementSfx(gameObject, isMoving);
            wasMoving = isMoving;
        }
    }

    private void OnDisable()
    {
        ShooterAudioManager.Instance?.SetEnemyMovementSfx(gameObject, false);
        wasMoving = false;
    }

    private void RotateTowardsTarget()
    {
        Vector2 targetDirection = target.position - transform.position;
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg - 90f;
        Quaternion q = Quaternion.Euler(new Vector3(0, 0, angle));
        transform.localRotation = Quaternion.Slerp(transform.localRotation, q, rotationSpeed);
    }

    private void GetTarget()
    {
        if (GameObject.FindGameObjectWithTag("Player"))
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            DealContactDamage(other.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            DealContactDamage(other.gameObject);
        }
    }

    private void DealContactDamage(GameObject targetObject)
    {
        Player player = targetObject.GetComponent<Player>();
        if (player != null)
        {
            player.ApplyDamage(contactDamage);
            return;
        }

        Health targetHealth = targetObject.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.TakeDamage(contactDamage);
        }
    }

    private void HandleDied(Health deadHealth)
    {
        ShooterLevelManager.manager?.InscreaseScore(scoreValue);
        GetDropper().DropLoot();
        ShooterAudioManager.Instance?.PlayEnemyDeathSfx();
        GetDeathEffect().PlayAndDestroy();
    }

    private EnemyPowerUpDropper GetDropper()
    {
        EnemyPowerUpDropper dropper = GetComponent<EnemyPowerUpDropper>();
        if (!dropper)
        {
            dropper = gameObject.AddComponent<EnemyPowerUpDropper>();
        }

        return dropper;
    }

    private ArcadeDeathEffect2D GetDeathEffect()
    {
        ArcadeDeathEffect2D effect = GetComponent<ArcadeDeathEffect2D>();
        if (!effect)
        {
            effect = gameObject.AddComponent<ArcadeDeathEffect2D>();
        }

        return effect;
    }

    private void PlayDeathEffect(GameObject targetObject)
    {
        ArcadeDeathEffect2D playerDeathEffect = targetObject.GetComponent<ArcadeDeathEffect2D>();
        if (!playerDeathEffect)
        {
            playerDeathEffect = targetObject.AddComponent<ArcadeDeathEffect2D>();
        }

        playerDeathEffect.PlayAndDestroy();
    }
}
