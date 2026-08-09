using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ShooterWeaponPickup : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private Sprite weaponIcon;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField, Min(0.1f)] private float fireRate = 0.5f;
    [SerializeField] private bool infiniteAmmo;
    [SerializeField, Min(0)] private int maxAmmo = 30;
    [SerializeField, Min(0)] private int startingAmmo = 30;

    [Header("Visuals")]
    [SerializeField] private Sprite[] bodyFrames;
    [SerializeField] private Sprite[] gunFrames;

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    public Sprite GetWeaponIcon()
    {
        return weaponIcon;
    }

    public GameObject GetBulletPrefab()
    {
        return bulletPrefab;
    }

    public float GetFireRate()
    {
        return fireRate;
    }

    public bool HasInfiniteAmmo()
    {
        return infiniteAmmo;
    }

    public int GetMaxAmmo()
    {
        return maxAmmo;
    }

    public int GetStartingAmmo()
    {
        return startingAmmo;
    }

    public Sprite[] GetBodyFrames()
    {
        return bodyFrames;
    }

    public Sprite[] GetGunFrames()
    {
        return gunFrames;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (!player)
        {
            return;
        }

        player.EquipWeapon(this);
        Destroy(gameObject);
    }
}