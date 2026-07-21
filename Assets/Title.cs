using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScript : MonoBehaviour
{
    [Header("Target Scene Names")]
    [SerializeField] private string cubeDashSceneName = "CubeDash";
    [SerializeField] private string miniGolfSceneName = "MainMenu";
    [SerializeField] private string topDownShooterSceneName = "Menu";
    [SerializeField] private string snakeSceneName = "SnakeMenu";
    [SerializeField] private float sceneLoadDelay = 0.08f;

    private TextMeshProUGUI totalStarsText;
    private Image totalStarsImage;
    private Sprite starSprite;

    private void Start()
    {
        EnsureTitleButtonFeedbackComponents();
        CreateStarsDisplay();
        UpdateStarsDisplay();
        TitleAudioManager.Instance?.PlayBgm();
    }

    private void EnsureTitleButtonFeedbackComponents()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            TitleButtonFeedback feedback = button.GetComponent<TitleButtonFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<TitleButtonFeedback>();
            }
        }
    }

    private void CreateStarsDisplay()
    {
        if (totalStarsText != null)
        {
            return;
        }
        GameObject canvasObject = new GameObject("TotalStarsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(null, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        // Container with horizontal layout to show number then image
        GameObject container = new GameObject("TotalStarsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        container.transform.SetParent(canvasObject.transform, false);

        HorizontalLayoutGroup h = container.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleLeft;
        h.spacing = 2f;
        h.padding = new RectOffset(2, 2, 2, 2);
        // Prevent the layout from stretching children so the star keeps its aspect
        h.childControlWidth = false;
        h.childControlHeight = false;
        h.childForceExpandWidth = false;
        h.childForceExpandHeight = false;

        // Number text
        GameObject textObject = new GameObject("TotalStarsText", typeof(RectTransform));
        textObject.transform.SetParent(container.transform, false);
        totalStarsText = textObject.AddComponent<TextMeshProUGUI>();
        totalStarsText.text = "0";
        totalStarsText.fontSize = 32;
        totalStarsText.color = Color.white;
        totalStarsText.alignment = TextAlignmentOptions.MidlineLeft;
        totalStarsText.enableWordWrapping = false;

        // Allow the text to auto-size up to the image height so it visually matches the star
        totalStarsText.enableAutoSizing = true;
        totalStarsText.fontSizeMin = 10;
        totalStarsText.fontSizeMax = 72;


        // Star image (may be empty if sprite not found)
        GameObject imageObject = new GameObject("TotalStarsImage", typeof(RectTransform));
        imageObject.transform.SetParent(container.transform, false);
        totalStarsImage = imageObject.AddComponent<Image>();
        totalStarsImage.preserveAspect = true;

        // Give the image a preferred fixed size so it doesn't stretch in the layout
        UnityEngine.UI.LayoutElement imgLayout = imageObject.AddComponent<UnityEngine.UI.LayoutElement>();
        imgLayout.preferredWidth = 48f;
        imgLayout.preferredHeight = 48f;

        // Give the text a layout element so it shares the same height as the star image
        UnityEngine.UI.LayoutElement textLayout = textObject.AddComponent<UnityEngine.UI.LayoutElement>();
        textLayout.preferredHeight = imgLayout.preferredHeight;
        textLayout.minWidth = 0f;

        // Make the text size itself to its content so it grows when digits increase
        var fitter = textObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Try to load a sprite from Resources/star (user should place the provided image at Assets/Resources/star.png)
        starSprite = Resources.Load<Sprite>("star");
        if (starSprite != null)
        {
            totalStarsImage.sprite = starSprite;
        }
        else
        {
            // hide image if sprite not available; fallback to unicode star in the text
            totalStarsImage.enabled = false;
        }

        RectTransform rectTransform = container.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(20f, -20f);
        rectTransform.sizeDelta = new Vector2(400f, 60f);
    }

    private void UpdateStarsDisplay()
    {
        if (totalStarsText == null)
        {
            CreateStarsDisplay();
        }

        int total = MiniGolfTotalStarsManager.GetTotalStars();
        if (starSprite != null)
        {
            // show number in text and enable image
            totalStarsText.text = total.ToString();
            if (totalStarsImage != null)
            {
                totalStarsImage.enabled = true;
                totalStarsImage.sprite = starSprite;
            }
        }
        else
        {
            // fallback: text with unicode star
            totalStarsText.text = total + " ★";
            if (totalStarsImage != null)
            {
                totalStarsImage.enabled = false;
            }
        }
    }

    public void PlayCubeDash()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        StartCoroutine(LoadSceneAfterDelay(cubeDashSceneName));
    }

    public void PlayMiniGolf()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        StartCoroutine(LoadSceneAfterDelay(miniGolfSceneName));
    }

    public void PlayTopDownShooter()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        StartCoroutine(LoadSceneAfterDelay(topDownShooterSceneName));
    }

    public void PlaySnakeGame()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        StartCoroutine(LoadSceneAfterDelay(snakeSceneName));
    }

    public void LoadSceneByName(string sceneName)
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        StartCoroutine(LoadSceneAfterDelay(sceneName));
    }

    public void BackToTitleScreen()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        Time.timeScale = 1f;
        StartCoroutine(LoadSceneAfterDelay("TitleScreen"));
    }

    public void ExitGame()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();

        StartCoroutine(ExitAfterDelay());
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        yield return new WaitForSecondsRealtime(sceneLoadDelay);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator ExitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(sceneLoadDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
 #endif
    }
}
