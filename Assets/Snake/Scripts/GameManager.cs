using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Snake snake;
    [SerializeField] private Food food;
    [SerializeField] private AudioClip gameOverSfx;
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
        LeaderboardManager.EnsureInstance();

        gameOverScreen.SetActive(false);
        ScoreManager.Instance.StageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        ScoreManager.Instance.LoadHighScore();
        SnakeAudioManager.Instance?.PlayBgm();

        EnsureLevel4MapGenerator();
    }

    private void EnsureLevel4MapGenerator()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "Level 4") return;

        if (FindObjectOfType<Level4MapGenerator>() != null) return;

        GameObject go = new GameObject("Level4MapGenerator");
        go.AddComponent<Level4MapGenerator>();
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
        // The leaderboard shows first; the game over screen activates only once
        // the leaderboard is closed.
        gameOverScreen.SetActive(false);
        snake.enabled = false;

        ShowLeaderboard(score);

        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            PauseManager.Instance.Resume();

        PowerUpUI.Instance?.ClearAll();
        SnakeAudioManager.Instance?.StopBgm();
        SnakeAudioManager.Instance?.PlayGameOverSfx(gameOverSfx);
    }

    private void ShowLeaderboard(int score)
    {
        if (LeaderboardManager.Instance == null) return;

        Canvas canvas = gameOverScreen != null ? gameOverScreen.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        string boardKey = LeaderboardManager.GetBoardKey(ScoreManager.GameKey, ScoreManager.Instance.StageId);
        if (!LeaderboardManager.Instance.IsValidBoard(boardKey)) return;

        LeaderboardUI ui = LeaderboardUI.Show(canvas, boardKey, score);
        if (ui != null)
            ui.onClose = () =>
            {
                gameOverScreen.SetActive(true);
                ui.onClose = null;
            };
    }

    public void Retry()
    {
        IsGameOver = false;
        Time.timeScale = 1f;
        gameOverScreen.SetActive(false);

        DirectRetry();
    }

    private void DirectRetry()
    {
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
        SceneManager.LoadScene(PauseManager.Instance?.MainMenuSceneName ?? "SnakeMenu");
    }
}
