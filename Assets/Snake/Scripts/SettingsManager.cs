using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private Resolution[] availableResolutions;
    private const string ScreenModeKey = "SnakeScreenMode";
    private const string ResolutionWidthKey = "SnakeResolutionWidth";
    private const string ResolutionHeightKey = "SnakeResolutionHeight";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        PopulateResolutionDropdown();

        int savedScreenMode = PlayerPrefs.GetInt(ScreenModeKey, 0);
        int savedResWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedResHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);

        if (bgmSlider != null)
            bgmSlider.value = PlayerPrefs.GetFloat("SnakeBGMVolume", 0.5f);
        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat("SnakeSFXVolume", 0.5f);

        if (screenModeDropdown != null)
            screenModeDropdown.value = savedScreenMode;
        ApplyScreenMode(savedScreenMode);

        int resIndex = GetResolutionIndex(savedResWidth, savedResHeight);
        if (resolutionDropdown != null)
            resolutionDropdown.value = resIndex >= 0 ? resIndex : 0;
        ApplyResolution(resolutionDropdown != null ? resolutionDropdown.value : 0);

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.AddListener(ApplyScreenMode);
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(ApplyResolution);
        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void PopulateResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        float targetAspect = 16f / 9f;

        availableResolutions = Screen.resolutions
            .GroupBy(r => new { r.width, r.height })
            .Select(g => g.First())
            .Where(r => Mathf.Abs((float)r.width / r.height - targetAspect) < 0.02f)
            .OrderBy(r => r.width)
            .ThenBy(r => r.height)
            .ToArray();

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(
            availableResolutions
                .Select(r => $"{r.width} x {r.height}")
                .ToList()
        );
    }

    private int GetResolutionIndex(int width, int height)
    {
        if (availableResolutions == null) return -1;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            if (availableResolutions[i].width == width && availableResolutions[i].height == height)
                return i;
        }
        return -1;
    }

    public void ApplyScreenMode(int index)
    {
        FullScreenMode mode = index switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.FullScreenWindow,
        };
        Screen.fullScreenMode = mode;
        if (resolutionDropdown != null)
            ApplyResolution(resolutionDropdown.value);
        PlayerPrefs.SetInt(ScreenModeKey, index);
        PlayerPrefs.Save();
    }

    public void ApplyResolution(int index)
    {
        if (availableResolutions == null || index < 0 || index >= availableResolutions.Length) return;
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ResolutionWidthKey, res.width);
        PlayerPrefs.SetInt(ResolutionHeightKey, res.height);
        PlayerPrefs.Save();
    }

    private void SetBGMVolume(float value)
    {
        if (SnakeAudioManager.Instance != null)
            SnakeAudioManager.Instance.BGMVolume = value;
    }

    private void SetSFXVolume(float value)
    {
        if (SnakeAudioManager.Instance != null)
            SnakeAudioManager.Instance.SFXVolume = value;
    }

    public void BackButton()
    {
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        PauseManager.Instance?.HideSettings();
    }
}
