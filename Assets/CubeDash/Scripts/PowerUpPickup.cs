using UnityEngine;

public class PowerUpPickup : MonoBehaviour
{
    public enum PowerUpType
    {
        ScoreBonus,
        ScoreMultiplier
    }

    [SerializeField] private PowerUpType powerUpType = PowerUpType.ScoreMultiplier;
    [SerializeField] private float scoreBonus = 10f;
    [SerializeField] private float scoreMultiplier = 2f;
    [SerializeField] private float scoreMultiplierDuration = 5f;

    private bool isCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerMovement>() == null)
        {
            return;
        }

        isCollected = true;
        ApplyEffect();
        Destroy(gameObject);
    }

    private void ApplyEffect()
    {
        if (CubeGameManager.Instance == null)
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
        }
    }
}