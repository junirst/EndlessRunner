using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Snake snake;
    [SerializeField] private Food food;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Application.runInBackground = true;
    }

    private void Start()
    {
        gameOverScreen.SetActive(false);
        SnakeAudioManager.Instance?.PlayBgm();
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        int score = ScoreManager.Instance.Score;
        if (score > ScoreManager.Instance.HighScore)
        {
            ScoreManager.Instance.HighScore = score;
            ScoreManager.Instance.SaveHighScore();
        }

        finalScoreText.text = $"Score: {score}\nHigh Score: {ScoreManager.Instance.HighScore}";
        gameOverScreen.SetActive(true);
        snake.enabled = false;

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();

        PowerUpUI.Instance?.ClearAll();
        SnakeAudioManager.Instance?.StopBgm();
        SnakeAudioManager.Instance?.PlayGameOverSfx();
    }

    public void Retry()
    {
        IsGameOver = false;
        Time.timeScale = 1f;
        gameOverScreen.SetActive(false);
        snake.enabled = true;
        snake.ResetState();
        food.Reposition();
        ScoreManager.Instance.Reset();
        SnakeAudioManager.Instance?.PlayBgm();
    }

    public void LoadMainMenu()
    {
        SettingsManager.Instance?.RevertToPlayerPrefs();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}
