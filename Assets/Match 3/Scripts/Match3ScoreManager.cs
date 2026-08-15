using UnityEngine;

public class Match3ScoreManager : MonoBehaviour
{
public static Match3ScoreManager Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    public int Score { get; private set; }
    public int HighScore { get; private set; }
    
    public void AddScore(int points)
    {
        Score += points;
        Debug.Log("Score added: " + points + ", total: " + Score);
    }
    
    public void Reset()
    {
        Score = 0;
    }
    
    public void LoadHighScore()
    {
        // Load high score using PlayerPrefs or JSON serialization
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
    }
    
    public void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", HighScore);
        PlayerPrefs.Save();
    }
}