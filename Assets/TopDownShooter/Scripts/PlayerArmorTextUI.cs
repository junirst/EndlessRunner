using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerArmorTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private Player player;

    private Armor playerArmor;

    private void Awake()
    {
        if (!armorText)
        {
            armorText = GetComponent<TextMeshProUGUI>();
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

        playerArmor = player.GetArmor();
        if (playerArmor == null)
        {
            UpdateText(null);
            return;
        }

        playerArmor.ArmorChanged += HandleArmorChanged;
        HandleArmorChanged(playerArmor);
    }

    private void UnbindPlayer()
    {
        if (playerArmor != null)
        {
            playerArmor.ArmorChanged -= HandleArmorChanged;
            playerArmor = null;
        }
    }

    private void HandleArmorChanged(Armor armor)
    {
        UpdateText(armor);
    }

    private void UpdateText(Armor armor)
    {
        if (!armorText)
        {
            return;
        }

        if (armor == null)
        {
            armorText.text = "--%";
            return;
        }

        int percent = Mathf.RoundToInt(armor.NormalizedArmor * 100f);
        armorText.text = percent + "%";
    }
}