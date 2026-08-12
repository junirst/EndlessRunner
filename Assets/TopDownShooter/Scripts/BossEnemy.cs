using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BossEnemy : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private string bossDisplayName = "BOSS";

    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 500f;

    [Header("Movement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float rotationSpeed = 0.0025f;

    [Header("Contact Damage")]
    [SerializeField, Min(0f)] private float contactDamage = 40f;
    [SerializeField, Min(0)] private int scoreValue = 20;

    [Header("Ranged Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [SerializeField, Min(0f)] private float distanceToShoot = 6f;
    [SerializeField, Min(0f)] private float distanceToStop = 4f;
    [SerializeField, Min(0f)] private float fireRate = 1.5f;

    [Header("Death")]
    [SerializeField] private GameObject deathOverlayEffectPrefab;
    [SerializeField] private Vector2 deathOverlayOffset = Vector2.zero;
    [SerializeField, Min(0f)] private float deathOverlayLifetime = 2f;

    public static BossEnemy CurrentBoss { get; private set; }

    public static event Action<BossEnemy> BossSpawned;
    public static event Action<BossEnemy> BossDespawned;

    public string BossDisplayName => bossDisplayName;
    public Health Health { get; private set; }

    private Transform target;
    private Rigidbody2D rb;
    private MotionDrivenBodySpriteAnimator2D bodyAnimator;
    private float timeToFire;
    private bool wasMoving;

    private void Awake()
    {
        Health = GetComponent<Health>();
        if (Health == null)
        {
            Health = gameObject.AddComponent<Health>();
        }

        Health.SetMaxHealth(maxHealth);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyAnimator = GetComponentInChildren<MotionDrivenBodySpriteAnimator2D>(true);
        Health.Died += HandleDied;
    }

    private void OnEnable()
    {
        CurrentBoss = this;
        BossSpawned?.Invoke(this);
    }

    private void OnDisable()
    {
        if (CurrentBoss == this)
        {
            CurrentBoss = null;
        }

        BossDespawned?.Invoke(this);
        ShooterAudioManager.Instance?.SetBossMovementSfx(gameObject, false);
        wasMoving = false;
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.Died -= HandleDied;
        }
    }

    private void Update()
    {
        if (!target)
        {
            GetTarget();
        }
        else
        {
            RotateTowardsTarget();
        }

        if (target != null && Vector2.Distance(target.position, transform.position) <= distanceToShoot)
        {
            Shoot();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || target == null)
        {
            return;
        }

        rb.velocity = Vector2.Distance(target.position, transform.position) >= distanceToStop
            ? (Vector2)transform.up * speed
            : Vector2.zero;

        bool isMoving = rb.velocity.sqrMagnitude > 0.01f;
        if (isMoving != wasMoving)
        {
            ShooterAudioManager.Instance?.SetBossMovementSfx(gameObject, isMoving);
            wasMoving = isMoving;
        }
    }

    private void Shoot()
    {
        if (!bulletPrefab || !firingPoint)
        {
            return;
        }

        if (timeToFire <= 0f)
        {
            Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
            bodyAnimator?.TriggerAttack();
            ShooterAudioManager.Instance?.PlayBossAttackSfx();
            timeToFire = fireRate;
        }
        else
        {
            timeToFire -= Time.deltaTime;
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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            target = player.transform;
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
        SpawnDeathOverlayEffect();
        GetBossDeathEffect().PlayAndDestroy();
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

    private BossDeathEffect2D GetBossDeathEffect()
    {
        BossDeathEffect2D effect = GetComponent<BossDeathEffect2D>();
        if (!effect)
        {
            effect = gameObject.AddComponent<BossDeathEffect2D>();
        }

        return effect;
    }

    private void SpawnDeathOverlayEffect()
    {
        if (!deathOverlayEffectPrefab)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + (Vector3)deathOverlayOffset;
        GameObject overlay = Instantiate(deathOverlayEffectPrefab, spawnPosition, Quaternion.identity);

        if (deathOverlayLifetime > 0f)
        {
            Destroy(overlay, deathOverlayLifetime);
        }
    }
}
