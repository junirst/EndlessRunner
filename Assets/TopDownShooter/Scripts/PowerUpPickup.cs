using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ShooterPowerUpPickup : MonoBehaviour
{
    public enum PowerUpType
    {
        Health,
        Armor
    }

    [SerializeField] private PowerUpType powerUpType = PowerUpType.Health;
    [SerializeField, Min(0f)] private float amount = 25f;

    private void Reset()
    {
        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponentInParent<Player>();
        if (player == null)
        {
            return;
        }

        bool consumed = ApplyToPlayer(player);
        if (consumed)
        {
            Destroy(gameObject);
        }
    }

    private bool ApplyToPlayer(Player player)
    {
        switch (powerUpType)
        {
            case PowerUpType.Health:
                Health health = player.GetHealth();
                if (health == null)
                {
                    return false;
                }

                health.Heal(amount);
                return true;

            case PowerUpType.Armor:
                Armor armor = player.GetArmor();
                if (armor == null)
                {
                    return false;
                }

                armor.Repair(amount);
                return true;

            default:
                return false;
        }
    }
}