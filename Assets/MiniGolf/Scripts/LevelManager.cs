using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        Time.timeScale = 1f;
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        updateStrokeUI();
    }

    private void Update()
    {
        if (CanTogglePause() && Input.GetKeyDown(KeyCode.Escape))
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

        levelCompleteUI.SetActive(true);
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
        MiniGolfAudioManager.Instance?.PlayGameOverSfx();
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
}
