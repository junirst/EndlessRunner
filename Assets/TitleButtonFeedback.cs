using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TitleButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.96f, 0.93f, 0.82f, 1f);
    [SerializeField] private Color hoverColor = new Color(1f, 0.84f, 0.36f, 1f);
    [SerializeField] private Color pressedColor = new Color(1f, 0.67f, 0.24f, 1f);

    [Header("Motion")]
    [SerializeField] private Vector3 hoverScale = new Vector3(1.08f, 1.08f, 1f);
    [SerializeField] private Vector3 pressedScale = new Vector3(0.94f, 0.94f, 1f);
    [SerializeField] private float transitionDuration = 0.10f;

    private Graphic backgroundGraphic;
    private TMP_Text labelText;
    private Vector3 defaultScale;
    private bool isPointerOver;
    private bool isPointerDown;
    private Coroutine transitionRoutine;

    public void Configure(Color normal, Color hover, Color pressed, Vector3 hoverScaleValue, Vector3 pressedScaleValue, float duration)
    {
        normalColor = normal;
        hoverColor = hover;
        pressedColor = pressed;
        hoverScale = hoverScaleValue;
        pressedScale = pressedScaleValue;
        transitionDuration = duration;

        ApplyStateInstantly(GetCurrentTargetColor(), GetCurrentTargetScale());
    }

    private void Awake()
    {
        backgroundGraphic = GetComponent<Graphic>();
        labelText = GetComponentInChildren<TMP_Text>(true);
        defaultScale = transform.localScale;
    }

    private void OnEnable()
    {
        isPointerOver = false;
        isPointerDown = false;
        ApplyStateInstantly(normalColor, defaultScale);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;

        TitleAudioManager.Instance?.PlayButtonHoverSfx();

        if (!isPointerDown)
        {
            AnimateTo(hoverColor, hoverScale);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;

        if (!isPointerDown)
        {
            AnimateTo(normalColor, defaultScale);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        AnimateTo(pressedColor, pressedScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;

        if (isPointerOver)
        {
            AnimateTo(hoverColor, hoverScale);
        }
        else
        {
            AnimateTo(normalColor, defaultScale);
        }
    }

    private void AnimateTo(Color color, Vector3 targetScale)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(AnimateRoutine(color, targetScale));
    }

    private IEnumerator AnimateRoutine(Color targetColor, Vector3 targetScale)
    {
        Color startColor = GetCurrentTargetColor();
        Vector3 startScale = transform.localScale;

        if (transitionDuration <= 0f)
        {
            ApplyStateInstantly(targetColor, targetScale);
            transitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float easedT = t * t * (3f - 2f * t);

            ApplyStateInstantly(Color.Lerp(startColor, targetColor, easedT), Vector3.Lerp(startScale, targetScale, easedT));
            yield return null;
        }

        ApplyStateInstantly(targetColor, targetScale);
        transitionRoutine = null;
    }

    private void ApplyStateInstantly(Color color, Vector3 targetScale)
    {
        if (backgroundGraphic != null)
        {
            backgroundGraphic.color = color;
        }

        if (labelText != null)
        {
            labelText.color = Color.Lerp(Color.white, color, 0.12f);
        }

        transform.localScale = targetScale;
    }

    private Color GetCurrentTargetColor()
    {
        if (isPointerDown)
        {
            return pressedColor;
        }

        if (isPointerOver)
        {
            return hoverColor;
        }

        return normalColor;
    }

    private Vector3 GetCurrentTargetScale()
    {
        if (isPointerDown)
        {
            return pressedScale;
        }

        if (isPointerOver)
        {
            return hoverScale;
        }

        return defaultScale;
    }
}