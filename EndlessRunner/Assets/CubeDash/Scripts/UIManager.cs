using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private GameObject startMenuUi;
    [SerializeField] private GameObject gameOverUi;
    [SerializeField] private GameObject pauseMenuUi;
    [SerializeField] private TextMeshProUGUI pauseScoreUI;
    [SerializeField] private TextMeshProUGUI pauseHighscoreUI;

    [SerializeField] private TextMeshProUGUI gameOverScoreUI;

    [SerializeField] private TextMeshProUGUI gameOverHighscoreUI;

    CubeGameManager gm;
    private void Start()
    {
        gm = CubeGameManager.Instance;
        gm.onGameOver.AddListener(ActivateGameOverUI);
        gm.onPause.AddListener(ActivatePauseUI);
        gm.onResume.AddListener(HidePauseUI);
        HidePauseUI();
    }

    public void PlayButtonHandler () 
    {
        AudioManager.Instance?.PlayButtonClickSfx();
        gm.StartGame();
        startMenuUi.SetActive(false);
    }

    public void ActivateGameOverUI () 
    {
        HidePauseUI();
        gameOverUi.SetActive(true);
        gameOverScoreUI.text = "Score: " + gm.PrettyScore();
        gameOverHighscoreUI.text = "Highscore: " + gm.PrettyHighscore();
    }

    public void ActivatePauseUI()
    {
        if (pauseMenuUi == null)
        {
            return;
        }

        pauseMenuUi.SetActive(true);

        if (pauseScoreUI != null)
        {
            pauseScoreUI.text = "Score: " + gm.PrettyScore();
        }

        if (pauseHighscoreUI != null)
        {
            pauseHighscoreUI.text = "Highscore: " + gm.PrettyHighscore();
        }
    }

    public void HidePauseUI()
    {
        if (pauseMenuUi != null)
        {
            pauseMenuUi.SetActive(false);
        }
    }

    public void ContinueButtonHandler()
    {
        AudioManager.Instance?.PlayButtonClickSfx();
        gm.ResumeGame();
    }

    public void BackToTitleScreen()
    {
        AudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }

    private void OnGUI()
    {
        scoreUI.text = gm.PrettyScore();
    }
}
