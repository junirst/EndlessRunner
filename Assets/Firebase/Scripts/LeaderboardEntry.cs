using System;

[System.Serializable]
public class LeaderboardEntry
{
    public string Name;
    public int Score;
    public long Timestamp;
    public string Game;
    public string Scene;

    public string Breakdown;

    public bool IsPlayer { get; set; }

    public LeaderboardEntry() { }

    public LeaderboardEntry(string name, int score, long timestamp)
    {
        Name = name;
        Score = score;
        Timestamp = timestamp;
    }

    public LeaderboardEntry(string name, int score, long timestamp, string game, string scene)
        : this(name, score, timestamp)
    {
        Game = game;
        Scene = scene;
    }

    public LeaderboardEntry(string name, int score, long timestamp, bool isPlayer) : this(name, score, timestamp)
    {
        IsPlayer = isPlayer;
    }
}
