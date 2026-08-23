using UnityEngine;
using UnityEngine.UI;

public enum GameType
{
    Moves,
    Time
}

[System.Serializable]
public class EndGameRequirements
{
    public GameType gameType;
    public int counterValue;
}

public class EndGameManager : MonoBehaviour
{
    public GameObject movesLabel;
    public GameObject timeLabel;
    public Text counter;
    public EndGameRequirements requirements;
    public int currentCounterValue;

    private float timerSeconds;

    /// <summary>Returns true when the configured move or time counter reaches zero.</summary>
    public bool IsDepleted => currentCounterValue <= 0;

    // Start is called before the first frame update
    void Start()
    {
        if (requirements == null)
        {
            requirements = new EndGameRequirements { gameType = GameType.Moves, counterValue = 40 };
        }

        if (requirements.counterValue <= 0)
        {
            requirements.counterValue = 40;
        }

        if (currentCounterValue <= 0)
        {
            currentCounterValue = requirements.counterValue;
        }

        SetupGame();
    }

    /// <summary>Configures the Match 3 counter for a moves or time-based level.</summary>
    public void ConfigureForMatch3(GameType gameType, int counterValue)
    {
        requirements = new EndGameRequirements { gameType = gameType, counterValue = counterValue };
        currentCounterValue = counterValue;
        timerSeconds = 1f;
        UpdateCounterText();
    }

    private void SetupGame()
    {
        if (requirements.gameType == GameType.Moves)
        {
            if (movesLabel != null)
            {
                movesLabel.SetActive(true);
            }

            if (timeLabel != null)
            {
                timeLabel.SetActive(false);
            }
        }
        else
        {
            timerSeconds = 1f;
            if (movesLabel != null)
            {
                movesLabel.SetActive(false);
            }

            if (timeLabel != null)
            {
                timeLabel.SetActive(true);
            }
        }

        UpdateCounterText();
    }

    /// <summary>Decreases the current counter and leaves the result controller to show the outcome.</summary>
    public void DecreaseCounterValue()
    {
        if (currentCounterValue <= 0)
        {
            return;
        }

        currentCounterValue--;
        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        if (counter != null)
        {
            counter.text = Mathf.Max(0, currentCounterValue).ToString();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (requirements == null || requirements.gameType != GameType.Time || currentCounterValue <= 0)
        {
            return;
        }

        timerSeconds -= Time.deltaTime;
        if (timerSeconds <= 0f)
        {
            DecreaseCounterValue();
            timerSeconds = 1f;
        }
    }
}
