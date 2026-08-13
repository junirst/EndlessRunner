using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameOverMenu : MonoBehaviour
{
    [SerializeField] private string arrowChildName = "ArrowIndicator";
    [SerializeField] private Vector2[] buttonArrowPositions;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private int blinkCount = 6;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private float introDuration = 1f;
    [SerializeField] private Color frameColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private float framePadding = 8f;
    [SerializeField] private float frameThickness = 3f;
    [SerializeField] private float frameBlinkInterval = 0.5f;
    [SerializeField, Range(0f, 1f)] private float frameIdleAlpha = 0.88f;

    private Button[] buttons;
    private RectTransform arrowRect;
    private Image[][] buttonFrames;
    private Coroutine frameBlinkRoutine;
    private int selectedIndex = 0;
    private bool canInteract = true;
    private bool isBlinking;

    private void Awake()
    {
        buttons = GetComponentsInChildren<Button>(true);
        arrowRect = FindChildRecursive(transform, arrowChildName)?.GetComponent<RectTransform>();
        BuildButtonFrames();
    }

    private void BuildButtonFrames()
    {
        buttonFrames = new Image[buttons.Length][];
        for (int i = 0; i < buttons.Length; i++)
        {
            RectTransform btnRT = (RectTransform)buttons[i].transform;
            GameObject frameGO = new GameObject("Frame", typeof(RectTransform));
            frameGO.transform.SetParent(btnRT.parent, false);
            RectTransform frameRT = (RectTransform)frameGO.transform;
            frameRT.anchorMin = btnRT.anchorMin;
            frameRT.anchorMax = btnRT.anchorMax;
            frameRT.pivot = btnRT.pivot;
            frameRT.anchoredPosition = btnRT.anchoredPosition;
            frameRT.sizeDelta = btnRT.sizeDelta + new Vector2(framePadding * 2f, framePadding * 2f);
            frameGO.transform.SetSiblingIndex(btnRT.GetSiblingIndex());

            buttonFrames[i] = new Image[4]
            {
                CreateOutlineBar("Top", frameRT, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, frameThickness)),
                CreateOutlineBar("Bottom", frameRT, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, frameThickness)),
                CreateOutlineBar("Left", frameRT, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(frameThickness, 0f)),
                CreateOutlineBar("Right", frameRT, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(frameThickness, 0f)),
            };
        }
    }

    private Image CreateOutlineBar(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)bar.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = sizeDelta;
        Image img = bar.GetComponent<Image>();
        img.color = frameColor;
        img.color = new Color(img.color.r, img.color.g, img.color.b, frameIdleAlpha);
        img.raycastTarget = false;
        return img;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private void Start()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            EventTrigger trigger = buttons[i].GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = buttons[i].gameObject.AddComponent<EventTrigger>();

            int capturedIndex = i;
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entry.callback.AddListener((data) => OnButtonHover(capturedIndex));
            trigger.triggers.Add(entry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) => OnButtonExit(capturedIndex));
            trigger.triggers.Add(exitEntry);
        }
    }

    private void OnEnable()
    {
        selectedIndex = 0;
        canInteract = false;

        for (int i = 0; i < buttons.Length; i++)
        {
            int capturedIndex = i;
            buttons[i].onClick.AddListener(() =>
            {
                if (gameObject.activeInHierarchy)
                    StartCoroutine(BlinkAndExecute(capturedIndex));
            });
        }

        StartCoroutine(PlayIntro());
    }

    private void OnDisable()
    {
        foreach (var btn in buttons)
            btn.onClick.RemoveAllListeners();
        HideHighlight();
    }

    private IEnumerator SelectFirstButtonDelayed()
    {
        yield return null;
        if (EventSystem.current != null && buttons.Length > 0)
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    /// <summary>
    /// Plays the game over reveal: the title fades from black to white over
    /// <see cref="introDuration"/> while everything else stays hidden. The
    /// player can skip by clicking or pressing any key.
    /// </summary>
    private IEnumerator PlayIntro()
    {
        if (titleText == null)
            titleText = FindTitleText();

        if (titleText == null || introDuration <= 0f)
        {
            RevealContent(true);
            yield break;
        }

        List<GameObject> hidden = HideSiblings(titleText.transform);

        Color from = Color.black;
        Color darkGray = new Color(0.25f, 0.25f, 0.25f, 1f);
        Color gray = new Color(0.5f, 0.5f, 0.5f, 1f);
        titleText.color = from;

        float elapsed = 0f;
        while (elapsed < introDuration)
        {
            if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                break;

            float t = Mathf.Clamp01(elapsed / introDuration);
            titleText.color = FadeColor(t, from, darkGray, gray, Color.white);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        titleText.color = Color.white;
        RevealContent(false, hidden);

        yield return null;
        canInteract = true;
        UpdateArrowPosition();
        StartCoroutine(SelectFirstButtonDelayed());
    }

    private TextMeshProUGUI FindTitleText()
    {
        foreach (TextMeshProUGUI t in GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (t.text == "GAME over") return t;
        }
        return null;
    }

    private List<GameObject> HideSiblings(Transform target)
    {
        List<GameObject> hidden = new List<GameObject>();
        if (target.parent == null) return hidden;

        foreach (Transform child in target.parent)
        {
            if (child.gameObject == target.gameObject) continue;
            if (child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(false);
                hidden.Add(child.gameObject);
            }
        }
        return hidden;
    }

    private void RevealContent(bool instant, List<GameObject> hidden = null)
    {
        if (hidden == null)
        {
            hidden = new List<GameObject>();
            if (titleText != null && titleText.transform.parent != null)
            {
                foreach (Transform child in titleText.transform.parent)
                {
                    if (child.gameObject != titleText.gameObject)
                        hidden.Add(child.gameObject);
                }
            }
        }

        foreach (GameObject go in hidden)
        {
            if (go != null) go.SetActive(true);
        }

        if (instant)
        {
            canInteract = true;
            UpdateArrowPosition();
            StartCoroutine(SelectFirstButtonDelayed());
        }
    }

    private static Color FadeColor(float t, Color black, Color darkGray, Color gray, Color white)
    {
        if (t < 0.33f)
            return Color.Lerp(black, darkGray, t / 0.33f);
        if (t < 0.66f)
            return Color.Lerp(darkGray, gray, (t - 0.33f) / 0.33f);
        return Color.Lerp(gray, white, (t - 0.66f) / 0.34f);
    }

    private void Update()
    {
        if (!canInteract || buttons.Length == 0) return;

        float v = Input.GetAxisRaw("Vertical");
        if (v < -0.5f && selectedIndex < buttons.Length - 1)
        {
            selectedIndex++;
            UpdateArrowPosition();
            ShowHighlight(selectedIndex);
            PlayNavSfx();
        }
        else if (v > 0.5f && selectedIndex > 0)
        {
            selectedIndex--;
            UpdateArrowPosition();
            ShowHighlight(selectedIndex);
            PlayNavSfx();
        }

        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartCoroutine(BlinkAndExecute(selectedIndex));
        }
    }

    public void OnButtonHover(int index)
    {
        if (!canInteract) return;
        if (selectedIndex == index) return;

        AudioManager.Instance?.PlayButtonHoverSfx();
        selectedIndex = index;
        UpdateArrowPosition();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        ShowHighlight(index);
    }

    public void OnButtonExit(int index)
    {
        if (!canInteract) return;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == buttons[index].gameObject)
            return;
        if (selectedIndex == index) return;
        HideHighlight();
    }

    private void ShowHighlight(int index)
    {
        if (buttonFrames == null || index < 0 || index >= buttonFrames.Length) return;
        StopFrameBlink();
        foreach (Image[] bars in buttonFrames)
        {
            if (bars == null) continue;
            SetBarsAlpha(bars, frameIdleAlpha);
        }
        if (buttonFrames[index] != null)
            frameBlinkRoutine = StartCoroutine(BlinkFrame(buttonFrames[index]));
    }

    private void HideHighlight()
    {
        StopFrameBlink();
        if (buttonFrames == null) return;
        foreach (Image[] bars in buttonFrames)
        {
            if (bars == null) continue;
            SetBarsAlpha(bars, frameIdleAlpha);
        }
    }

    private void StopFrameBlink()
    {
        if (frameBlinkRoutine != null)
        {
            StopCoroutine(frameBlinkRoutine);
            frameBlinkRoutine = null;
        }
    }

    private IEnumerator BlinkFrame(Image[] bars)
    {
        while (true)
        {
            SetBarsAlpha(bars, 0.2f);
            yield return new WaitForSecondsRealtime(frameBlinkInterval);

            SetBarsAlpha(bars, 1f);
            yield return new WaitForSecondsRealtime(frameBlinkInterval);
        }
    }

    private static void SetBarsAlpha(Image[] bars, float alpha)
    {
        foreach (Image bar in bars)
        {
            if (bar == null) continue;
            Color c = bar.color;
            c.a = alpha;
            bar.color = c;
        }
    }

    private void UpdateArrowPosition()
    {
        if (arrowRect == null || buttons.Length == 0 || selectedIndex >= buttons.Length) return;
        if (buttonArrowPositions == null || selectedIndex >= buttonArrowPositions.Length) return;

        arrowRect.SetParent(buttons[selectedIndex].transform, false);
        arrowRect.anchoredPosition = buttonArrowPositions[selectedIndex];
    }

    private IEnumerator BlinkAndExecute(int index)
    {
        if (isBlinking) yield break;
        isBlinking = true;
        canInteract = false;

        Button btn = buttons[index];
        TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
        Color originalColor = btnText.color;

        for (int i = 0; i < blinkCount; i++)
        {
            btnText.color = (i % 2 == 0)
                ? new Color(originalColor.r, originalColor.g, originalColor.b, 0f)
                : originalColor;
            yield return new WaitForSecondsRealtime(blinkInterval);
        }

        btnText.color = originalColor;
        canInteract = true;
        isBlinking = false;

        if (gameObject.activeInHierarchy)
            btn.onClick.Invoke();
    }

    private void PlayNavSfx()
    {
        if (SnakeAudioManager.Instance != null)
            SnakeAudioManager.Instance.PlayButtonClickSfx();
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClickSfx();
        else if (MiniGolfAudioManager.Instance != null)
            MiniGolfAudioManager.Instance.PlayButtonClickSfx();
        else if (ShooterAudioManager.Instance != null)
            ShooterAudioManager.Instance.PlayButtonClickSfx();
    }
}
