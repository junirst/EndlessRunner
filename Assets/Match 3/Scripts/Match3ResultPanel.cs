using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Match3ResultPanel : MonoBehaviour
{
    private const int TotalLevels = 6;
    private const int ResultCanvasSortingOrder = 200;
    private const float CanvasWidth = 960f;
    private const float CanvasHeight = 540f;
    private const float PanelWidth = 620f;
    private const float ResultPanelHeight = 340f;
    private const string ResultCanvasName = "Match 3 Result Canvas";
    private const string WinPanelName = "Win Panel";
    private const string LostPanelName = "Lost Panel";
    private const string LevelSelectSceneName = "LevelSelect";
    private const string StarText = "★★★";
    private const string HollowStarText = "☆☆☆";
    private static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color WinPanelColor = new Color(0.05f, 0.34f, 0.32f, 0.98f);
    private static readonly Color LostPanelColor = new Color(0.38f, 0.11f, 0.16f, 0.98f);
    private static readonly Color ButtonColor = new Color(0.08f, 0.53f, 0.72f, 1f);
    private static readonly Color ScoreColor = new Color(1f, 0.9f, 0.48f, 1f);
    private static readonly Color FilledStarColor = new Color(1f, 0.82f, 0.2f, 1f);
    private static readonly Color HollowStarColor = new Color(0.65f, 0.68f, 0.72f, 1f);

    private static Sprite defaultUiSprite;

    private Board board;
    private GoalManager goalManager;
    private Match3ScoreManager scoreManager;
    private EndGameManager endGameManager;
    private GameObject winPanel;
    private GameObject lostPanel;
    private GameObject resultCanvasObject;
    private Text winScoreText;
    private Text lostScoreText;
    private Text winStarsText;
    private Text lostStarsText;
    private bool resultShown;
    private bool gameplayInitialized;
    private bool introDismissed;
    private static bool sceneHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        if (sceneHookRegistered)
        {
            return;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        sceneHookRegistered = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMatch3GameplayScene(scene.name) || FindObjectOfType<Match3ResultPanel>() != null)
        {
            return;
        }

        Board sceneBoard = FindObjectOfType<Board>();
        if (sceneBoard != null)
        {
            sceneBoard.gameObject.AddComponent<Match3ResultPanel>();
        }
    }

    private static bool IsMatch3GameplayScene(string sceneName)
    {
        if (sceneName == "Main")
        {
            return true;
        }

        if (!sceneName.StartsWith("Level"))
        {
            return false;
        }

        return int.TryParse(sceneName.Substring("Level".Length), out int levelNumber) && levelNumber >= 1 && levelNumber <= TotalLevels;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        resultShown = false;
        gameplayInitialized = false;
        board = FindObjectOfType<Board>();
        goalManager = FindObjectOfType<GoalManager>();
        scoreManager = FindObjectOfType<Match3ScoreManager>();
        endGameManager = FindObjectOfType<EndGameManager>();
        introDismissed = GameObject.Find("Top UI/Fade Panel") == null;
        EnsureEventSystem();
        BuildResultInterface();
    }

    private void Update()
    {
        if (resultShown || board == null || !introDismissed)
        {
            return;
        }

        if (!gameplayInitialized)
        {
            gameplayInitialized = IsGameplayInitialized();
            if (!gameplayInitialized)
            {
                return;
            }
        }

        if (goalManager != null && goalManager.AreAllGoalsComplete)
        {
            ShowResult(true);
            return;
        }

        if (endGameManager != null && endGameManager.IsDepleted)
        {
            ShowResult(false);
        }
    }

    private bool IsGameplayInitialized()
    {
        bool hasGoals = goalManager != null && goalManager.levelGoals != null && goalManager.levelGoals.Length > 0;
        bool hasCounter = endGameManager != null && endGameManager.requirements != null && endGameManager.requirements.counterValue > 0 && endGameManager.currentCounterValue > 0;
        return board != null && board.currentState == GameState.move && hasGoals && hasCounter;
    }

    /// <summary>Allows result evaluation after the goal-intro panel has been dismissed.</summary>
    public void SetIntroDismissed()
    {
        introDismissed = true;
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void BuildResultInterface()
    {
        resultCanvasObject = new GameObject(ResultCanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        resultCanvasObject.SetActive(false);
        Canvas canvas = resultCanvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ResultCanvasSortingOrder;

        CanvasScaler canvasScaler = resultCanvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
        canvasScaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = resultCanvasObject.GetComponent<RectTransform>();
        SetFullScreen(canvasRect);

        GameObject shade = CreateImage("Result Shade", resultCanvasObject.transform, ShadeColor, Vector2.zero, new Vector2(CanvasWidth, CanvasHeight));
        SetFullScreen(shade.GetComponent<RectTransform>());

        winPanel = CreateResultPanel(WinPanelName, resultCanvasObject.transform, WinPanelColor, "LEVEL COMPLETE!", true, out winScoreText, out winStarsText);
        lostPanel = CreateResultPanel(LostPanelName, resultCanvasObject.transform, LostPanelColor, "LEVEL FAILED", false, out lostScoreText, out lostStarsText);
        winPanel.SetActive(false);
        lostPanel.SetActive(false);
        resultCanvasObject.SetActive(false);
    }

    private GameObject CreateResultPanel(string panelName, Transform parent, Color panelColor, string title, bool isWinPanel, out Text scoreText, out Text starsText)
    {
        GameObject panel = CreateImage(panelName, parent, panelColor, Vector2.zero, new Vector2(PanelWidth, ResultPanelHeight));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText("Title", panel.transform, title, 34, Color.white, new Vector2(0f, 125f), new Vector2(560f, 48f), FontStyle.Bold);
        string initialStars = isWinPanel ? StarText : HollowStarText;
        Color initialStarColor = isWinPanel ? FilledStarColor : HollowStarColor;
        starsText = CreateText("Stars", panel.transform, initialStars, 54, initialStarColor, new Vector2(0f, 62f), new Vector2(560f, 70f), FontStyle.Bold);
        scoreText = CreateText("Score", panel.transform, "SCORE: 0", 30, ScoreColor, new Vector2(0f, -18f), new Vector2(560f, 48f), FontStyle.Bold);
        CreateButton("Back To Level Button", panel.transform, "BACK TO LEVEL", new Vector2(0f, -100f), OpenLevelSelect);
        return panel;
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color, Vector2 position, Vector2 size)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = imageObject.GetComponent<Image>();
        image.sprite = GetDefaultUiSprite();
        image.color = color;
        return imageObject;
    }

    private static Sprite GetDefaultUiSprite()
    {
        if (defaultUiSprite != null)
        {
            return defaultUiSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        defaultUiSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        return defaultUiSprite;
    }

    private Text CreateText(string objectName, Transform parent, string content, int fontSize, Color color, Vector2 position, Vector2 size, FontStyle fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private void CreateButton(string objectName, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImage(objectName, parent, ButtonColor, position, new Vector2(160f, 46f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        CreateText("Label", buttonObject.transform, label, 18, Color.white, Vector2.zero, new Vector2(150f, 42f), FontStyle.Bold);
    }

    private void SetFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private void ShowResult(bool didWin)
    {
        resultShown = true;
        int score = scoreManager != null ? scoreManager.score : 0;
        int stars = didWin ? Match3Progress.GetStarsForScore(score) : 0;

        if (board != null)
        {
            board.currentState = didWin ? GameState.win : GameState.lose;
        }

        if (didWin)
        {
            winScoreText.text = "SCORE: " + score;
            winStarsText.text = BuildStarText(stars);
        }
        else
        {
            lostScoreText.text = "SCORE: " + score;
            lostStarsText.text = HollowStarText;
        }

        resultCanvasObject.SetActive(true);
        winPanel.SetActive(didWin);
        lostPanel.SetActive(!didWin);
        Time.timeScale = 0f;
    }

    private string BuildStarText(int filledStars)
    {
        int clampedStars = Mathf.Clamp(filledStars, 0, 3);
        return new string('★', clampedStars) + new string('☆', 3 - clampedStars);
    }

    private void OpenLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelSelectSceneName);
    }
}
