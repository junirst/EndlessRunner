using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ShooterMenuManager : MonoBehaviour
{
    public void ChangeScene(string name)
    {
        ShooterAudioManager.Instance?.PlayButtonClickSfx();
        SceneManager.LoadScene(name);
    }
}
