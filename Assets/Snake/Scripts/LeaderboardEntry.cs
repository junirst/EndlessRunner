using System;

[System.Serializable]
public class LeaderboardEntry
{
    public string Name;
    public int Score;
    public long Timestamp;

    public bool IsPlayer { get; set; }

    public LeaderboardEntry() { }

    public LeaderboardEntry(string name, int score, long timestamp)
    {
        Name = name;
        Score = score;
        Timestamp = timestamp;
    }

    public LeaderboardEntry(string name, int score, long timestamp, bool isPlayer) : this(name, score, timestamp)
    {
        IsPlayer = isPlayer;
    }
}
