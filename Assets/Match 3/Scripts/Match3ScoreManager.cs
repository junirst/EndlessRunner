using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Match3ScoreManager : MonoBehaviour
{
    private Board board;
    private int currentLevelNumber;

    public Text scoreText;
    public int score;
    public Image scoreBar;

    // Use this for initialization
    void Start()
    {
        board = FindObjectOfType<Board>();
        currentLevelNumber = GetCurrentLevelNumber();
        score = 0;
        UpdateScoreText();
        UpdateBar();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScoreText();
    }

    private void OnDestroy()
    {
        SaveCurrentLevelScore();
    }

    /// <summary>Adds points to the current level and saves the best level score.</summary>
    public void IncreaseScore(int amountToIncrease)
    {
        score += amountToIncrease;
        SaveCurrentLevelScore();
        UpdateScoreText();
        UpdateBar();
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    private void UpdateBar()
    {
        if (board != null && scoreBar != null && board.scoreGoals != null && board.scoreGoals.Length > 0)
        {
            int highestGoal = board.scoreGoals[board.scoreGoals.Length - 1];
            scoreBar.fillAmount = highestGoal > 0 ? (float)score / highestGoal : 0f;
        }
    }

    private void SaveCurrentLevelScore()
    {
        if (currentLevelNumber > 0)
        {
            Match3Progress.SaveLevelScore(currentLevelNumber, score);
        }
    }

    private int GetCurrentLevelNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (!sceneName.StartsWith("Level"))
        {
            return 0;
        }

        string levelNumberText = sceneName.Substring("Level".Length);
        return int.TryParse(levelNumberText, out int levelNumber) ? levelNumber : 0;
    }
}
