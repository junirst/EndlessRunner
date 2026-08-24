using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(LevelStarRating))]
public class LevelManager : MonoBehaviour
{
    public static LevelManager main;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI strokeUI;
    [Space(10)]
    [SerializeField] private GameObject levelCompleteUI;
    [SerializeField] private TextMeshProUGUI levelCompleteStrokeUI;
    [Space(10)]
    [SerializeField] private GameObject GameOverUI;
    [Space(10)]
    [SerializeField] private GameObject pauseMenuUI;

    [Header("Audio")]
    [SerializeField] private AudioClip gameOverSfx;

    [Header("Attributes")]
    [SerializeField] private int maxStrokes;

    private int strokes;
    private LevelStarRating levelStarRating;
    [HideInInspector] public bool outOfStrokes;
    [HideInInspector] public bool levelCompleted;
    [HideInInspector] public bool isPaused;

    private bool gameOver;

    private void Awake()
    {
        main = this;
        levelStarRating = GetComponent<LevelStarRating>();
    }

    private void Start()
    {
        LeaderboardManager.EnsureInstance();

        Time.timeScale = 1f;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        EnsureCompletionButtonLabels();
        updateStrokeUI();
    }

    private void Update()
    {
        if (CanTogglePause() && Input.GetKeyDown(KeyCode.Escape) && PauseManager.Instance == null)
        {
            TogglePause();
        }
    }

    public void IncreaseStroke()
    {
        strokes++;
        updateStrokeUI();

        if (strokes >= maxStrokes)
        {
            outOfStrokes = true;
        }
    }

    public void LevelComplete()
    {
        if (isPaused)
        {
            ResumeGame();
        }

        levelCompleted = true;
        int starRating = levelStarRating != null ? levelStarRating.GetStarRating(strokes) : 1;
        levelStarRating?.SetStarDisplay(starRating);
        global::MiniGolfTotalStarsManager.RegisterLevelStars(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, starRating);
        MiniGolfAudioManager.Instance?.PlayLevelCompleteSfx();

        string strokeMessage = strokes > 1 ? "You putted in " + strokes + " strokes" : "You got a hole in one!";
        string completionHint = levelStarRating != null ? levelStarRating.GetCompletionHintText(strokes) : string.Empty;
        if (levelCompleteStrokeUI != null)
        {
            levelCompleteStrokeUI.text = string.IsNullOrEmpty(completionHint) ? strokeMessage : strokeMessage + "\n" + completionHint;
        }

        if (levelCompleteUI != null)
        {
            EnsureCompletionButtonLabels();
            levelCompleteUI.SetActive(false);
        }
        ShowLevelLeaderboard(levelCompleteUI, strokes);
    }

    public static string LeaderboardStageId()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var match = System.Text.RegularExpressions.Regex.Match(scene, @"\d+$");
        return match.Success ? "level" + match.Value : scene.ToLowerInvariant().Replace("-", "_");
    }

    private static void ShowLevelLeaderboard(GameObject screenToReveal, int strokes)
    {
        LeaderboardUI.ShowForGame(screenToReveal, "minigolf", LeaderboardStageId(), strokes);
    }

    public void GameOver()
    {
        if (gameOver)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }

        gameOver = true;
        MiniGolfAudioManager.Instance?.PlayGameOverSfx(gameOverSfx);
        GameOverUI.SetActive(true);
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (!CanTogglePause() || isPaused)
        {
            return;
        }

        isPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    public void ContinueButtonHandler()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        ResumeGame();
    }

    public void ReplayButtonHandler()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        if (levelCompleteUI != null)
        {
            levelCompleteUI.SetActive(false);
        }
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenuButtonHandler()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("MainMenu", UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public bool CanAcceptInput()
    {
        return !isPaused && !levelCompleted && !gameOver;
    }

    private bool CanTogglePause()
    {
        return !levelCompleted && !gameOver;
    }

    private void updateStrokeUI()
    {
        strokeUI.text = strokes + "/" + maxStrokes;
    }

    private void EnsureCompletionButtonLabels()
    {
        if (levelCompleteUI == null)
        {
            return;
        }

        foreach (Button button in levelCompleteUI.GetComponentsInChildren<Button>(true))
        {
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null)
            {
                continue;
            }

            if (button.name.IndexOf("Continue", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Continue";
            }
            else if (button.name.IndexOf("Next", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Next Level";
            }
            else if (button.name.IndexOf("Replay", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                label.text = "Replay";
            }

            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = new Color(0.12f, 0.55f, 0.72f, 1f);
            }

            label.enabled = true;
            label.gameObject.SetActive(true);
            label.color = Color.white;
            label.transform.SetAsLastSibling();
        }
    }
}
