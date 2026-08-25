using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    #region Fields

    private const int TopLimit = 10;
    private const int MaxNameLength = 16;

    private string boardKey;
    private int playerScore;
    private string scoreBreakdown;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI listText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private GameObject nameInputRoot;
    [SerializeField] private TMP_InputField nameInput;

    private bool fetching;
    private bool submitted;
    private bool framed;

    /// <summary>
    /// Invoked when the panel is closed, so callers can reveal the screen that
    /// was waiting behind the leaderboard (e.g. the game over screen).
    /// </summary>
    public System.Action onClose;

    #endregion

    #region Builder

    public static LeaderboardUI Build(Canvas canvas, string boardKey, int playerScore)
    {
        if (canvas == null) return null;

        GameObject root = CreatePanel(canvas);
        LeaderboardUI ui = root.GetComponent<LeaderboardUI>();
        ui.boardKey = boardKey;
        ui.playerScore = playerScore;
        ui.FetchAndRender();
        return ui;
    }

    /// <summary>
    /// Builds the full leaderboard hierarchy under the given canvas
    /// without touching the network. Used at runtime by <see cref="Build"/>
    /// and by the editor tool to generate the reusable prefab.
    /// </summary>
    public static GameObject CreatePanel(Canvas canvas)
    {
        if (canvas == null) return null;

        GameObject prefab = LeaderboardManager.Instance?.LeaderboardPrefab;
        if (prefab != null)
        {
            GameObject root = Instantiate(prefab, canvas.transform, false);
            root.name = "LeaderboardUI";
            root.GetComponent<LeaderboardUI>().EnsureRefs();
            return root;
        }

        return CreateProceduralPanel(canvas);
    }

    /// <summary>
    /// Builds the leaderboard hierarchy from code, ignoring any prefab.
    /// Used by the editor tool to generate the reusable prefab asset.
    /// </summary>
    public static GameObject CreateProceduralPanel(Canvas canvas)
    {
        if (canvas == null) return null;

        GameObject go = new GameObject("LeaderboardUI", typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);

        go.AddComponent<LeaderboardUI>().BuildPanel();
        return go;
    }

    /// <summary>
    /// Shows the leaderboard, reusing an existing instance in the scene if one
    /// exists (e.g. a prefab placed in the hierarchy), otherwise building one.
    /// </summary>
    public static LeaderboardUI Show(Canvas canvas, string boardKey, int playerScore, string scoreBreakdown = null)
    {
        if (canvas == null) return null;

        LeaderboardUI ui = null;
        foreach (LeaderboardUI existing in canvas.GetComponentsInChildren<LeaderboardUI>(true))
        {
            ui = existing;
            break;
        }

        if (ui == null)
        {
            GameObject root = CreatePanel(canvas);
            ui = root.GetComponent<LeaderboardUI>();
        }

        ui.EnsureRefs();

        ui.ApplyPrefabStyle();

        if (ui.nameInput != null)
            ui.nameInput.text = "";

        ui.gameObject.SetActive(true);
        ui.canvasGroup.alpha = 1f;
        ui.canvasGroup.blocksRaycasts = true;
        ui.canvasGroup.interactable = true;
        ui.submitted = false;
        ui.boardKey = boardKey;
        ui.playerScore = playerScore;
        ui.scoreBreakdown = scoreBreakdown ?? string.Empty;
        ui.fetching = false;
        ui.FetchAndRender();
        return ui;
    }

    /// <summary>
    /// Shows the leaderboard in front of a game's end screen. The screen stays
    /// hidden until the leaderboard is closed (mirrors snake's game over flow).
    /// Falls back to revealing the screen immediately if the leaderboard cannot
    /// run (no manager, unknown board, or no canvas).
    /// </summary>
    public static LeaderboardUI ShowForGame(GameObject screenToReveal, string gameKey, string stageId, int score)
    {
        if (LeaderboardManager.Instance == null)
        {
            if (screenToReveal != null) screenToReveal.SetActive(true);
            return null;
        }

        string boardKey = LeaderboardManager.GetBoardKey(gameKey, stageId);
        if (!LeaderboardManager.Instance.IsValidBoard(boardKey))
        {
            if (screenToReveal != null) screenToReveal.SetActive(true);
            return null;
        }

        Canvas canvas = screenToReveal != null ? screenToReveal.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            if (screenToReveal != null) screenToReveal.SetActive(true);
            return null;
        }

        LeaderboardUI ui = Show(canvas, boardKey, score);
        if (ui == null)
        {
            if (screenToReveal != null) screenToReveal.SetActive(true);
            return null;
        }

        ui.onClose = () =>
        {
            ui.onClose = null;
            if (screenToReveal != null) screenToReveal.SetActive(true);
        };
        return ui;
    }

    #endregion

    #region UI Construction

    private void BuildPanel()
    {
        RectTransform rootRT = (RectTransform)transform;
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        CanvasGroup cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
        canvasGroup = cg;

        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 1f);
        bg.raycastTarget = true;

        RectTransform card = CreateFrame(rootRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(560f, 640f));

        TextMeshProUGUI title = CreateText("Title", card,
            "LEADERBOARD", 34f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f));
        StretchTop(title.rectTransform, 0f, 48f);

        statusText = CreateText("Status", card,
            "Loading...", 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f));
        StretchTop(statusText.rectTransform, -56f, 30f);

        RectTransform listFrame = CreateFrame(card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(520f, 380f));
        listText = CreateText("List", listFrame,
            "", 20f, TextAlignmentOptions.TopLeft, Color.white);
        RectTransform listRT = listText.rectTransform;
        listRT.anchorMin = new Vector2(0.5f, 1f);
        listRT.anchorMax = new Vector2(0.5f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.anchoredPosition = new Vector2(0f, -12f);
        listRT.sizeDelta = new Vector2(490f, 356f);

        rankText = CreateText("Rank", card,
            "", 20f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f));
        StretchTop(rankText.rectTransform, -494f, 30f);

        nameInputRoot = new GameObject("NameInputRoot", typeof(RectTransform));
        nameInputRoot.transform.SetParent(card, false);
        RectTransform niRT = nameInputRoot.GetComponent<RectTransform>();
        niRT.anchorMin = new Vector2(0.5f, 0f);
        niRT.anchorMax = new Vector2(0.5f, 0f);
        niRT.pivot = new Vector2(0.5f, 0f);
        niRT.anchoredPosition = new Vector2(0f, 60f);
        niRT.sizeDelta = new Vector2(500f, 56f);
        nameInputRoot.SetActive(false);

        BuildNameInput(niRT);

        Button closeBtn = CreateButton("CloseButton", card, new Vector2(0f, 10f), new Vector2(180f, 44f), "Close");
        closeBtn.onClick.AddListener(Close);
    }

    private void BuildNameInput(RectTransform parent)
    {
        TextMeshProUGUI label = CreateText("Label", parent,
            "Your name:", 20f, TextAlignmentOptions.Left, Color.white);
        label.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        label.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        label.rectTransform.sizeDelta = new Vector2(110f, 40f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 0f);

        GameObject fieldGO = new GameObject("Field", typeof(RectTransform));
        fieldGO.transform.SetParent(parent, false);
        RectTransform fieldRT = fieldGO.GetComponent<RectTransform>();
        fieldRT.anchorMin = new Vector2(0f, 0.5f);
        fieldRT.anchorMax = new Vector2(0f, 0.5f);
        fieldRT.sizeDelta = new Vector2(200f, 40f);
        fieldRT.anchoredPosition = new Vector2(130f, 0f);

        Image fieldBg = fieldGO.AddComponent<Image>();
        fieldBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject textAreaGO = new GameObject("TextArea", typeof(RectTransform));
        textAreaGO.transform.SetParent(fieldRT, false);
        RectTransform textAreaRT = textAreaGO.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(8f, 4f);
        textAreaRT.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI text = CreateText("Text", textAreaGO.transform,
            "", 20f, TextAlignmentOptions.Left, Color.white);
        StretchFull(text.rectTransform);

        TextMeshProUGUI placeholder = CreateText("Placeholder", textAreaGO.transform,
            "Enter name...", 20f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.4f));
        StretchFull(placeholder.rectTransform);

        nameInput = fieldGO.AddComponent<TMP_InputField>();
        nameInput.textComponent = text;
        nameInput.placeholder = placeholder;
        nameInput.textViewport = textAreaRT;
        nameInput.text = "";
        nameInput.characterLimit = MaxNameLength;
        nameInput.characterValidation = TMP_InputField.CharacterValidation.Name;

        RectTransform saveRT = CreateRect("SaveButton", parent, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 0f), new Vector2(150f, 44f));
        Image saveBg = saveRT.gameObject.AddComponent<Image>();
        saveBg.color = new Color(0.85f, 0.55f, 0.1f, 1f);
        Button saveBtn = saveRT.gameObject.AddComponent<Button>();
        saveBtn.targetGraphic = saveBg;
        TextMeshProUGUI saveLabel = CreateText("Label", saveRT, "Save", 20f, TextAlignmentOptions.Center, Color.white);
        StretchFull(saveLabel.rectTransform);
        saveBtn.onClick.AddListener(SaveName);
    }

    public void SaveName()
    {
        if (submitted) return;
        if (nameInput == null) EnsureRefs();
        if (nameInput == null) return;

        string name = nameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        if (name.Length > MaxNameLength)
            name = name.Substring(0, MaxNameLength);

        submitted = true;
        nameInputRoot.SetActive(false);
        _ = SaveAndRenderAsync(name);
    }

    private async System.Threading.Tasks.Task SaveAndRenderAsync(string name)
    {
        if (LeaderboardManager.Instance != null)
            await LeaderboardManager.Instance.SubmitScoreAndWaitAsync(boardKey, name, playerScore);
        FetchAndRender();
    }

    #endregion

    #region Data

    [Header("Button Wiring")]
    [SerializeField] private Canvas canvas;

    public void OpenLeaderboard()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();
        LeaderboardUI ui = Show(canvas, boardKey, playerScore);
        if (ui != null && ui.rankText != null)
            ui.rankText.gameObject.SetActive(false);
    }

    private void FetchAndRender()
    {
        if (fetching) return;
        fetching = true;

        if (boardKey.StartsWith("minigolf_") && playerScore > 0)
        {
            SaveMiniGolfStrokesLocally();
        }

        if (!LeaderboardManager.IsOnline)
        {
            fetching = false;
            ShowOfflineLocalScores();
            return;
        }

        RefreshAsync();
    }

    private void SaveMiniGolfStrokesLocally()
    {
        string levelPart = boardKey.Substring("minigolf_".Length);
        string sceneName = "MiniGolf-" + levelPart.Substring(0, 1).ToUpper() + levelPart.Substring(1);
        string key = "MiniGolf_BestStrokes_" + sceneName;
        int best = PlayerPrefs.GetInt(key, 0);
        if (best == 0 || playerScore < best)
        {
            PlayerPrefs.SetInt(key, playerScore);
            PlayerPrefs.Save();
        }
    }

    private void ShowOfflineLocalScores()
    {
        if (statusText != null)
            statusText.text = "No internet connection";

        if (nameInputRoot != null)
            nameInputRoot.SetActive(false);

        if (rankText != null)
            rankText.gameObject.SetActive(false);

        List<LeaderboardEntry> localEntries = LoadLocalScores(boardKey);
        RenderList(localEntries);
    }

    private List<LeaderboardEntry> LoadLocalScores(string boardKey)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
        int score = 0;

        if (boardKey == "cubedash")
        {
            string cubeData = SaveSystem.Load("save");
            if (!string.IsNullOrEmpty(cubeData))
            {
                Data d = JsonUtility.FromJson<Data>(cubeData);
                if (d != null) score = Mathf.RoundToInt(d.highscore);
            }
        }
        else if (boardKey == "shooter")
        {
            ShooterSaveSystem.Initialize();
            string shooterData = ShooterSaveSystem.Load("save");
            if (!string.IsNullOrEmpty(shooterData))
            {
                ShooterSaveData sd = JsonUtility.FromJson<ShooterSaveData>(shooterData);
                if (sd != null) score = sd.highscore;
            }
        }
        else if (boardKey == "match3")
        {
            score = Match3Progress.GetSavedTotalScore();
        }
        else if (boardKey.StartsWith("snake_"))
        {
            string stageId = boardKey.Substring("snake_".Length);
            stageId = char.ToUpper(stageId[0]) + stageId.Substring(1);
            score = SnakeSaveSystem.GetHighScore(stageId);
        }
        else if (boardKey.StartsWith("minigolf_"))
        {
            string levelPart = boardKey.Substring("minigolf_".Length);
            string sceneName = "MiniGolf-" + levelPart.Substring(0, 1).ToUpper() + levelPart.Substring(1);
            score = PlayerPrefs.GetInt("MiniGolf_BestStrokes_" + sceneName, 0);
        }

        if (score > 0)
        {
            entries.Add(new LeaderboardEntry("You", score, 0));
        }

        return entries;
    }

    private void RefreshAsync()
    {
        LeaderboardManager.Instance?.FetchTop(boardKey, TopLimit, entries =>
        {
            fetching = false;
            RenderList(entries);
        }, () =>
        {
            fetching = false;
        });

        LeaderboardManager.Instance?.GetPlayerRank(boardKey, playerScore, rank =>
        {
            rankText.text = $"Your rank: #{rank}";
            if (!submitted)
                nameInputRoot.SetActive(playerScore > 0 && rank > 0 && rank <= TopLimit);
        }, () =>
        {
            rankText.text = "";
            if (!submitted)
                nameInputRoot.SetActive(false);
        });
    }

    private void RenderList(List<LeaderboardEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            listText.text = "No scores yet. Be the first!";
            return;
        }

        string output = "";
        int count = Mathf.Min(entries.Count, TopLimit);
        for (int i = 0; i < count; i++)
        {
            LeaderboardEntry e = entries[i];
            string rank = (i + 1).ToString();
            string name = e.Name;
            if (name.Length > MaxNameLength)
                name = name.Substring(0, MaxNameLength);
            string rankCell = $"<mspace=0.7em>#{rank.PadLeft(2)}</mspace>";
            string nameCell = $"<mspace=0.52em>{name.PadRight(MaxNameLength)}</mspace>";
            string scoreCell = $"<mspace=0.52em>{e.Score,8}</mspace>";
            string breakdownLine = boardKey == "match3" && !string.IsNullOrEmpty(e.Breakdown) ? $"\n<size=14><color=#B8F5FF>{e.Breakdown}</color></size>" : "";
            output += rankCell + " " + nameCell + scoreCell + breakdownLine + "\n";
        }
        listText.text = output.TrimEnd('\n');
    }

    public void Close()
    {
        System.Action callback = onClose;
        onClose = null;
        gameObject.SetActive(false);
        callback?.Invoke();
    }

    #endregion

    #region Helpers

    private void EnsureRefs()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (statusText == null) statusText = FindText("Status");
        if (listText == null) listText = FindText("List");
        if (rankText == null) rankText = FindText("Rank");
        if (nameInputRoot == null)
        {
            foreach (RectTransform child in GetComponentsInChildren<RectTransform>(true))
            {
                if (child.name == "NameInputRoot") { nameInputRoot = child.gameObject; break; }
            }
        }
        if (nameInput == null && nameInputRoot != null)
            nameInput = nameInputRoot.GetComponentInChildren<TMP_InputField>(true);
    }

    private TextMeshProUGUI FindText(string name)
    {
        foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.gameObject.name == name) return t;
        }
        return null;
    }

    /// <summary>
    /// Applies the black backdrop / white-frame styling to an existing prefab
    /// instance. The prefab was authored before the framed look, so instead of
    /// regenerating it (which would wipe manual layout), we dress it in place.
    /// </summary>
    public void ApplyPrefabStyle()
    {
        if (framed) return;
        framed = true;

        Image bg = GetComponent<Image>();
        if (bg != null && bg.sprite == null)
            bg.color = new Color(0f, 0f, 0f, 1f);

        RectTransform card = FindChildRect("Card");
        if (card != null && card.GetComponent<Image>() == null)
        {
            Image cardImg = card.gameObject.AddComponent<Image>();
            cardImg.color = Color.white;
            cardImg.raycastTarget = false;

            GameObject innerGO = new GameObject("Inner", typeof(RectTransform));
            innerGO.transform.SetParent(card, false);
            RectTransform inner = (RectTransform)innerGO.transform;
            inner.anchorMin = Vector2.zero;
            inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(4f, 4f);
            inner.offsetMax = new Vector2(-4f, -4f);
            Image innerImg = innerGO.AddComponent<Image>();
            innerImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            innerImg.raycastTarget = false;
            inner.SetAsFirstSibling();
        }

        RectTransform list = FindChildRect("List");
        if (list != null && list.parent != null && list.GetComponent<Image>() == null)
        {
            GameObject frameGO = new GameObject("ListFrame", typeof(RectTransform));
            RectTransform frame = (RectTransform)frameGO.transform;
            frame.SetParent(card, false);
            frame.anchorMin = list.anchorMin;
            frame.anchorMax = list.anchorMax;
            frame.pivot = list.pivot;
            frame.anchoredPosition = list.anchoredPosition;
            frame.sizeDelta = new Vector2(list.sizeDelta.x + 8f, list.sizeDelta.y + 8f);
            Image frameImg = frameGO.AddComponent<Image>();
            frameImg.color = Color.white;
            frameImg.raycastTarget = false;

            GameObject listBgGO = new GameObject("Inner", typeof(RectTransform));
            listBgGO.transform.SetParent(frame, false);
            RectTransform listBg = (RectTransform)listBgGO.transform;
            listBg.anchorMin = Vector2.zero;
            listBg.anchorMax = Vector2.one;
            listBg.offsetMin = new Vector2(4f, 4f);
            listBg.offsetMax = new Vector2(-4f, -4f);
            Image listBgImg = listBgGO.AddComponent<Image>();
            listBgImg.color = new Color(0.05f, 0.05f, 0.05f, 1f);
            listBgImg.raycastTarget = false;

            frame.SetSiblingIndex(list.GetSiblingIndex());
        }
    }

    private RectTransform FindChildRect(string name)
    {
        foreach (RectTransform child in GetComponentsInChildren<RectTransform>(true))
        {
            if (child.gameObject.name == name) return child;
        }
        return null;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
        return rt;
    }

    /// <summary>
    /// Builds a rect with a white border: a white backdrop plus a slightly smaller
    /// dark fill, so the framed area reads as a clean white outline around the
    /// content (used for the leaderboard card and the score list).
    /// </summary>
    private static RectTransform CreateFrame(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size)
    {
        RectTransform frame = CreateRect("Frame", parent, anchorMin, anchorMax, position, size);

        Image border = frame.gameObject.AddComponent<Image>();
        border.color = Color.white;
        border.raycastTarget = false;

        RectTransform inner = CreateRect("Inner", frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(size.x - 8f, size.y - 8f));
        Image fill = inner.gameObject.AddComponent<Image>();
        fill.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        fill.raycastTarget = false;

        return frame;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string label)
    {
        RectTransform rt = CreateRect(name, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = new Color(0.85f, 0.55f, 0.1f, 1f);
        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = bg;

        TextMeshProUGUI text = CreateText("Label", rt, label, 20f, TextAlignmentOptions.Center, Color.white);
        StretchFull(text.rectTransform);
        return button;
    }

    private static void StretchTop(RectTransform rt, float topOffset, float height)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, topOffset);
        rt.sizeDelta = new Vector2(560f, height);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    #endregion
}
