using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Player player;

    private Health playerHealth;

    private void Awake()
    {
        if (!healthText)
        {
            healthText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        BindPlayer();
    }

    private void OnDestroy()
    {
        UnbindPlayer();
    }

    public void BindPlayer(Player targetPlayer)
    {
        if (player == targetPlayer)
        {
            return;
        }

        UnbindPlayer();
        player = targetPlayer;
        BindPlayer();
    }

    private void BindPlayer()
    {
        if (!player)
        {
            player = FindObjectOfType<Player>();
        }

        if (!player)
        {
            UpdateText(null);
            return;
        }

        playerHealth = player.GetHealth();
        if (playerHealth == null)
        {
            UpdateText(null);
            return;
        }

        playerHealth.HealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerHealth);
    }

    private void UnbindPlayer()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= HandleHealthChanged;
            playerHealth = null;
        }
    }

    private void HandleHealthChanged(Health health)
    {
        UpdateText(health);
    }

    private void UpdateText(Health health)
    {
        if (!healthText)
        {
            return;
        }

        if (health == null)
        {
            healthText.text = "--%";
            return;
        }

        int percent = Mathf.RoundToInt(health.NormalizedHealth * 100f);
        healthText.text = percent + "%";
    }
}