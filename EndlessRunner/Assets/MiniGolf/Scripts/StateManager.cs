using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StateManager : MonoBehaviour
{
    public void LoadLevel (string levelName)
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);
    }

    public void ReloadLevel()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToTitleScreen()
    {
        MiniGolfAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadSceneAsync("TitleScreen", LoadSceneMode.Single);
    }

}
