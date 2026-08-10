using UnityEngine;

[DisallowMultipleComponent]
public class ShooterWeaponDropper : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.1f;
    [SerializeField] private GameObject weaponPickupPrefab;
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.4f);
    [SerializeField] private bool dropOnDeath = true;
    [SerializeField] private bool destroyObjectOnDeath = true;
    [SerializeField, Min(1f)] private float maxHealthIfMissing = 50f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
        {
            health = gameObject.AddComponent<Health>();
            health.SetMaxHealth(maxHealthIfMissing);
        }
    }

    private void Start()
    {
        if (dropOnDeath && health != null)
        {
            health.Died += HandleDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
        }
    }

    public void DropWeapon()
    {
        if (!weaponPickupPrefab || Random.value > dropChance)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;
        Instantiate(weaponPickupPrefab, spawnPosition, Quaternion.identity);
    }

    private void HandleDied(Health deadHealth)
    {
        DropWeapon();

        if (!destroyObjectOnDeath)
        {
            return;
        }

        ArcadeDeathEffect2D deathEffect = GetComponent<ArcadeDeathEffect2D>();
        if (deathEffect != null)
        {
            deathEffect.PlayAndDestroy();
            return;
        }

        Destroy(gameObject);
    }
}