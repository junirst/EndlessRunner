using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class StageCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private Image stageImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Button button;
    [SerializeField] private Color stageColor = Color.white;
    [SerializeField] private Color frameColor = Color.white;
    [SerializeField] private string stageName;
    [SerializeField] private string sceneName;

    private int highScore;
    private string hoverText = "";
    private Coroutine blinkRoutine;

    private static readonly float BlinkInterval = 0.5f;
    private static readonly float FastBlinkInterval = 0.1f;

    public string SceneName => sceneName;
    public Button Button => button;

    public void Configure(TextMeshProUGUI hsText, Image img, Image frame, Button btn)
    {
        highScoreText = hsText;
        stageImage = img;
        frameImage = frame;
        button = btn;
    }

    public void Setup(string stageId, string scene, int score)
    {
        stageName = stageId;
        sceneName = scene;
        highScore = score;
        ApplyColors();

        if (highScoreText != null)
        {
            hoverText = score > 0 ? $"Your best   {score}" : "0 - Be the first";
            highScoreText.gameObject.SetActive(false);
        }

        gameObject.name = $"{stageName}_Card";
    }

    public void ApplyColors()
    {
        if (stageImage != null)
            stageImage.color = stageColor;
        if (frameImage != null)
        {
            frameImage.color = frameColor;
            frameImage.gameObject.SetActive(true);
        }
    }

    public void UpdateHighScore(int score)
    {
        highScore = score;
        if (highScoreText != null)
        {
            hoverText = score > 0 ? $"Your best   {score}" : "0 - Be the first";
        }
    }

    public void SetLeaderboardTop(string name, int score)
    {
        hoverText = $"{name}   {score}";
    }

    private void OnEnable()
    {
        if (highScoreText != null)
            highScoreText.gameObject.SetActive(false);
        StopBlinking();
        ResetAlpha();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            return;
        HideHighlight();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowHighlight();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideHighlight();
    }

    private void ShowHighlight()
    {
        if (highScoreText != null)
        {
            highScoreText.text = hoverText;
            highScoreText.gameObject.SetActive(true);
        }
        StartBlinking(BlinkInterval);
    }

    private void HideHighlight()
    {
        if (highScoreText != null)
            highScoreText.gameObject.SetActive(false);
        StopBlinking();
        ResetAlpha();
    }

    private void ResetAlpha()
    {
        if (stageImage != null)
        {
            Color c = stageImage.color;
            c.a = 1f;
            stageImage.color = c;
        }
        if (frameImage != null)
        {
            Color c = frameImage.color;
            c.a = 1f;
            frameImage.color = c;
        }
    }

    public void StartBlinking(float interval)
    {
        StopBlinking();
        blinkRoutine = StartCoroutine(BlinkRoutine(interval));
    }

    private void StopBlinking()
    {
        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }
    }

    private IEnumerator BlinkRoutine(float interval)
    {
        while (true)
        {
            if (frameImage != null)
            {
                Color c = frameImage.color;
                c.a = 0.2f;
                frameImage.color = c;
            }
            yield return new WaitForSeconds(interval);
            if (frameImage != null)
            {
                Color c = frameImage.color;
                c.a = 1f;
                frameImage.color = c;
            }
            yield return new WaitForSeconds(interval);
        }
    }

    public void PlayConfirmBlink(System.Action onComplete)
    {
        StopBlinking();
        StartCoroutine(FastBlinkThenComplete(onComplete));
    }

    private IEnumerator FastBlinkThenComplete(System.Action onComplete)
    {
        float elapsed = 0f;
        float duration = 1f;
        bool visible = true;

        while (elapsed < duration)
        {
            if (frameImage != null)
            {
                Color c = frameImage.color;
                c.a = visible ? 1f : 0.1f;
                frameImage.color = c;
            }
            visible = !visible;
            elapsed += FastBlinkInterval;
            yield return new WaitForSeconds(FastBlinkInterval);
        }

        ResetAlpha();
        onComplete?.Invoke();
    }
}
