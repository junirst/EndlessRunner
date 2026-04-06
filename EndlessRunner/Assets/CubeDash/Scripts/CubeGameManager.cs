using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CubeGameManager : MonoBehaviour
{
    #region Singleton

    public static CubeGameManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    #endregion

    public float currentScore = 0f;

    public Data data;
    public bool isPlaying = false;
    public bool isPaused = false;

    public UnityEvent onPlay = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent onPause = new UnityEvent();
    public UnityEvent onResume = new UnityEvent();

    private void Start() 
    {
        string loadedData = SaveSystem.Load("save");
        if (loadedData != null) 
        {
            data = JsonUtility.FromJson<Data>(loadedData);
        }
        else 
        {
            data = new Data();
        }
    }

    private void Update() 
    {
        if (isPlaying && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPlaying) 
        {
            currentScore += Time.deltaTime;
        }
    }
    public void StartGame () 
    {
        onPlay.Invoke();
        isPlaying = true;
        isPaused = false;
        Time.timeScale = 1f;
        currentScore = 0;
        AudioManager.Instance?.PlayBgm();
    }

    public void GameOver () 
    {
        if (isPaused)
        {
            ResumeGame();
        }

        if (data.highscore < currentScore) 
        {
            data.highscore = currentScore;
            string saveString = JsonUtility.ToJson(data);
            SaveSystem.Save("save", saveString);
        }
        isPlaying = false;
        AudioManager.Instance?.StopBgm();
        AudioManager.Instance?.PlayGameOverSfx();
        onGameOver.Invoke();
    }

    public void TogglePause()
    {
        if (!isPlaying)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (!isPlaying || isPaused)
        {
            return;
        }

        isPaused = true;
        isPlaying = false;
        Time.timeScale = 0f;
        onPause.Invoke();
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;
        isPlaying = true;
        Time.timeScale = 1f;
        onResume.Invoke();
    }

    public string PrettyScore () 
    {
        return Mathf.RoundToInt(currentScore).ToString();
    }

    public string PrettyHighscore () 
    {
        return Mathf.RoundToInt(data.highscore).ToString();
    }
}
