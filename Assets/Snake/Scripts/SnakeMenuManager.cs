using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SnakeMenuManager : MonoBehaviour
{
    [Header("Stage Cards")]
    [SerializeField] private StageCard[] stageCards;
    [SerializeField] private Sprite[] stageSprites;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button backButton;

    [Header("Loading")]
    [SerializeField] private LoadingScreen loadingScreen;

    [Header("Snake Divider")]
    [SerializeField] private RectTransform dividerRoot;
    [SerializeField] private int segmentCount = 18;
    [SerializeField] private float segmentSize = 22f;
    [SerializeField] private float segmentGap = 5f;

    private static readonly Color SnakePink = new Color(0.8679245f, 0.5199359f, 0.5663873f);

    private static readonly (string id, string scene)[] StageDefs = new (string, string)[]
    {
        ("Infinite", "Infinite"),
        ("Level 1", "Level 1"),
        ("Level 2", "Level 2"),
        ("Level 3", "Level 3"),
        ("Level 4", "Level 4"),
    };

    private Canvas canvas;
    private TMP_FontAsset buttonFont;

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();

        bool hasValidCards = stageCards != null;
        if (hasValidCards)
        {
            hasValidCards = false;
            for (int i = 0; i < stageCards.Length; i++)
            {
                if (stageCards[i] != null) { hasValidCards = true; break; }
            }
        }

        if (!hasValidCards)
        {
            stageCards = FindObjectsOfType<StageCard>();
            if ((stageCards == null || stageCards.Length == 0) && canvas != null)
                BuildStageCardsUI();
        }

        if (backButton == null)
            backButton = GetComponentInChildren<Button>(true);

        if (loadingScreen == null)
        {
            loadingScreen = GetComponentInChildren<LoadingScreen>(true);
            if (loadingScreen == null && canvas != null)
                BuildLoadingScreen();
        }
    }

    private void Start()
    {
        LoadHighScores();
        SetupStageCards();
        FetchLeaderboardHighScores();
        GenerateSnakeDivider();
        SnakeAudioManager.Instance?.PlayBgm();
    }

    private void BuildStageCardsUI()
    {
        FindExistingFont();

        GameObject containerGO = new GameObject("StageCardsContainer", typeof(RectTransform));
        containerGO.transform.SetParent(canvas.transform, false);
        RectTransform containerRT = containerGO.GetComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.5f);
        containerRT.anchorMax = new Vector2(0.5f, 0.5f);
        containerRT.anchoredPosition = new Vector2(0f, 10f);
        containerRT.sizeDelta = new Vector2(900f, 260f);

        HorizontalLayoutGroup hlg = containerGO.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 40f;
        hlg.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter csf = containerGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        List<StageCard> cards = new List<StageCard>();
        for (int i = 0; i < StageDefs.Length; i++)
        {
            StageCard card = CreateStageCard(containerRT, i);
            cards.Add(card);
        }
        stageCards = cards.ToArray();
    }

    private void FindExistingFont()
    {
        if (titleText != null)
        {
            buttonFont = titleText.font;
            return;
        }
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in texts)
        {
            if (t.font != null)
            {
                buttonFont = t.font;
                return;
            }
        }
    }

    private StageCard CreateStageCard(Transform parent, int index)
    {
        string id = StageDefs[index].id;

        GameObject cardGO = new GameObject($"{id}_Card", typeof(RectTransform));
        cardGO.transform.SetParent(parent, false);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(240f, 240f);

        GameObject frameGO = new GameObject("FrameImage", typeof(RectTransform), typeof(Image));
        frameGO.transform.SetParent(cardRT, false);
        RectTransform frameRT = frameGO.GetComponent<RectTransform>();
        frameRT.anchorMin = new Vector2(0.5f, 0.5f);
        frameRT.anchorMax = new Vector2(0.5f, 0.5f);
        frameRT.anchoredPosition = new Vector2(0f, 15f);
        frameRT.sizeDelta = new Vector2(210f, 170f);
        Image frameImage = frameGO.GetComponent<Image>();
        frameImage.color = Color.white;
        frameImage.raycastTarget = false;

        GameObject imageGO = new GameObject("StageImage", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(cardRT, false);
        RectTransform imageRT = imageGO.GetComponent<RectTransform>();
        imageRT.anchorMin = new Vector2(0.5f, 0.5f);
        imageRT.anchorMax = new Vector2(0.5f, 0.5f);
        imageRT.anchoredPosition = new Vector2(0f, 15f);
        imageRT.sizeDelta = new Vector2(200f, 160f);
        Image stageImage = imageGO.GetComponent<Image>();
        stageImage.color = Color.white;
        if (stageSprites != null && index < stageSprites.Length && stageSprites[index] != null)
        {
            stageImage.sprite = stageSprites[index];
        }
        stageImage.raycastTarget = true;

        GameObject hsGO = new GameObject("HighScoreText", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasRenderer));
        hsGO.transform.SetParent(cardRT, false);
        RectTransform hsRT = hsGO.GetComponent<RectTransform>();
        hsRT.anchorMin = new Vector2(0.5f, 1f);
        hsRT.anchorMax = new Vector2(0.5f, 1f);
        hsRT.anchoredPosition = new Vector2(0f, 0f);
        hsRT.sizeDelta = new Vector2(220f, 36f);
        TextMeshProUGUI hsText = hsGO.GetComponent<TextMeshProUGUI>();
        hsText.font = buttonFont;
        hsText.fontSize = 28;
        hsText.alignment = TextAlignmentOptions.Center;
        hsText.color = Color.white;
        hsText.text = "HIGH SCORE: 0";
        hsText.gameObject.SetActive(false);

        GameObject labelGO = new GameObject("StageLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasRenderer));
        labelGO.transform.SetParent(cardRT, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0.5f, 0f);
        labelRT.anchorMax = new Vector2(0.5f, 0f);
        labelRT.anchoredPosition = new Vector2(0f, 20f);
        labelRT.sizeDelta = new Vector2(220f, 36f);
        TextMeshProUGUI labelText = labelGO.GetComponent<TextMeshProUGUI>();
        labelText.font = buttonFont;
        labelText.fontSize = 32;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
        labelText.text = id.ToUpper();

        Button button = cardGO.AddComponent<Button>();
        button.targetGraphic = stageImage;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        colors.selectedColor = new Color(0.9f, 0.9f, 0.9f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;

        StageCard card = cardGO.AddComponent<StageCard>();
        card.Configure(hsText, stageImage, frameImage, button);

        return card;
    }

    private void BuildLoadingScreen()
    {
        GameObject lsGO = new GameObject("LoadingScreen", typeof(RectTransform));
        lsGO.transform.SetParent(canvas.transform, false);
        RectTransform lsRT = lsGO.GetComponent<RectTransform>();
        lsRT.anchorMin = Vector2.zero;
        lsRT.anchorMax = Vector2.one;
        lsRT.offsetMin = Vector2.zero;
        lsRT.offsetMax = Vector2.zero;

        CanvasGroup cg = lsGO.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        Image bg = lsGO.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true;

        GameObject snakeGO = new GameObject("SnakeContainer", typeof(RectTransform));
        snakeGO.transform.SetParent(lsRT, false);
        RectTransform snakeRT = snakeGO.GetComponent<RectTransform>();
        snakeRT.anchorMin = new Vector2(1f, 0f);
        snakeRT.anchorMax = new Vector2(1f, 0f);
        snakeRT.anchoredPosition = new Vector2(-40f, 40f);
        snakeRT.sizeDelta = new Vector2(250f, 60f);
        snakeRT.pivot = new Vector2(1f, 0f);

        LoadingScreen ls = lsGO.AddComponent<LoadingScreen>();
        ls.Configure(cg, snakeRT);

        loadingScreen = ls;
    }

    private void LoadHighScores()
    {
        for (int i = 0; i < stageCards.Length && i < StageDefs.Length; i++)
        {
            int score = SnakeSaveSystem.GetHighScore(StageDefs[i].id);
            stageCards[i].Setup(StageDefs[i].id, StageDefs[i].scene, score);
        }
    }

    private void FetchLeaderboardHighScores()
    {
        LeaderboardManager.EnsureInstance();
        if (LeaderboardManager.Instance == null) return;

        for (int i = 0; i < stageCards.Length && i < StageDefs.Length; i++)
        {
            string boardKey = LeaderboardManager.GetBoardKey(ScoreManager.GameKey, StageDefs[i].id);
            if (!LeaderboardManager.Instance.IsValidBoard(boardKey)) continue;

            int capturedIndex = i;
            LeaderboardManager.Instance.FetchTop(boardKey, 1, entries =>
            {
                if (entries != null && entries.Count > 0)
                    stageCards[capturedIndex].SetLeaderboardTop(entries[0].Name, entries[0].Score);
            });
        }
    }

    private void SetupStageCards()
    {
        for (int i = 0; i < stageCards.Length; i++)
        {
            StageCard card = stageCards[i];
            if (card == null) continue;
            int capturedIndex = i;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => OnStageSelected(capturedIndex));
        }
    }

    public void PlayLevel(string sceneName)
    {
        for (int i = 0; i < StageDefs.Length && i < stageCards.Length; i++)
        {
            if (StageDefs[i].scene == sceneName)
            {
                OnStageSelected(i);
                return;
            }
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private void OnStageSelected(int index)
    {
        if (index < 0 || index >= stageCards.Length || index >= StageDefs.Length) return;

        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        StageCard card = stageCards[index];

        foreach (var c in stageCards)
        {
            if (c != null && c.Button != null)
                c.Button.interactable = false;
        }

        card.PlayConfirmBlink(() =>
        {
            if (backButton != null)
                backButton.interactable = false;
            if (loadingScreen != null)
                loadingScreen.ShowAndLoad(StageDefs[index].scene);
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(StageDefs[index].scene);
            }
        });
    }

    public void BackToTitleScreen()
    {
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        SnakeAudioManager.Instance?.StopBgm();
        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScreen");
    }

    private void GenerateSnakeDivider()
    {
        if (dividerRoot == null) return;

        float totalW = segmentCount * segmentSize + (segmentCount - 1) * segmentGap;
        float startX = -totalW / 2f + segmentSize / 2f;

        for (int i = 0; i < segmentCount; i++)
        {
            GameObject seg = new GameObject($"Seg_{i}", typeof(RectTransform), typeof(Image));
            seg.transform.SetParent(dividerRoot, false);

            RectTransform rt = seg.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(startX + i * (segmentSize + segmentGap), 0);
            rt.sizeDelta = new Vector2(segmentSize, segmentSize);

            Image img = seg.GetComponent<Image>();
            img.color = SnakePink;
            img.raycastTarget = false;
        }
    }
}
