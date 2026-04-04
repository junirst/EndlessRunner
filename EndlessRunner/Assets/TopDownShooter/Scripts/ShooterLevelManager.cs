using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ShooterLevelManager : MonoBehaviour
{
    public static ShooterLevelManager manager;

    public GameObject deathScreen;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highscoreText;

    public ShooterSaveData data;

    public int score;
    

    private void Awake()
    {
        manager = this;
        ShooterSaveSystem.Initialize();

        data = new ShooterSaveData(0);
    }

    private void Start()
    {
        ShooterAudioManager.Instance?.PlayBgm();
    }

    public void GameOver()
    {
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

    public void ReplayGame()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMenu()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadScene("Menu");
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
