using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScript : MonoBehaviour
{
    [Header("Target Scene Names")]
    [SerializeField] private string cubeDashSceneName = "CubeDash";
    [SerializeField] private string miniGolfSceneName = "MainMenu";
    [SerializeField] private string topDownShooterSceneName = "Menu";

    private void Start()
    {
        TitleAudioManager.Instance?.PlayBgm();
    }

    public void PlayCubeDash()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(cubeDashSceneName);
    }

    public void PlayMiniGolf()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(miniGolfSceneName);
    }

    public void PlayTopDownShooter()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(topDownShooterSceneName);
    }

    public void LoadSceneByName(string sceneName)
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
