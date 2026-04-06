using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ShooterMenuManager : MonoBehaviour
{
    public void ChangeScene(string name)
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        ShooterAudioManager.Instance?.StopBgm();
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
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
}
