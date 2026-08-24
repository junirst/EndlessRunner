using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Match3PauseController : MonoBehaviour
{
    private const int TotalLevels = 6;
    private const int PauseButtonSortingOrder = -100;
    private const float CanvasWidth = 960f;
    private const float CanvasHeight = 540f;
    private const string ScenePathPrefix = "Assets/Match 3/Scenes/";
    private const string LevelSelectScenePath = ScenePathPrefix + "LevelSelect.unity";
    private const string MenuScenePath = ScenePathPrefix + "Menu.unity";

    private Board board;
    private EndGameManager endGameManager;
    private Match3ScoreManager scoreManager;
    private GameObject pauseButtonCanvasObject;
    private GameObject pauseCanvasObject;
    private GameObject pausePanelObject;
    private Text pauseCounterText;
    private Text pauseScoreText;
    private Match3PrefabUiController prefabUiController;
    private bool pauseShown;
    private string currentSceneName;

    public Sprite panelSprite;
    public Sprite buttonSprite;

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
        if (!IsMatch3GameplayScene(scene.name) || FindObjectOfType<Match3PauseController>() != null)
        {
            return;
        }

        Board sceneBoard = FindObjectOfType<Board>();
        if (sceneBoard != null)
        {
            sceneBoard.gameObject.AddComponent<Match3PauseController>();
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
        currentSceneName = SceneManager.GetActiveScene().name;
        board = FindObjectOfType<Board>();
        endGameManager = FindObjectOfType<EndGameManager>();
        scoreManager = FindObjectOfType<Match3ScoreManager>();
        EnsureEventSystem();
        EnsureEndGameManager();
        UpdateLevelText();
        WireGameplayNavigationButtons();
        BuildPauseButton();
        BuildPauseInterface();
        prefabUiController = FindObjectOfType<Match3PrefabUiController>();
        if (prefabUiController != null)
        {
            prefabUiController.Initialize(this);
        }
        SetPauseButtonVisible(false);
        UpdatePauseStats();
        if (pauseCanvasObject != null)
        {
            pauseCanvasObject.SetActive(false);
        }
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
            StandaloneInputModule inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            inputModule.horizontalAxis = "Horizontal";
            inputModule.verticalAxis = "Vertical";
            inputModule.submitButton = "Submit";
            inputModule.cancelButton = "Cancel";
        }
    }

    private void EnsureEndGameManager()
    {
        if (board == null)
        {
            return;
        }

        EndGameManager sceneEndGameManager = FindObjectOfType<EndGameManager>();
        if (sceneEndGameManager == null && board != null)
        {
            sceneEndGameManager = board.gameObject.AddComponent<EndGameManager>();
        }

        endGameManager = sceneEndGameManager;
        if (endGameManager == null)
        {
            return;
        }
        if (endGameManager.requirements == null)
        {
            endGameManager.requirements = new EndGameRequirements { gameType = GameType.Moves, counterValue = 40 };
        }

        int counterValue = endGameManager.requirements.counterValue > 0 ? endGameManager.requirements.counterValue : 40;
        endGameManager.movesLabel = GameObject.Find("Moves Label");
        endGameManager.timeLabel = GameObject.Find("Time Label");
        endGameManager.counter = FindSceneText("Counter");
        endGameManager.ConfigureForMatch3(endGameManager.requirements.gameType, counterValue);
    }

    private void WireBackToLevelsButton()
    {
        GameObject levelsButtonObject = GameObject.Find("Top UI/Score UI/Levels Button");
        if (levelsButtonObject == null)
        {
            return;
        }

        Button levelsButton = levelsButtonObject.GetComponent<Button>();
        if (levelsButton == null)
        {
            return;
        }

        levelsButton.onClick.RemoveAllListeners();
        levelsButton.onClick.AddListener(OpenLevelSelect);
    }

    private void WireGameplayNavigationButtons()
    {
        WireButton(GameObject.Find("Top UI/Score UI/Main Menu Button"), OpenMainMenu);
        WireButton(GameObject.Find("Top UI/Score UI/Levels Button"), OpenLevelSelect);

        GameObject okButtonObject = GameObject.Find("Top UI/Fade Panel/Panel/OK Button");
        GameObject animationControllerObject = GameObject.Find("Board/Animation Controller");
        FadePanelController fadePanelController = animationControllerObject != null ? animationControllerObject.GetComponent<FadePanelController>() : null;
        if (fadePanelController != null)
        {
            WireButton(okButtonObject, fadePanelController.Okay);
        }
    }

    private Text FindSceneText(string objectName)
    {
        Text[] textElements = FindObjectsOfType<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.name == objectName)
            {
                return textElement;
            }
        }

        return null;
    }

    private void UpdateLevelText()
    {
        int levelNumber = GetCurrentLevelNumber();
        if (levelNumber <= 0 && currentSceneName == "Main")
        {
            levelNumber = PlayerPrefs.GetInt("Match3SelectedLevel", 1);
        }

        if (levelNumber < 1 || levelNumber > TotalLevels)
        {
            levelNumber = 1;
        }

        string levelDisplay = "LEVEL " + levelNumber;
        Text[] textElements = FindObjectsOfType<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.name == "Level Text")
            {
                textElement.text = levelDisplay;
            }
        }
    }

    private int GetCurrentLevelNumber()
    {
        if (!currentSceneName.StartsWith("Level") || !int.TryParse(currentSceneName.Substring("Level".Length), out int levelNumber))
        {
            return 0;
        }

        return levelNumber;
    }

    private void BuildPauseButton()
    {
        pauseButtonCanvasObject = FindSceneObjectByName("Match 3 Pause Button Canvas");
        if (pauseButtonCanvasObject != null)
        {
            ConfigurePauseCanvas(pauseButtonCanvasObject, PauseButtonSortingOrder);
            GameObject authoredButton = FindChildObject(pauseButtonCanvasObject.transform, "Pause Button");
            WireButton(authoredButton, OpenPause);
            pauseButtonCanvasObject.SetActive(true);
            return;
        }

        GameObject topUi = GameObject.Find("Top UI");
        Transform pauseButtonTransform = topUi != null ? topUi.transform.Find("Pause Button") : null;

        if (pauseButtonTransform == null)
        {
            pauseButtonCanvasObject = new GameObject("Match 3 Pause Button Canvas");
            Canvas canvas = pauseButtonCanvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = PauseButtonSortingOrder;
            canvas.overrideSorting = true;

            CanvasScaler scaler = pauseButtonCanvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
            scaler.matchWidthOrHeight = 0.5f;
            pauseButtonCanvasObject.AddComponent<GraphicRaycaster>();

            GameObject pauseButton = CreateButton("Pause Button", pauseButtonCanvasObject.transform, new Vector2(395f, 205f), new Vector2(64f, 52f), new Color(0.05f, 0.55f, 0.7f, 1f), OpenPause);
            Text label = CreateText("Label", pauseButton.transform, "II", Vector2.zero, new Vector2(64f, 52f), 23, Color.white);
            label.fontStyle = FontStyle.Bold;
            return;
        }

        Button existingButton = pauseButtonTransform.GetComponent<Button>();
        if (existingButton == null)
        {
            Image image = pauseButtonTransform.GetComponent<Image>();
            if (image == null)
            {
                image = pauseButtonTransform.gameObject.AddComponent<Image>();
            }

            image.color = new Color(0.05f, 0.55f, 0.7f, 1f);
            image.raycastTarget = true;
            existingButton = pauseButtonTransform.gameObject.AddComponent<Button>();
            existingButton.targetGraphic = image;
        }

        existingButton.interactable = true;
        existingButton.onClick.RemoveAllListeners();
        existingButton.onClick.AddListener(OpenPause);
    }

    private void WireButton(GameObject buttonObject, UnityEngine.Events.UnityAction action)
    {
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.interactable = true;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] sceneObjects = FindObjectsOfType<GameObject>(true);
        foreach (GameObject sceneObject in sceneObjects)
        {
            if (sceneObject.name == objectName)
            {
                return sceneObject;
            }
        }

        return null;
    }

    private GameObject FindChildObject(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        return child != null ? child.gameObject : null;
    }

    private Text FindChildText(Transform parent, string objectName)
    {
        Text[] textElements = parent.GetComponentsInChildren<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.name == objectName)
            {
                return textElement;
            }
        }

        return null;
    }

    private void ConfigurePauseCanvas(GameObject canvasObject, int sortingOrder)
    {
        if (canvasObject == null)
        {
            return;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = canvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvas.overrideSorting = true;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
        scaler.matchWidthOrHeight = 0.5f;
        if (canvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsurePauseFonts()
    {
        if (pauseCanvasObject == null)
        {
            return;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text[] textElements = pauseCanvasObject.GetComponentsInChildren<Text>(true);
        foreach (Text textElement in textElements)
        {
            if (textElement.font == null)
            {
                textElement.font = defaultFont;
            }
        }
    }

    private void BuildPauseInterface()
    {
        pauseCanvasObject = FindSceneObjectByName("Match 3 Pause Canvas");
        if (pauseCanvasObject != null)
        {
            ConfigurePauseCanvas(pauseCanvasObject, 150);
            pausePanelObject = FindChildObject(pauseCanvasObject.transform, "Pause Panel");
            pauseCounterText = FindChildText(pauseCanvasObject.transform, "Counter Text");
            pauseScoreText = FindChildText(pauseCanvasObject.transform, "Score Text");
            if (pausePanelObject != null)
            {
                WireButton(FindChildObject(pauseCanvasObject.transform, "Resume Button"), ResumeGame);
                WireButton(FindChildObject(pauseCanvasObject.transform, "Restart Button"), RestartLevel);
                WireButton(FindChildObject(pauseCanvasObject.transform, "Level Select Button"), OpenLevelSelect);
                WireButton(FindChildObject(pauseCanvasObject.transform, "Main Menu Button"), OpenMainMenu);
                EnsurePauseFonts();
                return;
            }
        }

        if (pauseCanvasObject == null)
        {
            pauseCanvasObject = new GameObject("Match 3 Pause Canvas");
        }

        Canvas canvas = pauseCanvasObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = pauseCanvasObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        canvas.overrideSorting = true;

        CanvasScaler scaler = pauseCanvasObject.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = pauseCanvasObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasWidth, CanvasHeight);
        scaler.matchWidthOrHeight = 0.5f;
        if (pauseCanvasObject.GetComponent<GraphicRaycaster>() == null)
        {
            pauseCanvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject shade = CreateUiObject("Pause Shade", pauseCanvasObject.transform, Vector2.zero, new Vector2(CanvasWidth, CanvasHeight));
        AddImage(shade, null, new Color(0.01f, 0.02f, 0.05f, 0.74f), false);

        pausePanelObject = CreateUiObject("Pause Panel", pauseCanvasObject.transform, Vector2.zero, new Vector2(520f, 410f));
        AddImage(pausePanelObject, panelSprite, new Color(0.05f, 0.48f, 0.62f, 1f), false);
        AddOutline(pausePanelObject, new Color(0.02f, 0.18f, 0.25f, 1f), new Vector2(6f, -6f));

        Text title = CreateText("Title", pausePanelObject.transform, "PAUSED", new Vector2(0f, 145f), new Vector2(460f, 55f), 34, new Color(1f, 0.94f, 0.68f, 1f));
        title.fontStyle = FontStyle.Bold;
        AddShadow(title, new Color(0.01f, 0.08f, 0.12f, 0.9f), new Vector2(3f, -3f));

        pauseCounterText = CreateText("Counter Text", pausePanelObject.transform, "MOVES LEFT: 0", new Vector2(0f, 92f), new Vector2(440f, 38f), 22, Color.white);
        pauseCounterText.fontStyle = FontStyle.Bold;
        AddShadow(pauseCounterText, new Color(0.01f, 0.08f, 0.12f, 0.9f), new Vector2(2f, -2f));

        pauseScoreText = CreateText("Score Text", pausePanelObject.transform, "SCORE: 0", new Vector2(0f, 55f), new Vector2(440f, 38f), 22, Color.white);
        pauseScoreText.fontStyle = FontStyle.Bold;
        AddShadow(pauseScoreText, new Color(0.01f, 0.08f, 0.12f, 0.9f), new Vector2(2f, -2f));

        CreateButtonWithLabel("Resume Button", pausePanelObject.transform, "BACK TO GAME", new Vector2(-135f, -65f), ResumeGame, 17, new Vector2(220f, 50f));
        CreateButtonWithLabel("Restart Button", pausePanelObject.transform, "RESTART", new Vector2(135f, -65f), RestartLevel, 18, new Vector2(220f, 50f));
        CreateButtonWithLabel("Level Select Button", pausePanelObject.transform, "LEVEL SELECT", new Vector2(-135f, -130f), OpenLevelSelect, 15, new Vector2(220f, 50f));
        CreateButtonWithLabel("Main Menu Button", pausePanelObject.transform, "MAIN MENU", new Vector2(135f, -130f), OpenMainMenu, 16, new Vector2(220f, 50f));
    }

    private void UpdatePauseStats()
    {
        if (!pauseShown)
        {
            return;
        }

        if (pauseCounterText != null && endGameManager != null)
        {
            string counterLabel = endGameManager.requirements != null && endGameManager.requirements.gameType == GameType.Time ? "TIME LEFT: " : "MOVES LEFT: ";
            pauseCounterText.text = counterLabel + Mathf.Max(0, endGameManager.currentCounterValue);
        }

        if (pauseScoreText != null)
        {
            int currentScore = scoreManager != null ? scoreManager.score : 0;
            pauseScoreText.text = "SCORE: " + currentScore;
        }
    }

    private void Update()
    {
        UpdatePauseStats();
    }
    /// <summary>Controls whether the pause button is visible above active gameplay UI.</summary>
    public void SetPauseButtonVisible(bool visible)
    {
        if (pauseButtonCanvasObject == null)
        {
            return;
        }

        ConfigurePauseCanvas(pauseButtonCanvasObject, PauseButtonSortingOrder);
        pauseButtonCanvasObject.SetActive(visible && !pauseShown);
    }



    public void OpenPause()
    {
        if (pauseShown)
        {
            return;
        }

        if (board == null)
        {
            board = FindObjectOfType<Board>();
        }

        if (pauseCanvasObject == null)
        {
            BuildPauseInterface();
        }

        pauseShown = true;
        if (board != null)
        {
            board.currentState = GameState.pause;
        }

        if (pauseButtonCanvasObject != null)
        {
            pauseButtonCanvasObject.SetActive(false);
        }

        pauseCanvasObject.SetActive(true);
        if (prefabUiController == null)
        {
            prefabUiController = FindObjectOfType<Match3PrefabUiController>();
        }
        if (prefabUiController != null)
        {
            prefabUiController.ShowPauseScreen();
        }
        Time.timeScale = 0f;
    }

    /// <summary>Closes the pause panel and resumes Match 3 gameplay.</summary>
    public void ResumeGame()
    {
        pauseShown = false;
        if (pauseCanvasObject != null)
        {
            pauseCanvasObject.SetActive(false);
        }

        if (pauseButtonCanvasObject != null)
        {
            pauseButtonCanvasObject.SetActive(true);
        }

        if (prefabUiController != null)
        {
            prefabUiController.HidePauseScreen();
        }

        Time.timeScale = 1f;
        if (board != null && board.currentState == GameState.pause)
        {
            board.currentState = GameState.move;
        }
    }

    /// <summary>Reloads the active Match 3 level from the beginning.</summary>
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(ScenePathPrefix + currentSceneName + ".unity");
    }

    /// <summary>Opens the Match 3 level-select screen.</summary>
    public void OpenLevelSelect()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(LevelSelectScenePath);
    }

    /// <summary>Returns to the Match 3 menu screen.</summary>
    public void OpenMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScenePath);
    }

    private void CreateButtonWithLabel(string objectName, Transform parent, string labelText, Vector2 position, UnityEngine.Events.UnityAction action, int fontSize, Vector2 size)
    {
        GameObject button = CreateButton(objectName, parent, position, size, new Color(0.88f, 0.29f, 0.12f, 1f), action);
        Text label = CreateText("Label", button.transform, labelText, Vector2.zero, size, fontSize, Color.white);
        label.fontStyle = FontStyle.Bold;
        AddShadow(label, new Color(0.25f, 0.04f, 0.01f, 0.85f), new Vector2(2f, -2f));
        label.transform.SetAsLastSibling();
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

    private Image AddImage(GameObject target, Sprite sprite, Color color, bool raycastTarget)
    {
        Image image = target.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.preserveAspect = sprite != null;
        return image;
    }

    private GameObject CreateButton(string objectName, Transform parent, Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent, position, size);
        Image image = AddImage(buttonObject, buttonSprite, color, true);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(action);
        AddOutline(buttonObject, new Color(0.28f, 0.04f, 0.02f, 1f), new Vector2(2f, -2f));
        return buttonObject;
    }

    private Text CreateText(string objectName, Transform parent, string content, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        GameObject textObject = CreateUiObject(objectName, parent, position, size);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        textObject.transform.SetAsLastSibling();
        return text;
    }

    private void AddOutline(GameObject target, Color color, Vector2 distance)
    {
        Outline outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private void AddShadow(Graphic graphic, Color color, Vector2 distance)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }
}
