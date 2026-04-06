using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    public void LoadLevel (string levelName)
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);
    }

    public void ReloadLevel()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToTitleScreen()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
    }

}
