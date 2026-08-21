using System.IO;
using UnityEngine;

[System.Serializable]
public class HighScoresData
{
    public int infinite;
    public int level1;
    public int level2;
}

public static class SnakeSaveSystem
{
    public static readonly string SAVE_FOLDER = Application.persistentDataPath + "/snakesaves/";
    public static readonly string FILE_NAME = "highscores.json";

    public static void Save(HighScoresData data)
    {
        if (!Directory.Exists(SAVE_FOLDER))
            Directory.CreateDirectory(SAVE_FOLDER);
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(SAVE_FOLDER + FILE_NAME, json);
    }

    public static HighScoresData Load()
    {
        string fileLoc = SAVE_FOLDER + FILE_NAME;
        if (File.Exists(fileLoc))
        {
            string json = File.ReadAllText(fileLoc);
            return JsonUtility.FromJson<HighScoresData>(json);
        }
        return null;
    }

    public static int GetHighScore(string stageId)
    {
        HighScoresData data = Load();
        if (data == null) return 0;
        return stageId switch
        {
            "Infinite" => data.infinite,
            "Level1" => data.level1,
            "Level2" => data.level2,
            _ => 0
        };
    }

    public static void SetHighScore(string stageId, int score)
    {
        HighScoresData data = Load();
        if (data == null)
            data = new HighScoresData();
        switch (stageId)
        {
            case "Infinite": if (score > data.infinite) data.infinite = score; break;
            case "Level1": if (score > data.level1) data.level1 = score; break;
            case "Level2": if (score > data.level2) data.level2 = score; break;
        }
        Save(data);
    }
}
