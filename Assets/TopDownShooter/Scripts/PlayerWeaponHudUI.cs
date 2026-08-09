using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerWeaponHudUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private Sprite weaponIcon;
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
        RefreshIcon();
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
        player = targetPlayer;
        RefreshAmmoText();
    }

    private void BindPlayer()
    {
        if (!player)
        {
            player = FindObjectOfType<Player>();
        }

        RefreshAmmoText();
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

        weaponIconImage.sprite = weaponIcon;
        weaponIconImage.enabled = weaponIcon != null;
    }
}