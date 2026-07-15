using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    public GameObject pauseMenu;
    public GameObject settingsUI;

    public bool IsPaused { get; private set; }

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
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (settingsUI != null)
            settingsUI.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                SnakeAudioManager.Instance?.PlayButtonClickSfx();
                Time.timeScale = 1f;
                SceneManager.LoadScene("TitleScreen");
                return;
            }

            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        ScoreUIManager.Instance?.SetVisible(false);
        if (PowerUpUI.Instance != null)
            PowerUpUI.Instance.gameObject.SetActive(false);
        CubeGameManager.Instance?.PauseGame();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (settingsUI != null)
            settingsUI.SetActive(false);
        ScoreUIManager.Instance?.SetVisible(true);
        if (PowerUpUI.Instance != null)
            PowerUpUI.Instance.gameObject.SetActive(true);
        CubeGameManager.Instance?.ResumeGame();
    }

    public void ContinueButton()
    {
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        Resume();
    }

    public void ShowSettings()
    {
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (settingsUI != null)
            settingsUI.SetActive(true);
    }

    public void HideSettings()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(true);
        if (settingsUI != null)
            settingsUI.SetActive(false);
    }

    public void LoadMainMenu()
    {
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}
