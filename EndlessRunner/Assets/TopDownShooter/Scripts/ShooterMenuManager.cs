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
        SceneManager.LoadScene(name);
    }

    public void BackToTitleScreen()
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        ShooterAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene("TitleScreen");
    }
}
