using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    #region Fields

    private const int TopLimit = 10;

    [SerializeField] private string boardKey;
    [SerializeField] private int playerScore;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI listText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private GameObject nameInputRoot;
    [SerializeField] private TMP_InputField nameInput;

    private bool fetching;

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
    public static LeaderboardUI Show(Canvas canvas, string boardKey, int playerScore)
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

        if (ui.canvasGroup == null || ui.statusText == null)
            ui.EnsureRefs();

        ui.gameObject.SetActive(true);
        ui.canvasGroup.alpha = 1f;
        ui.canvasGroup.blocksRaycasts = true;
        ui.canvasGroup.interactable = true;
        ui.boardKey = boardKey;
        ui.playerScore = playerScore;
        ui.fetching = false;
        ui.FetchAndRender();
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
        bg.color = new Color(0f, 0f, 0f, 0.9f);
        bg.raycastTarget = true;

        RectTransform card = CreateRect("Card", rootRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 20f), new Vector2(560f, 640f));

        TextMeshProUGUI title = CreateText("Title", card,
            "LEADERBOARD", 34f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f));
        StretchTop(title.rectTransform, 0f, 48f);

        statusText = CreateText("Status", card,
            "Loading...", 20f, TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.9f));
        StretchTop(statusText.rectTransform, -56f, 30f);

        listText = CreateText("List", card,
            "", 20f, TextAlignmentOptions.TopLeft, Color.white);
        RectTransform listRT = listText.rectTransform;
        listRT.anchorMin = new Vector2(0f, 1f);
        listRT.anchorMax = new Vector2(1f, 1f);
        listRT.pivot = new Vector2(0.5f, 1f);
        listRT.anchoredPosition = new Vector2(0f, -96f);
        listRT.sizeDelta = new Vector2(520f, 380f);

        rankText = CreateText("Rank", card,
            "", 20f, TextAlignmentOptions.Center, new Color(1f, 0.84f, 0.2f));
        StretchTop(rankText.rectTransform, -470f, 30f);

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
        nameInput.text = LeaderboardManager.Instance?.PlayerName ?? "";
        nameInput.characterLimit = 16;
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

    private void SaveName()
    {
        string name = nameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        LeaderboardManager.Instance.PlayerName = name;
        nameInputRoot.SetActive(false);
        FetchAndRender();
    }

    #endregion

    #region Data

    private void FetchAndRender()
    {
        if (fetching) return;
        fetching = true;
        statusText.text = "Loading...";

        bool hasName = LeaderboardManager.Instance != null && LeaderboardManager.Instance.HasPlayerName;

        _ = RefreshAsync(hasName);
    }

    private async System.Threading.Tasks.Task RefreshAsync(bool hasName)
    {
        // Make sure the current score is on the server before reading it back,
        // otherwise the list/rank can show the previous best.
        if (LeaderboardManager.Instance != null)
            await LeaderboardManager.Instance.SubmitAndWaitAsync(boardKey, playerScore);

        LeaderboardManager.Instance?.FetchTop(boardKey, TopLimit, entries =>
        {
            fetching = false;
            RenderList(entries);
            if (!hasName)
            {
                nameInputRoot.SetActive(true);
                statusText.text = "Enter your name to join the ranking!";
            }
        }, () =>
        {
            fetching = false;
            statusText.text = "Offline - leaderboard unavailable.";
        });

        LeaderboardManager.Instance?.GetPlayerRank(boardKey, playerScore, rank =>
        {
            rankText.text = $"Your rank: #{rank}";
        }, () =>
        {
            rankText.text = "";
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
        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry e = entries[i];
            string rank = (i + 1).ToString();
            output += $"#{rank.PadLeft(2)}  {e.Name.PadRight(16)}  {e.Score}\n";
        }
        listText.text = output.TrimEnd('\n');
        statusText.text = $"Top {entries.Count} of {boardKey}";
    }

    public void Close()
    {
        gameObject.SetActive(false);
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
