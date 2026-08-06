using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemy : MonoBehaviour
{
    public Transform target;
    public float speed = 3f;
    public float rotationSpeed = 0.0025f;
    private Rigidbody2D rb;
    private MotionDrivenBodySpriteAnimator2D bodyAnimator;
    public GameObject bulletPrefab;

    public float distanceToShoot = 5f;
    public float distanceToStop = 3f;

    public float fireRate;
    private float timeToFire;

    public Transform firingPoint;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyAnimator = GetComponentInChildren<MotionDrivenBodySpriteAnimator2D>(true);
    }

    private void Update()
    {
        if (!target)
        {
           GetTarget(); 
        } else {
            RotateTowardsTarget();
        }

        if (target != null && Vector2.Distance(target.position, transform.position) <= distanceToShoot)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (timeToFire <= 0f)
        {
            Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
            bodyAnimator?.TriggerAttack();
            ShooterAudioManager.Instance?.PlayEnemyShootSfx();
            timeToFire = fireRate;
        } else {
            timeToFire -= Time.deltaTime;
        }
    }
    private void FixedUpdate()
    {   
        if (target != null)
        {
            if (Vector2.Distance(target.position, transform.position) >= distanceToShoot)
            {
            rb.velocity = transform.up * speed;
            } else {
            rb.velocity = Vector2.zero;
            }
        }
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
            ShooterLevelManager.manager.GameOver();
            PlayDeathEffect(other.gameObject);
            target = null;
        } else if (other.gameObject.CompareTag("Bullet"))
        {
            ShooterLevelManager.manager.InscreaseScore(3);
            ShooterAudioManager.Instance?.PlayEnemyDeathSfx();
            Destroy(other.gameObject);
            GetDeathEffect().PlayAndDestroy();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Bullet"))
        {
            ShooterLevelManager.manager.InscreaseScore(3);
            ShooterAudioManager.Instance?.PlayEnemyDeathSfx();
            Destroy(other.gameObject);
            GetDeathEffect().PlayAndDestroy();
        }
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
