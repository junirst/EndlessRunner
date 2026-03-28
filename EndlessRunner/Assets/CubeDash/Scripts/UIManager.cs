using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreUI;
    [SerializeField] private GameObject startMenuUi;
    [SerializeField] private GameObject gameOverUi;

    [SerializeField] private TextMeshProUGUI gameOverScoreUI;

    [SerializeField] private TextMeshProUGUI gameOverHighscoreUI;

    GameManager gm;
    private void Start()
    {
        gm = GameManager.Instance;
        gm.onGameOver.AddListener(ActivateGameOverUI);
    }

    public void PlayButtonHandler () 
    {
        AudioManager.Instance?.PlayButtonClickSfx();
        gm.StartGame();
        startMenuUi.SetActive(false);
    }

    public void ActivateGameOverUI () 
    {
        gameOverUi.SetActive(true);
        gameOverScoreUI.text = "Score: " + gm.PrettyScore();
        gameOverHighscoreUI.text = "Highscore: " + gm.PrettyHighscore();
    }

    private void OnGUI()
    {
        scoreUI.text = gm.PrettyScore();
    }
}
