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
    private const string BGMVolumeKey = "SnakeBGMVolume";
    private const string SFXVolumeKey = "SnakeSFXVolume";

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
        LoadFromPlayerPrefs();

        if (bgmSlider != null)
            bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void LoadFromPlayerPrefs()
    {
        int savedScreenMode = PlayerPrefs.GetInt(ScreenModeKey, 0);
        int savedResWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedResHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        int resIndex = GetResolutionIndex(savedResWidth, savedResHeight);

        if (bgmSlider != null)
            bgmSlider.value = PlayerPrefs.GetFloat(BGMVolumeKey, 0.5f);
        if (sfxSlider != null)
            sfxSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 0.5f);

        if (screenModeDropdown != null)
            screenModeDropdown.SetValueWithoutNotify(savedScreenMode);
        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(resIndex >= 0 ? resIndex : 0);

        ApplyScreenMode(savedScreenMode);
        if (resIndex >= 0)
            ApplyResolution(resIndex);
    }

    public void SaveToPlayerPrefs()
    {
        if (screenModeDropdown != null)
            PlayerPrefs.SetInt(ScreenModeKey, screenModeDropdown.value);
        if (resolutionDropdown != null && availableResolutions != null)
        {
            Resolution res = availableResolutions[resolutionDropdown.value];
            PlayerPrefs.SetInt(ResolutionWidthKey, res.width);
            PlayerPrefs.SetInt(ResolutionHeightKey, res.height);
        }
        if (bgmSlider != null)
            PlayerPrefs.SetFloat(BGMVolumeKey, bgmSlider.value);
        if (sfxSlider != null)
            PlayerPrefs.SetFloat(SFXVolumeKey, sfxSlider.value);
        PlayerPrefs.Save();
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
            0 => FullScreenMode.FullScreenWindow,
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.FullScreenWindow,
        };

        if (resolutionDropdown != null && availableResolutions != null &&
            resolutionDropdown.value >= 0 && resolutionDropdown.value < availableResolutions.Length)
        {
            Resolution res = availableResolutions[resolutionDropdown.value];
            Screen.SetResolution(res.width, res.height, mode);
        }
        else
        {
            Screen.SetResolution(Screen.width, Screen.height, mode);
        }
    }

    public void ApplyResolution(int index)
    {
        if (availableResolutions == null || index < 0 || index >= availableResolutions.Length) return;
        Resolution res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
    }

    private void SetBGMVolume(float value)
    {
        if (SnakeAudioManager.Instance != null)
            SnakeAudioManager.Instance.SetBGMVolume(value);
    }

    private void SetSFXVolume(float value)
    {
        if (SnakeAudioManager.Instance != null)
            SnakeAudioManager.Instance.SetSFXVolume(value);
    }

    public void RevertToPlayerPrefs()
    {
        int savedScreenMode = PlayerPrefs.GetInt(ScreenModeKey, 0);
        int savedResWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
        int savedResHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
        int resIndex = GetResolutionIndex(savedResWidth, savedResHeight);

        if (screenModeDropdown != null)
            screenModeDropdown.SetValueWithoutNotify(savedScreenMode);
        if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(resIndex >= 0 ? resIndex : 0);

        if (SnakeAudioManager.Instance != null)
        {
            SnakeAudioManager.Instance.SetBGMVolume(PlayerPrefs.GetFloat(BGMVolumeKey, 0.5f));
            SnakeAudioManager.Instance.SetSFXVolume(PlayerPrefs.GetFloat(SFXVolumeKey, 0.5f));
        }
    }

    public void Save()
    {
        if (screenModeDropdown != null)
            ApplyScreenMode(screenModeDropdown.value);
        if (resolutionDropdown != null && resolutionDropdown.value >= 0 && availableResolutions != null &&
            resolutionDropdown.value < availableResolutions.Length)
            ApplyResolution(resolutionDropdown.value);

        SaveToPlayerPrefs();
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        PauseManager.Instance?.HideSettings();
    }

    public void Back()
    {
        RevertToPlayerPrefs();
        SnakeAudioManager.Instance?.PlayButtonClickSfx();
        PauseManager.Instance?.HideSettings();
    }

    public void BackButton()
    {
        Back();
    }
}
