using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [Range(0.1f, 2f)]
    [SerializeField] private float fireRate = 0.5f;

    private Rigidbody2D rb;
    private ArcadeDeathEffect2D deathEffect;
    private MotionDrivenBodySpriteAnimator2D bodyAnimator;
    private Health health;
    private Armor armor;
    private float mx;
    private float my;

    private float fireTimer;

    private Vector2 mousePos;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!health)
        {
            health = gameObject.AddComponent<Health>();
        }

        armor = GetComponent<Armor>();
        if (!armor)
        {
            armor = gameObject.AddComponent<Armor>();
        }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyAnimator = GetComponentInChildren<MotionDrivenBodySpriteAnimator2D>(true);
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
        if (!ShooterLevelManager.manager.CanAcceptInput())
        {
            mx = 0f;
            my = 0f;
            return;
        }

        mx = Input.GetAxisRaw("Horizontal");
        my = Input.GetAxisRaw("Vertical");
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        float angle = Mathf.Atan2(mousePos.y - transform.position.y, mousePos.x - transform.position.x) * Mathf.Rad2Deg - 90f;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        if (Input.GetMouseButton(0) && fireTimer <= 0f)
        {
            Shoot();
            fireTimer = fireRate;
        } else {
            fireTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(mx, my).normalized * speed;
    }

    public void ResetFireTimer()
    {
        fireTimer = fireRate;
    }

    public Health GetHealth()
    {
        return health;
    }

    public Armor GetArmor()
    {
        return armor;
    }

    public void ApplyDamage(float damageAmount)
    {
        if (damageAmount <= 0f)
        {
            return;
        }

        float remainingDamage = damageAmount;
        if (armor != null)
        {
            remainingDamage = armor.AbsorbDamage(remainingDamage);
        }

        if (remainingDamage > 0f)
        {
            health?.TakeDamage(remainingDamage);
        }
    }

    private void Shoot()
    {
        Instantiate(bulletPrefab, firingPoint.position, firingPoint.rotation);
        bodyAnimator?.TriggerAttack();
        ShooterAudioManager.Instance?.PlayPlayerShootSfx();
    }

    private void HandleDied(Health deadHealth)
    {
        ShooterLevelManager.manager?.GameOver();
        GetDeathEffect().PlayAndDestroy();
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
}
