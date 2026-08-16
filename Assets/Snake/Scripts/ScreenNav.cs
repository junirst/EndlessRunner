using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScreenNav : MonoBehaviour
{
    [SerializeField] private string arrowChildName = "ArrowIndicator";
    [SerializeField] private Vector2[] buttonArrowPositions;
    [SerializeField] private bool enableKeyboardNav = true;

    private Button[] buttons;
    private RectTransform arrowRect;
    private int selectedIndex;
    private bool canInteract = true;

    private void Awake()
    {
        buttons = GetComponentsInChildren<Button>(true);
        if (!string.IsNullOrEmpty(arrowChildName))
        {
            var found = FindChildRecursive(transform, arrowChildName);
            if (found != null)
                arrowRect = found.GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        if (buttons.Length == 0) return;

        selectedIndex = 0;
        canInteract = true;
        EventSystem.current?.SetSelectedGameObject(buttons[0].gameObject);
        UpdateArrowPosition();
    }

    private void Update()
    {
        if (!enableKeyboardNav || !canInteract || buttons.Length == 0) return;

        float v = Input.GetAxisRaw("Vertical");
        if (v < -0.5f && selectedIndex < buttons.Length - 1)
        {
            selectedIndex++;
            PlayNavSfx();
            UpdateArrowPosition();
        }
        else if (v > 0.5f && selectedIndex > 0)
        {
            selectedIndex--;
            PlayNavSfx();
            UpdateArrowPosition();
        }

        if (Input.GetButtonDown("Submit") || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            buttons[selectedIndex].onClick.Invoke();
        }
    }

    private void UpdateArrowPosition()
    {
        if (arrowRect == null || selectedIndex >= buttons.Length) return;
        if (buttonArrowPositions != null && selectedIndex < buttonArrowPositions.Length)
        {
            arrowRect.SetParent(buttons[selectedIndex].transform, false);
            arrowRect.anchoredPosition = buttonArrowPositions[selectedIndex];
        }
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
