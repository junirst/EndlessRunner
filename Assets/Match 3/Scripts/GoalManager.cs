using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlankGoal
{
    public int numberNeeded;
    public int numberCollected;
    public Sprite goalSprite;
    public string matchValue;
}

public class GoalManager : MonoBehaviour
{
    private const string BlueGoalTag = "blue dot";
    private const string LegacyBlueCandyTag = "teal dot";

    public BlankGoal[] levelGoals;
    public List<GoalPanel> currentGoals = new List<GoalPanel>();
    public GameObject goalPrefab;
    public GameObject goalIntroParent;
    public GameObject goalGameParent;

    /// <summary>Returns the number of objectives currently completed.</summary>
    public int CompletedGoalCount
    {
        get
        {
            if (levelGoals == null)
            {
                return 0;
            }

            int completedGoals = 0;
            foreach (BlankGoal goal in levelGoals)
            {
                if (goal != null && goal.numberCollected >= goal.numberNeeded)
                {
                    completedGoals++;
                }
            }

            return completedGoals;
        }
    }

    /// <summary>Returns true when at least one objective has been completed.</summary>
    public bool HasCompletedAtLeastOneObjective => CompletedGoalCount > 0;

    /// <summary>Returns true when every configured objective has been completed.</summary>
    public bool AreAllGoalsComplete
    {
        get
        {
            return levelGoals != null && levelGoals.Length > 0 && CompletedGoalCount >= levelGoals.Length;
        }
    }

    // Use this for initialization
    void Start()
    {
        SetupGoals();
    }

    private void SetupGoals()
    {
        if (levelGoals == null || goalPrefab == null)
        {
            return;
        }

        for (int i = 0; i < levelGoals.Length; i++)
        {
            if (goalIntroParent != null)
            {
                GameObject goal = Instantiate(goalPrefab, goalIntroParent.transform.position, Quaternion.identity);
                goal.transform.SetParent(goalIntroParent.transform);
                GoalPanel panel = goal.GetComponent<GoalPanel>();
                ConfigurePanel(panel, levelGoals[i]);
            }

            if (goalGameParent != null)
            {
                GameObject gameGoal = Instantiate(goalPrefab, goalGameParent.transform.position, Quaternion.identity);
                gameGoal.transform.SetParent(goalGameParent.transform);
                GoalPanel panel = gameGoal.GetComponent<GoalPanel>();
                currentGoals.Add(panel);
                ConfigurePanel(panel, levelGoals[i]);
            }
        }
    }

    private void ConfigurePanel(GoalPanel panel, BlankGoal goal)
    {
        if (panel == null || goal == null)
        {
            return;
        }

        panel.thisSprite = goal.goalSprite;
        panel.thisString = "0/" + goal.numberNeeded;
    }

    /// <summary>Updates objective counters and reports completion to the result controller.</summary>
    public void UpdateGoals()
    {
        if (levelGoals == null)
        {
            return;
        }

        for (int i = 0; i < levelGoals.Length; i++)
        {
            if (i < currentGoals.Count && currentGoals[i] != null && currentGoals[i].thisText != null)
            {
                currentGoals[i].thisText.text = Mathf.Min(levelGoals[i].numberCollected, levelGoals[i].numberNeeded) + "/" + levelGoals[i].numberNeeded;
            }
        }
    }

    /// <summary>Adds progress to every objective matching the destroyed piece tag.</summary>
    public void CompareGoal(string goalToCompare)
    {
        if (levelGoals == null)
        {
            return;
        }

        string normalizedTag = goalToCompare == LegacyBlueCandyTag ? BlueGoalTag : goalToCompare;

        foreach (BlankGoal goal in levelGoals)
        {
            if (goal != null && normalizedTag == goal.matchValue)
            {
                goal.numberCollected = Mathf.Min(goal.numberCollected + 1, goal.numberNeeded);
            }
        }
    }
}
