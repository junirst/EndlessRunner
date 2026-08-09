using UnityEngine;

[DisallowMultipleComponent]
public class EnemyPowerUpDropper : MonoBehaviour
{
    [Header("Health Drop")]
    [SerializeField, Range(0f, 1f)] private float healthDropChance = 0.25f;
    [SerializeField] private GameObject healthPickupPrefab;

    [Header("Armor Drop")]
    [SerializeField, Range(0f, 1f)] private float armorDropChance = 0.2f;
    [SerializeField] private GameObject armorPickupPrefab;

    [Header("Spawn")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0f, 0.4f);

    public void DropLoot()
    {
        TryDrop(healthPickupPrefab, healthDropChance);
        TryDrop(armorPickupPrefab, armorDropChance);
    }

    private void TryDrop(GameObject pickupPrefab, float dropChance)
    {
        if (!pickupPrefab || Random.value > dropChance)
        {
            return;
        }

        Vector3 spawnPosition = transform.position + (Vector3)spawnOffset;
        Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
    }
}