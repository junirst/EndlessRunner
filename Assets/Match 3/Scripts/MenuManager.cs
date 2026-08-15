using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void ChangeScene(string name)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
    }

    public void BackToTitleScreen()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }
}
