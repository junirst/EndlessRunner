using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerWeaponHudUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Sprite defaultWeaponIcon;
    [SerializeField] private Player player;

    private void Awake()
    {
        if (!ammoText)
        {
            ammoText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Start()
    {
        BindPlayer();
        RefreshAll();
    }

    private void OnEnable()
    {
        if (player != null)
        {
            player.PlayerWeaponChanged += HandleWeaponChanged;
        }
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.PlayerWeaponChanged -= HandleWeaponChanged;
        }
    }

    private void Update()
    {
        if (!player)
        {
            BindPlayer();
        }

        RefreshAmmoText();
    }

    public void BindPlayer(Player targetPlayer)
    {
        if (player != null)
        {
            player.PlayerWeaponChanged -= HandleWeaponChanged;
        }

        player = targetPlayer;
        if (player != null)
        {
            player.PlayerWeaponChanged += HandleWeaponChanged;
        }

        RefreshAll();
    }

    private void HandleWeaponChanged()
    {
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshAmmoText();
        RefreshIcon();
    }

    private void BindPlayer()
    {
        if (!player)
        {
            player = FindObjectOfType<Player>();
        }

        RefreshAll();
    }

    private void RefreshAmmoText()
    {
        if (!ammoText)
        {
            return;
        }

        if (!player)
        {
            ammoText.text = "--";
            return;
        }

        ammoText.text = player.GetAmmoDisplayText();
    }

    private void RefreshIcon()
    {
        if (!weaponIconImage)
        {
            return;
        }

        Sprite currentIcon = player != null ? player.GetWeaponIcon() : null;
        if (currentIcon == null)
        {
            currentIcon = defaultWeaponIcon;
        }

        weaponIconImage.sprite = currentIcon;
        weaponIconImage.enabled = currentIcon != null;
    }
}