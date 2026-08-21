using System;
using System.Collections.Generic;
using UnityEngine;

public class MiniGolfTotalStarsManager : MonoBehaviour
{
    private const string TotalStarsKey = "MiniGolf_TotalStars";
    private const string LevelStarsPrefix = "MiniGolf_LevelStars_";

    public static MiniGolfTotalStarsManager Instance { get; private set; }

    [Tooltip("Scene names to track for mini-golf level stars. Edit in Inspector or at runtime.")]
    public List<string> KnownLevelNames = new List<string>
    {
        "MiniGolf-Level1",
        "MiniGolf-Level2",
        "MiniGolf-Level3",
        "MiniGolf-Level4",
        "MiniGolf-Level5"
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
    }

    // --- Static compatibility wrappers so existing calls continue to work ---
    public static int GetTotalStars()
    {
        return PlayerPrefs.GetInt(TotalStarsKey, 0);
    }

    public static int GetStarsForLevel(string sceneName)
    {
        if (Instance != null)
            return Instance.GetStarsForLevelInstance(sceneName);

        if (string.IsNullOrEmpty(sceneName)) return 0;
        return PlayerPrefs.GetInt(LevelStarsPrefix + sceneName, 0);
    }

    public static void RegisterLevelStars(string sceneName, int earnedStars)
    {
        if (Instance != null)
        {
            Instance.RegisterLevelStarsInstance(sceneName, earnedStars);
            return;
        }

        // fallback if no instance exists
        if (string.IsNullOrEmpty(sceneName)) return;
        int previousBest = PlayerPrefs.GetInt(LevelStarsPrefix + sceneName, 0);
        if (earnedStars <= previousBest) return;
        PlayerPrefs.SetInt(LevelStarsPrefix + sceneName, earnedStars);
        int totalStars = PlayerPrefs.GetInt(TotalStarsKey, 0);
        PlayerPrefs.SetInt(TotalStarsKey, totalStars + (earnedStars - previousBest));
        PlayerPrefs.Save();
    }

    public static void ResetAllStars()
    {
        if (Instance != null)
        {
            Instance.ResetAllStarsInstance();
            return;
        }

        PlayerPrefs.DeleteKey(TotalStarsKey);
        // no access to known list if no instance; best effort: do nothing else
        PlayerPrefs.Save();
    }

    // --- Instance implementations ---
    private int GetStarsForLevelInstance(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return 0;
        return PlayerPrefs.GetInt(GetLevelKey(sceneName), 0);
    }

    private void RegisterLevelStarsInstance(string sceneName, int earnedStars)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        int previousBest = GetStarsForLevelInstance(sceneName);
        if (earnedStars <= previousBest) return;
        PlayerPrefs.SetInt(GetLevelKey(sceneName), earnedStars);
        int totalStars = GetTotalStars();
        PlayerPrefs.SetInt(TotalStarsKey, totalStars + (earnedStars - previousBest));
        PlayerPrefs.Save();
    }

    private void ResetAllStarsInstance()
    {
        PlayerPrefs.DeleteKey(TotalStarsKey);
        foreach (string levelName in KnownLevelNames)
        {
            PlayerPrefs.DeleteKey(GetLevelKey(levelName));
        }
        PlayerPrefs.Save();
    }

    public void SetKnownLevels(string[] levels)
    {
        KnownLevelNames = new List<string>(levels ?? Array.Empty<string>());
    }

    public void AddKnownLevel(string level)
    {
        if (string.IsNullOrEmpty(level)) return;
        if (!KnownLevelNames.Contains(level)) KnownLevelNames.Add(level);
    }

    private string GetLevelKey(string sceneName)
    {
        return LevelStarsPrefix + sceneName;
    }
}
