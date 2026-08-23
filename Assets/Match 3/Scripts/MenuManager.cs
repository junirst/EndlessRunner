using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    private const string Match3MenuScenePath = "Assets/Match 3/Scenes/Menu.unity";

    private const string Match3LevelSelectScenePath = "Assets/Match 3/Scenes/LevelSelect.unity";

    public void ChangeScene(string name)
    {
        Time.timeScale = 1f;
        if (name == "Main")
        {
            SceneManager.LoadScene(Match3LevelSelectScenePath);
            return;
        }

        if (name == "Menu")
        {
            SceneManager.LoadScene(Match3MenuScenePath);
            return;
        }

        SceneManager.LoadScene(name);
    }

    /// <summary>Opens the Match 3 level-select scene.</summary>
    public void OpenLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(Match3LevelSelectScenePath);
    }

    /// <summary>Opens the Match 3 pause panel through the scene pause controller.</summary>
    public void OpenPause()
    {
        Match3PauseController pauseController = GetPauseController();
        if (pauseController != null)
        {
            pauseController.OpenPause();
        }
    }

    /// <summary>Resumes Match 3 gameplay through the scene pause controller.</summary>
    public void ResumeGame()
    {
        Match3PauseController pauseController = GetPauseController();
        if (pauseController != null)
        {
            pauseController.ResumeGame();
        }
    }

    /// <summary>Restarts the current Match 3 scene through the scene pause controller.</summary>
    public void RestartLevel()
    {
        Match3PauseController pauseController = GetPauseController();
        if (pauseController != null)
        {
            pauseController.RestartLevel();
        }
    }

    /// <summary>Returns to the Match 3 menu through the scene pause controller.</summary>
    public void OpenMainMenu()
    {
        Match3PauseController pauseController = GetPauseController();
        if (pauseController != null)
        {
            pauseController.OpenMainMenu();
        }
    }

    private Match3PauseController GetPauseController()
    {
        Match3PauseController pauseController = GetComponent<Match3PauseController>();
        if (pauseController == null)
        {
            pauseController = FindObjectOfType<Match3PauseController>();
        }

        return pauseController;
    }

    /// <summary>Returns to the title screen.</summary>
    public void BackToTitleScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}
