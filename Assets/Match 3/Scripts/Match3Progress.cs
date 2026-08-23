using UnityEngine;

public static class Match3Progress
{
    private const int FirstStarThreshold = 300;
    private const int SecondStarThreshold = 600;
    private const int ThirdStarThreshold = 1000;
    private const int TotalLevels = 6;
    private const string LevelScoreKeyPrefix = "Match3LevelScore_";
    private const string TotalScoreKey = "Match3TotalScore";

    /// <summary>Returns the saved best score for a Match 3 level.</summary>
    public static int GetLevelScore(int levelNumber)
    {
        if (levelNumber < 1)
        {
            return 0;
        }

        return PlayerPrefs.GetInt(LevelScoreKeyPrefix + levelNumber, 0);
    }

    /// <summary>Saves a level score only when it improves the existing best score.</summary>
    public static void SaveLevelScore(int levelNumber, int score)
    {
        if (levelNumber < 1)
        {
            return;
        }

        int bestScore = Mathf.Max(GetLevelScore(levelNumber), score);
        PlayerPrefs.SetInt(LevelScoreKeyPrefix + levelNumber, bestScore);
        SaveTotalScore(CalculateTotalScore(TotalLevels));
        PlayerPrefs.Save();
    }

    /// <summary>Returns and persists the total of all saved Match 3 level scores.</summary>
    public static int GetTotalScore(int totalLevels)
    {
        int totalScore = CalculateTotalScore(totalLevels);
        SaveTotalScore(totalScore);
        PlayerPrefs.Save();
        return totalScore;
    }

    /// <summary>Returns the last persisted cumulative Match 3 score.</summary>
    public static int GetSavedTotalScore()
    {
        return PlayerPrefs.GetInt(TotalScoreKey, CalculateTotalScore(TotalLevels));
    }

    /// <summary>Builds the per-level score breakdown stored with a cumulative leaderboard entry.</summary>
    public static string GetScoreBreakdown(int totalLevels)
    {
        string breakdown = string.Empty;
        for (int levelNumber = 1; levelNumber <= totalLevels; levelNumber++)
        {
            if (levelNumber > 1)
            {
                breakdown += " | ";
            }

            breakdown += "L" + levelNumber + ": " + GetLevelScore(levelNumber);
        }

        return breakdown;
    }

    private static int CalculateTotalScore(int totalLevels)
    {
        int totalScore = 0;
        for (int levelNumber = 1; levelNumber <= totalLevels; levelNumber++)
        {
            totalScore += GetLevelScore(levelNumber);
        }

        return totalScore;
    }

    private static void SaveTotalScore(int totalScore)
    {
        PlayerPrefs.SetInt(TotalScoreKey, totalScore);
    }

    /// <summary>Converts a level score into a one-to-three-star rating.</summary>
    public static int GetStarsForScore(int score)
    {
        if (score >= ThirdStarThreshold)
        {
            return 3;
        }

        if (score >= SecondStarThreshold)
        {
            return 2;
        }

        if (score >= FirstStarThreshold)
        {
            return 1;
        }

        return 0;
    }
}
