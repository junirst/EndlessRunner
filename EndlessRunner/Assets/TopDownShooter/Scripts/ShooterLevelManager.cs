using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShooterLevelManager : MonoBehaviour
{
    public static ShooterLevelManager manager;

    public GameObject deathScreen;
    public GameObject pauseMenu;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;
    public TextMeshProUGUI pauseScoreText;
    public TextMeshProUGUI pauseHighscoreText;

    public ShooterSaveData data;

    public int score;
    public bool isPaused;
    public bool isGameOver;
    

    private void Awake()
    {
        manager = this;
        ShooterSaveSystem.Initialize();

        data = new ShooterSaveData(0);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }

        string loadedData = ShooterSaveSystem.Load("save");
        if (loadedData != null)
        {
            data = JsonUtility.FromJson<ShooterSaveData>(loadedData);
        }

        ShooterAudioManager.Instance?.PlayBgm();
    }

    private void Update()
    {
        if (CanTogglePause() && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void GameOver()
    {
        if (isGameOver)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }

        isGameOver = true;
        ShooterAudioManager.Instance?.StopBgm();
        ShooterAudioManager.Instance?.PlayGameOverSfx();

        deathScreen.SetActive(true);
        scoreText.text = "Score: " + score.ToString();

        string loadedData = ShooterSaveSystem.Load("save");
        if (loadedData != null) {
            data = JsonUtility.FromJson<ShooterSaveData>(loadedData);
        }
        if (data.highscore < score) {
            data.highscore = score;
        }

        highscoreText.text = "Highscore: " + data.highscore.ToString();

        string saveData = JsonUtility.ToJson(data);
        ShooterSaveSystem.Save("save", saveData);
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

        if (pauseScoreText != null)
        {
            pauseScoreText.text = "Score: " + score.ToString();
        }

        if (pauseHighscoreText != null)
        {
            pauseHighscoreText.text = "Highscore: " + data.highscore.ToString();
        }

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
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

        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
    }

    public void ContinueButtonHandler()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        ResumeGame();
    }

    public void ReplayGame()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        ShooterAudioManager.Instance?.StopBgm();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void BackToTitleScreen()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        ShooterAudioManager.Instance?.StopBgm();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }

    public bool CanAcceptInput()
    {
        return !isPaused && !isGameOver;
    }

    private bool CanTogglePause()
    {
        return !isGameOver;
    }

    public void InscreaseScore(int amount)
    {
        score += amount;
    }
}

[System.Serializable]
public class ShooterSaveData {
    public int highscore;

    public ShooterSaveData (int _hs) {
        highscore = _hs;
    }
}
