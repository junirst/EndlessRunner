using System.Collections;
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

    private Button[] buttons;
    private RectTransform arrowRect;
    private int selectedIndex = 0;
    private bool canInteract = true;

    private void Awake()
    {
        buttons = GetComponentsInChildren<Button>(true);
        arrowRect = FindChildRecursive(transform, arrowChildName)?.GetComponent<RectTransform>();
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
        }
    }

    private void OnEnable()
    {
        selectedIndex = 0;
        canInteract = true;

        for (int i = 0; i < buttons.Length; i++)
        {
            int capturedIndex = i;
            buttons[i].onClick.AddListener(() => StartCoroutine(BlinkAndExecute(capturedIndex)));
        }

        UpdateArrowPosition();
        StartCoroutine(SelectFirstButtonDelayed());
    }

    private void OnDisable()
    {
        foreach (var btn in buttons)
            btn.onClick.RemoveAllListeners();
    }

    private IEnumerator SelectFirstButtonDelayed()
    {
        yield return null;
        if (EventSystem.current != null && buttons.Length > 0)
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    private void Update()
    {
        if (!canInteract || buttons.Length == 0) return;

        float v = Input.GetAxisRaw("Vertical");
        if (v < -0.5f && selectedIndex < buttons.Length - 1)
        {
            selectedIndex++;
            UpdateArrowPosition();
            PlayNavSfx();
        }
        else if (v > 0.5f && selectedIndex > 0)
        {
            selectedIndex--;
            UpdateArrowPosition();
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
        selectedIndex = index;
        UpdateArrowPosition();
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
