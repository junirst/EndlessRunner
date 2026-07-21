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
        if (Instance == null)
        {
            Instance = this;
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    public float currentScore = 0f;

    [SerializeField] private float scoreMultiplier = 1f;
    [SerializeField] private float scoreMultiplierTimeRemaining = 0f;
    [SerializeField] private AudioClip gameOverSfx;

    public Data data;
    public bool isPlaying = false;
    public bool isPaused = false;

    public UnityEvent onPlay = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent onPause = new UnityEvent();
    public UnityEvent onResume = new UnityEvent();

    private void Start() 
    {
        if (data == null)
        {
            LoadData();
        }
    }

    private void LoadData()
    {
        string loadedData = SaveSystem.Load("save");
        if (!string.IsNullOrEmpty(loadedData))
        {
            data = JsonUtility.FromJson<Data>(loadedData);
        }

        if (data == null)
        {
            data = new Data();
        }
    }

    private void Update() 
    {
        if (scoreMultiplierTimeRemaining > 0f)
        {
            scoreMultiplierTimeRemaining -= Time.deltaTime;

            if (scoreMultiplierTimeRemaining <= 0f)
            {
                ResetScoreMultiplier();
            }
        }

        if (isPlaying) 
        {
            currentScore += Time.deltaTime * scoreMultiplier;
        }
    }
    public void StartGame () 
    {
        ResetScoreMultiplier();
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
            PauseManager.Instance?.Resume();
        }

        if (data == null)
        {
            LoadData();
        }

        if (data.highscore < currentScore) 
        {
            data.highscore = currentScore;
            string saveString = JsonUtility.ToJson(data);
            SaveSystem.Save("save", saveString);
        }
        isPlaying = false;
        ResetScoreMultiplier();
        AudioManager.Instance?.StopBgm();
        AudioManager.Instance?.PlayGameOverSfx(gameOverSfx);
        onGameOver.Invoke();
    }

    public void AddScore(float amount)
    {
        currentScore += amount;
    }

    public void ApplyScoreMultiplier(float multiplier, float duration)
    {
        if (multiplier <= 1f || duration <= 0f)
        {
            return;
        }

        scoreMultiplier = Mathf.Max(scoreMultiplier, multiplier);
        scoreMultiplierTimeRemaining = Mathf.Max(scoreMultiplierTimeRemaining, duration);
    }

    private void ResetScoreMultiplier()
    {
        scoreMultiplier = 1f;
        scoreMultiplierTimeRemaining = 0f;
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
        onResume.Invoke();
    }

    public string PrettyScore () 
    {
        return Mathf.RoundToInt(currentScore).ToString();
    }

    public string PrettyHighscore () 
    {
        if (data == null)
        {
            return "0";
        }

        return Mathf.RoundToInt(data.highscore).ToString();
    }
}
