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
    [SerializeField] private string snakeSceneName = "SnakeGame";

    private TextMeshProUGUI totalStarsText;

    private void Start()
    {
        CreateStarsDisplay();
        UpdateStarsDisplay();
        TitleAudioManager.Instance?.PlayBgm();
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

        GameObject textObject = new GameObject("TotalStarsText", typeof(RectTransform));
        textObject.transform.SetParent(canvasObject.transform, false);

        totalStarsText = textObject.AddComponent<TextMeshProUGUI>();
        totalStarsText.text = "Total stars: 0";
        totalStarsText.fontSize = 32;
        totalStarsText.color = Color.white;
        totalStarsText.alignment = TextAlignmentOptions.TopLeft;
        totalStarsText.enableWordWrapping = false;

        RectTransform rectTransform = totalStarsText.GetComponent<RectTransform>();
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

        totalStarsText.text = "Total stars: " + MiniGolfTotalStarsManager.GetTotalStars();
    }

    public void PlayCubeDash()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(cubeDashSceneName);
    }

    public void PlayMiniGolf()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(miniGolfSceneName);
    }

    public void PlayTopDownShooter()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(topDownShooterSceneName);
    }

        public void PlaySnakeGame()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(snakeSceneName);
    }
    public void LoadSceneByName(string sceneName)
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        TitleAudioManager.Instance?.PlayButtonClickSfx();
        TitleAudioManager.Instance?.StopBgm();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
