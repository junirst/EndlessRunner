using UnityEngine;

public class SnakeAudioManager : MonoBehaviour
{
    public static SnakeAudioManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip eatSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    private const string BGMVolumeKey = "SnakeBGMVolume";
    private const string SFXVolumeKey = "SnakeSFXVolume";

    public float BGMVolume
    {
        get => bgmSource.volume;
        set
        {
            bgmSource.volume = value;
            PlayerPrefs.SetFloat(BGMVolumeKey, value);
            PlayerPrefs.Save();
        }
    }

    public float SFXVolume
    {
        get => sfxSource.volume;
        set
        {
            sfxSource.volume = value;
            PlayerPrefs.SetFloat(SFXVolumeKey, value);
            PlayerPrefs.Save();
        }
    }

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
        bgmSource.volume = PlayerPrefs.GetFloat(BGMVolumeKey, 0.5f);
        sfxSource.volume = PlayerPrefs.GetFloat(SFXVolumeKey, 0.5f);
        PlayBgm();
    }

    public void PlayBgm()
    {
        if (bgmClip != null && bgmSource != null)
        {
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    public void PlayEatSfx()
    {
        if (eatSfx != null && sfxSource != null)
            sfxSource.PlayOneShot(eatSfx);
    }

    public void PlayGameOverSfx()
    {
        if (gameOverSfx != null && sfxSource != null)
            sfxSource.PlayOneShot(gameOverSfx);
    }

    public void PlayButtonClickSfx()
    {
        if (buttonClickSfx != null && sfxSource != null)
            sfxSource.PlayOneShot(buttonClickSfx);
    }
}
