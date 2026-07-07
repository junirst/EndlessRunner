using System;
using UnityEngine;

public static class MiniGolfTotalStarsManager
{
    private const string TotalStarsKey = "MiniGolf_TotalStars";
    private const string LevelStarsPrefix = "MiniGolf_LevelStars_";

    private static readonly string[] KnownLevelNames =
    {
        "MiniGolf-Level1",
        "MiniGolf-Level2",
        "MiniGolf-Level3",
        "MiniGolf-Level4",
        "MiniGolf-Level5"
    };

    public static int GetTotalStars()
    {
        return PlayerPrefs.GetInt(TotalStarsKey, 0);
    }

    public static int GetStarsForLevel(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return 0;
        }

        return PlayerPrefs.GetInt(GetLevelKey(sceneName), 0);
    }

    public static void RegisterLevelStars(string sceneName, int earnedStars)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        int previousBest = GetStarsForLevel(sceneName);
        if (earnedStars <= previousBest)
        {
            return;
        }

        PlayerPrefs.SetInt(GetLevelKey(sceneName), earnedStars);

        int totalStars = GetTotalStars();
        PlayerPrefs.SetInt(TotalStarsKey, totalStars + (earnedStars - previousBest));
        PlayerPrefs.Save();
    }

    public static void ResetAllStars()
    {
        PlayerPrefs.DeleteKey(TotalStarsKey);

        foreach (string levelName in KnownLevelNames)
        {
            PlayerPrefs.DeleteKey(GetLevelKey(levelName));
        }

        PlayerPrefs.Save();
    }

    private static string GetLevelKey(string sceneName)
    {
        return LevelStarsPrefix + sceneName;
    }
}
