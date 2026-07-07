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

    private bool isGameOver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gameOverScreen.SetActive(false);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        finalScoreText.text = $"Score: {ScoreManager.Instance.Score}";
        gameOverScreen.SetActive(true);
        snake.enabled = false;
    }

    public void Retry()
    {
        isGameOver = false;
        gameOverScreen.SetActive(false);
        snake.enabled = true;
        snake.ResetState();
        food.Reposition();
        ScoreManager.Instance.Reset();
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}
