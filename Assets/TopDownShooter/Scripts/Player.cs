using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    [Header("Default Weapon")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firingPoint;
    [Range(0.1f, 2f)]
    [SerializeField] private float fireRate = 0.5f;
    [SerializeField] private bool infiniteAmmo = true;
    [SerializeField, Min(0)] private int maxAmmo = 12;
    [SerializeField, Min(0)] private int startingAmmo = 12;
    [SerializeField] private Sprite weaponIcon;
    [SerializeField] private MotionDrivenBodySpriteAnimator2D bodyWeaponAnimator;
    [SerializeField] private MotionDrivenBodySpriteAnimator2D gunWeaponAnimator;

    private Rigidbody2D rb;
    private ArcadeDeathEffect2D deathEffect;
    private Health health;
    private Armor armor;
    private float mx;
    private float my;

    private float fireTimer;
    private int currentAmmo;
    private Sprite currentWeaponIcon;
    private GameObject currentBulletPrefab;
    private float currentFireRate;
    private bool currentInfiniteAmmo;
    private int currentMaxAmmo;
    private bool isUsingDefaultWeapon;
    private Sprite[] currentBodyFrames;
    private Sprite[] currentGunFrames;
    private Sprite[] defaultBodyFrames;
    private Sprite[] defaultGunFrames;

    public event Action PlayerWeaponChanged;

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

        if (!bodyWeaponAnimator)
        {
            bodyWeaponAnimator = GetComponentInChildren<MotionDrivenBodySpriteAnimator2D>(true);
        }

        if (!gunWeaponAnimator)
        {
            MotionDrivenBodySpriteAnimator2D[] weaponAnimators = GetComponentsInChildren<MotionDrivenBodySpriteAnimator2D>(true);
            if (weaponAnimators != null && weaponAnimators.Length > 1)
            {
                gunWeaponAnimator = weaponAnimators[1];
            }
        }

        defaultBodyFrames = CloneFrames(bodyWeaponAnimator != null ? bodyWeaponAnimator.GetFramesCopy() : null);
        defaultGunFrames = CloneFrames(gunWeaponAnimator != null ? gunWeaponAnimator.GetFramesCopy() : null);

        ApplyDefaultWeapon();

        if (currentInfiniteAmmo)
        {
            currentAmmo = int.MaxValue;
        }
        else
        {
            currentAmmo = Mathf.Clamp(startingAmmo, 0, currentMaxAmmo);
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
            fireTimer = currentFireRate;
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
        fireTimer = currentFireRate;
    }

    public Health GetHealth()
    {
        return health;
    }

    public Armor GetArmor()
    {
        return armor;
    }

    public bool HasInfiniteAmmo()
    {
        return currentInfiniteAmmo;
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public int GetMaxAmmo()
    {
        return currentMaxAmmo;
    }

    public string GetAmmoDisplayText()
    {
        if (currentInfiniteAmmo)
        {
            return "∞";
        }

        return currentAmmo + "/" + currentMaxAmmo;
    }

    public Sprite GetWeaponIcon()
    {
        return currentWeaponIcon;
    }

    public void EquipWeapon(ShooterWeaponPickup weaponPickup)
    {
        if (!weaponPickup)
        {
            return;
        }

        currentWeaponIcon = weaponPickup.GetWeaponIcon();
        currentBulletPrefab = weaponPickup.GetBulletPrefab();
        currentFireRate = weaponPickup.GetFireRate();
        currentInfiniteAmmo = weaponPickup.HasInfiniteAmmo();
        currentMaxAmmo = Mathf.Max(0, weaponPickup.GetMaxAmmo());
        isUsingDefaultWeapon = false;

        if (currentInfiniteAmmo)
        {
            currentAmmo = int.MaxValue;
        }
        else
        {
            int startingAmmoForWeapon = Mathf.Clamp(weaponPickup.GetStartingAmmo(), 0, currentMaxAmmo);
            currentAmmo = startingAmmoForWeapon;
        }

        currentBodyFrames = SelectFramesOrDefault(weaponPickup.GetBodyFrames(), defaultBodyFrames);
        currentGunFrames = SelectFramesOrDefault(weaponPickup.GetGunFrames(), defaultGunFrames);
        ApplyWeaponVisuals();
        ResetFireTimer();
        PlayerWeaponChanged?.Invoke();
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
        if (currentBulletPrefab == null)
        {
            return;
        }

        GameObject bulletToSpawn = currentBulletPrefab;

        if (!TryConsumeAmmo())
        {
            return;
        }

        Instantiate(bulletToSpawn, firingPoint.position, firingPoint.rotation);
        bodyWeaponAnimator?.TriggerAttack();
        gunWeaponAnimator?.TriggerAttack();
        ShooterAudioManager.Instance?.PlayPlayerShootSfx();

        TrySwitchToDefaultWeaponIfOutOfAmmo();
    }

    private bool TryConsumeAmmo()
    {
        if (currentInfiniteAmmo)
        {
            return true;
        }

        if (currentAmmo <= 0)
        {
            TrySwitchToDefaultWeaponIfOutOfAmmo();
            return false;
        }

        currentAmmo--;
        return true;
    }

    private void ApplyDefaultWeapon()
    {
        currentWeaponIcon = weaponIcon;
        currentBulletPrefab = bulletPrefab;
        currentFireRate = fireRate;
        currentInfiniteAmmo = infiniteAmmo;
        currentMaxAmmo = Mathf.Max(0, maxAmmo);
        isUsingDefaultWeapon = true;
        currentBodyFrames = CloneFrames(defaultBodyFrames);
        currentGunFrames = CloneFrames(defaultGunFrames);
        ApplyWeaponVisuals();
    }

    private void ApplyWeaponVisuals()
    {
        if (bodyWeaponAnimator != null && currentBodyFrames != null && currentBodyFrames.Length > 0)
        {
            bodyWeaponAnimator.SetFrames(currentBodyFrames);
        }

        if (gunWeaponAnimator != null && currentGunFrames != null && currentGunFrames.Length > 0)
        {
            gunWeaponAnimator.SetFrames(currentGunFrames);
        }
    }

    private void TrySwitchToDefaultWeaponIfOutOfAmmo()
    {
        if (currentInfiniteAmmo || currentAmmo > 0 || isUsingDefaultWeapon)
        {
            return;
        }

        ApplyDefaultWeapon();

        if (currentInfiniteAmmo)
        {
            currentAmmo = int.MaxValue;
        }
        else
        {
            currentAmmo = Mathf.Clamp(startingAmmo, 0, currentMaxAmmo);
        }

        ResetFireTimer();
        PlayerWeaponChanged?.Invoke();
    }

    private Sprite[] SelectFramesOrDefault(Sprite[] pickedFrames, Sprite[] fallbackFrames)
    {
        if (pickedFrames != null && pickedFrames.Length > 0)
        {
            return CloneFrames(pickedFrames);
        }

        return CloneFrames(fallbackFrames);
    }

    private Sprite[] CloneFrames(Sprite[] source)
    {
        if (source == null || source.Length == 0)
        {
            return new Sprite[0];
        }

        Sprite[] clone = new Sprite[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            clone[i] = source[i];
        }

        return clone;
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
