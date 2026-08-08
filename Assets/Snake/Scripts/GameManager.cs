using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private Snake snake;
    [SerializeField] private Food food;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private LoadingScreen loadingScreen;

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

        if (loadingScreen == null)
            BuildLoadingScreen();
    }

    private void BuildLoadingScreen()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        GameObject lsGO = new GameObject("LoadingScreen", typeof(RectTransform));
        lsGO.transform.SetParent(canvas.transform, false);
        RectTransform lsRT = lsGO.GetComponent<RectTransform>();
        lsRT.anchorMin = Vector2.zero;
        lsRT.anchorMax = Vector2.one;
        lsRT.offsetMin = Vector2.zero;
        lsRT.offsetMax = Vector2.zero;

        CanvasGroup cg = lsGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image bg = lsGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true;

        GameObject snakeGO = new GameObject("SnakeContainer", typeof(RectTransform));
        snakeGO.transform.SetParent(lsRT, false);
        RectTransform snakeRT = snakeGO.GetComponent<RectTransform>();
        snakeRT.anchorMin = new Vector2(1f, 0f);
        snakeRT.anchorMax = new Vector2(1f, 0f);
        snakeRT.anchoredPosition = new Vector2(-40f, 40f);
        snakeRT.sizeDelta = new Vector2(250f, 60f);
        snakeRT.pivot = new Vector2(1f, 0f);

        LoadingScreen ls = lsGO.AddComponent<LoadingScreen>();
        ls.Configure(cg, snakeRT);
        loadingScreen = ls;
    }

    private void Start()
    {
        LeaderboardManager.EnsureInstance();

        gameOverScreen.SetActive(false);
        ScoreManager.Instance.StageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        ScoreManager.Instance.LoadHighScore();
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

        if (loadingScreen != null)
            loadingScreen.ShowAndLoad(SceneManager.GetActiveScene().name);
        else
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
