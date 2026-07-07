using System.Collections;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private int basePoints = 10;

    private int score;
    private int highScore;
    private float multiplier = 1f;
    private Coroutine multiplierCoroutine;

    public int Score => score;
    public int HighScore
    {
        get => highScore;
        set => highScore = value;
    }

    private const string HighScoreKey = "SnakeHighScore";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadHighScore();
    }

    public void AddScore(int points)
    {
        score += Mathf.RoundToInt(points * multiplier);
    }

    public void SetMultiplier(float value, float duration)
    {
        if (multiplierCoroutine != null)
            StopCoroutine(multiplierCoroutine);
        multiplierCoroutine = StartCoroutine(MultiplierRoutine(value, duration));
    }

    public void ResetMultiplier()
    {
        if (multiplierCoroutine != null)
        {
            StopCoroutine(multiplierCoroutine);
            multiplierCoroutine = null;
        }
        multiplier = 1f;
    }

    private IEnumerator MultiplierRoutine(float value, float duration)
    {
        multiplier = value;
        PowerUpUI.Instance?.ShowPowerUp(Food.FoodType.ScoreMultiplier, duration);
        yield return new WaitForSeconds(duration);
        multiplier = 1f;
        multiplierCoroutine = null;
    }

    public void SaveHighScore()
    {
        PlayerPrefs.SetInt(HighScoreKey, highScore);
        PlayerPrefs.Save();
    }

    public void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    public void Reset()
    {
        score = 0;
        ResetMultiplier();
    }
}
