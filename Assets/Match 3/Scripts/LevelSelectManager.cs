using System.Collections;
using UnityEngine;

using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectManager : MonoBehaviour
{
    private void Awake()
    {
        Font defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        Text[] textElements = GetComponentsInChildren<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.font == null)
            {
                textElement.font = defaultFont;
            }
        }

        RefreshProgressDisplay();
        StartCardSlideIn();
    }

    private const int TotalLevels = 6;
    private const int Columns = 3;
    private const float CanvasWidth = 960f;
    private const float CanvasHeight = 540f;
    private const float CardSlideDistance = 260f;
    private const float CardSlideDuration = 0.45f;
    private const float CardSlideStagger = 0.08f;
    private const string Match3ScenePathPrefix = "Assets/Match 3/Scenes/";
    private const string Match3MenuScenePath = "Assets/Match 3/Scenes/Menu.unity";
    private const string Match3LevelSelectScenePath = "Assets/Match 3/Scenes/LevelSelect.unity";

    private const string Match3LeaderboardBoardKey = "match3";

    public Sprite filledStarSprite;
    public Sprite blankStarSprite;
    private bool cardSlideInStarted;


    private void Start()
    {
        RefreshProgressDisplay();
        EnsureLeaderboardButton();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateLevelSelectInterface()
    {
        if (SceneManager.GetActiveScene().name != "LevelSelect" || GameObject.Find("Level Select Canvas") != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("Level Select Manager");
        LevelSelectManager manager = managerObject.AddComponent<LevelSelectManager>();
        manager.BuildInterface();
    }

    /// <summary>Selects a level and opens its corresponding Match 3 scene.</summary>
    public void SelectLevel(int levelNumber)
    {
        PlayerPrefs.SetInt("Match3SelectedLevel", levelNumber);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        string sceneName = levelNumber >= 1 && levelNumber <= TotalLevels ? "Level" + levelNumber : "Main";
        SceneManager.LoadScene(Match3ScenePathPrefix + sceneName + ".unity");
    }

    /// <summary>Returns to the existing Match 3 menu scene.</summary>
    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(Match3MenuScenePath);
    }

    /// <summary>Opens the level selection scene from the Match 3 menu.</summary>
    public void OpenLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(Match3LevelSelectScenePath);
    }

    private void BuildInterface()
    {
        CreateEventSystem();

        GameObject canvasObject = new GameObject("Level Select Canvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        CreateBackground(canvasObject.transform);
        CreateTopBar(canvasObject.transform);
        CreateLevelCards(canvasObject.transform);
        CreateButton(canvasObject.transform, "BACK", new Vector2(-385f, -235f), new Vector2(150f, 44f), BackToMenu, new Color(0.95f, 0.37f, 0.12f, 1f));
        RefreshProgressDisplay();
        StartCardSlideIn();
    }

    private void EnsureLeaderboardButton()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        Transform existingButton = canvas.transform.Find("LEADERBOARD Button");
        if (existingButton == null)
        {
            CreateButton(canvas.transform, "LEADERBOARD", new Vector2(385f, -225f), new Vector2(190f, 44f), OpenLeaderboard, new Color(0.09f, 0.53f, 0.72f, 1f));
        }
    }

    /// <summary>Opens the Match 3 leaderboard with the cumulative saved score.</summary>
    public void OpenLeaderboard()
    {
        Match3Progress.GetTotalScore(TotalLevels);
        LeaderboardManager.EnsureInstance();
        if (LeaderboardManager.Instance == null)
        {
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            return;
        }

        LeaderboardUI.Show(canvas, Match3LeaderboardBoardKey, Match3Progress.GetSavedTotalScore(), Match3Progress.GetScoreBreakdown(TotalLevels));
    }

    private void StartCardSlideIn()
    {
        if (cardSlideInStarted)
        {
            return;
        }

        RectTransform[] cardTransforms = GetComponentsInChildren<RectTransform>(true);
        bool hasCards = false;
        foreach (RectTransform cardTransform in cardTransforms)
        {
            if (cardTransform.name.StartsWith("Level "))
            {
                hasCards = true;
                break;
            }
        }

        if (!hasCards)
        {
            return;
        }

        cardSlideInStarted = true;
        StartCoroutine(AnimateLevelCards(cardTransforms));
    }

    private IEnumerator AnimateLevelCards(RectTransform[] transforms)
    {
        RectTransform[] cards = new RectTransform[TotalLevels];
        Vector2[] targetPositions = new Vector2[TotalLevels];
        int cardCount = 0;

        foreach (RectTransform cardTransform in transforms)
        {
            if (!cardTransform.name.StartsWith("Level ") || !int.TryParse(cardTransform.name.Substring("Level ".Length), out int levelNumber) || levelNumber < 1 || levelNumber > TotalLevels)
            {
                continue;
            }

            int index = levelNumber - 1;
            cards[index] = cardTransform;
            targetPositions[index] = cardTransform.anchoredPosition;
            cardTransform.anchoredPosition = targetPositions[index] + Vector2.up * CardSlideDistance;
            cardCount++;
        }

        if (cardCount == 0)
        {
            yield break;
        }

        float elapsed = 0f;
        float totalDuration = CardSlideDuration + CardSlideStagger * (TotalLevels - 1);
        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index] == null)
                {
                    continue;
                }

                float cardTime = Mathf.Clamp01((elapsed - CardSlideStagger * index) / CardSlideDuration);
                float easedTime = 1f - Mathf.Pow(1f - cardTime, 3f);
                cards[index].anchoredPosition = Vector2.LerpUnclamped(targetPositions[index] + Vector2.up * CardSlideDistance, targetPositions[index], easedTime);
            }

            yield return null;
        }

        for (int index = 0; index < cards.Length; index++)
        {
            if (cards[index] != null)
            {
                cards[index].anchoredPosition = targetPositions[index];
            }
        }
    }

    private void RefreshProgressDisplay()
    {
        int totalScore = Match3Progress.GetTotalScore(TotalLevels);
        Text[] textElements = GetComponentsInChildren<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.name == "Score")
            {
                textElement.text = totalScore.ToString();
            }
        }

        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in transforms)
        {
            if (!child.name.StartsWith("Level "))
            {
                continue;
            }

            string levelNumberText = child.name.Substring("Level ".Length);
            if (!int.TryParse(levelNumberText, out int levelNumber))
            {
                continue;
            }

            int stars = Match3Progress.GetStarsForScore(Match3Progress.GetLevelScore(levelNumber));
            Text starsText = child.Find("Stars")?.GetComponent<Text>();
            if (starsText != null)
            {
                starsText.text = BuildStarText(stars);
            }

            Image[] images = child.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image.transform.name.StartsWith("Star "))
                {
                    string starIndexText = image.transform.name.Substring("Star ".Length);
                    if (int.TryParse(starIndexText, out int starIndex))
                    {
                        image.sprite = starIndex < stars ? filledStarSprite : blankStarSprite;
                        image.color = starIndex < stars ? Color.white : new Color(0.55f, 0.58f, 0.68f, 1f);
                    }
                }
            }
        }
    }

    private string BuildStarText(int stars)
    {
        string result = string.Empty;
        for (int index = 0; index < 3; index++)
        {
            result += index < stars ? "★" : "☆";
            if (index < 2)
            {
                result += " ";
            }
        }

        return result;
    }

    private void CreateEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        StandaloneInputModule inputModule = eventSystemObject.AddComponent<StandaloneInputModule>();
        inputModule.horizontalAxis = "Horizontal";
        inputModule.verticalAxis = "Vertical";
        inputModule.submitButton = "Submit";
        inputModule.cancelButton = "Cancel";
    }

    private void CreateBackground(Transform parent)
    {
        GameObject backgroundObject = CreateUiObject("Background", parent, Vector2.zero, new Vector2(CanvasWidth, CanvasHeight));
        Image background = backgroundObject.AddComponent<Image>();
        background.sprite = Resources.Load<Sprite>("match3menu");
        background.color = new Color(0.45f, 0.2f, 0.62f, 1f);
        background.preserveAspect = false;
        background.raycastTarget = false;
    }

    private void CreateTopBar(Transform parent)
    {
        Text title = CreateText("Title", parent, "SELECT LEVEL", new Vector2(0f, 213f), new Vector2(420f, 55f), 38, new Color(0.85f, 1f, 0.98f, 1f));
        title.fontStyle = FontStyle.Bold;
        AddShadow(title, new Color(0.04f, 0.03f, 0.16f, 0.95f), new Vector2(3f, -3f));

        GameObject scoreBadge = CreateUiObject("Score Badge", parent, new Vector2(-350f, 208f), new Vector2(190f, 46f));
        Image scoreImage = scoreBadge.AddComponent<Image>();
        scoreImage.color = new Color(0.09f, 0.53f, 0.72f, 0.96f);
        Outline scoreOutline = scoreBadge.AddComponent<Outline>();
        scoreOutline.effectColor = new Color(0.25f, 1f, 0.96f, 1f);
        scoreOutline.effectDistance = new Vector2(3f, 3f);
        Text scoreText = CreateText("Score", scoreBadge.transform, "123456", Vector2.zero, new Vector2(180f, 40f), 24, Color.white);
        scoreText.fontStyle = FontStyle.Bold;

        GameObject coinBadge = CreateUiObject("Coin Badge", parent, new Vector2(350f, 208f), new Vector2(220f, 46f));
        Image coinImage = coinBadge.AddComponent<Image>();
        coinImage.color = new Color(0.09f, 0.53f, 0.72f, 0.96f);
        Outline coinOutline = coinBadge.AddComponent<Outline>();
        coinOutline.effectColor = new Color(0.25f, 1f, 0.96f, 1f);
        coinOutline.effectDistance = new Vector2(3f, 3f);
        Text coinText = CreateText("Coins", coinBadge.transform, "COINS  1234  +", Vector2.zero, new Vector2(210f, 40f), 21, Color.white);
        coinText.fontStyle = FontStyle.Bold;
    }

    private void CreateLevelCards(Transform parent)
    {
        for (int level = 1; level <= TotalLevels; level++)
        {
            int index = level - 1;
            int row = index / Columns;
            int column = index % Columns;
            float x = -190f + column * 190f;
            float y = 90f - row * 155f;
            CreateLevelCard(parent, level, new Vector2(x, y));
        }
    }

    private void CreateLevelCard(Transform parent, int levelNumber, Vector2 position)
    {
        GameObject cardObject = CreateUiObject("Level " + levelNumber, parent, position, new Vector2(145f, 116f));
        Image cardImage = cardObject.AddComponent<Image>();
        cardImage.color = new Color(0.42f, 0.12f, 0.63f, 0.97f);
        Outline cardOutline = cardObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.88f, 0.55f, 1f, 1f);
        cardOutline.effectDistance = new Vector2(4f, 4f);

        Button button = cardObject.AddComponent<Button>();
        button.targetGraphic = cardImage;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors();
        int selectedLevel = levelNumber;
        button.onClick.AddListener(() => SelectLevel(selectedLevel));

        Text numberText = CreateText("Number", cardObject.transform, levelNumber.ToString(), new Vector2(0f, 12f), new Vector2(130f, 52f), 38, Color.white);
        numberText.fontStyle = FontStyle.Bold;
        AddShadow(numberText, new Color(0.14f, 0.02f, 0.2f, 0.95f), new Vector2(2f, -2f));

        Text stars = CreateText("Stars", cardObject.transform, levelNumber <= 3 ? "★ ★ ★" : "★ ★ ☆", new Vector2(0f, -35f), new Vector2(130f, 28f), 18, new Color(0.75f, 1f, 0.24f, 1f));
        stars.fontStyle = FontStyle.Bold;
        stars.raycastTarget = false;
    }

    private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, Color color)
    {
        GameObject buttonObject = CreateUiObject(label + " Button", parent, position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = color;
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.8f, 0.4f, 1f);
        outline.effectDistance = new Vector2(2f, 2f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors();
        button.onClick.AddListener(action);
        Text text = CreateText("Text", buttonObject.transform, label, Vector2.zero, size, 18, Color.white);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        AddShadow(text, new Color(0.15f, 0.02f, 0.02f, 0.9f), new Vector2(2f, -2f));
        return button;
    }

    private GameObject CreateUiObject(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject uiObject = new GameObject(objectName);
        uiObject.transform.SetParent(parent, false);
        RectTransform rectTransform = uiObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        return uiObject;
    }

    private Text CreateText(string objectName, Transform parent, string content, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent, position, size);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private void AddShadow(Graphic graphic, Color color, Vector2 distance)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.72f, 0.55f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.1f;
        return colors;
    }
}
