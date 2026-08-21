using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public enum PowerUpType
    {
        ScoreBonus,
        ScoreMultiplier,
        DoubleJump,
        Shield
    }

    [SerializeField] private PowerUpType powerUpType = PowerUpType.ScoreMultiplier;
    [SerializeField] private float scoreBonus = 10f;
    [SerializeField] private float scoreMultiplier = 2f;
    [SerializeField] private float scoreMultiplierDuration = 5f;
    [SerializeField] private float doubleJumpDuration = 8f;

    private bool isCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
        {
            return;
        }

        PlayerMovement playerMovement = other.GetComponentInParent<PlayerMovement>();
        PlayerCollison playerCollison = other.GetComponentInParent<PlayerCollison>();

        if (playerMovement == null && playerCollison == null)
        {
            return;
        }

        isCollected = true;
        AudioManager.Instance?.PlayPowerUpPickupSfx();
        ApplyEffect(playerMovement, playerCollison);
        Destroy(gameObject);
    }

    private void ApplyEffect(PlayerMovement playerMovement, PlayerCollison playerCollison)
    {
        if (CubeGameManager.Instance == null && powerUpType != PowerUpType.DoubleJump && powerUpType != PowerUpType.Shield)
        {
            return;
        }

        switch (powerUpType)
        {
            case PowerUpType.ScoreBonus:
                CubeGameManager.Instance.AddScore(scoreBonus);
                break;
            case PowerUpType.ScoreMultiplier:
                CubeGameManager.Instance.ApplyScoreMultiplier(scoreMultiplier, scoreMultiplierDuration);
                break;
            case PowerUpType.DoubleJump:
                if (playerMovement != null)
                {
                    playerMovement.ApplyDoubleJump(doubleJumpDuration);
                }

                break;
            case PowerUpType.Shield:
                if (playerCollison != null)
                {
                    playerCollison.ApplyShield();
                }

                break;
        }
    }
}