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
        set => bgmSource.volume = value;
    }

    public float SFXVolume
    {
        get => sfxSource.volume;
        set => sfxSource.volume = value;
    }

    public void SetBGMVolume(float value)
    {
        bgmSource.volume = value;
    }

    public void SetSFXVolume(float value)
    {
        sfxSource.volume = value;
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

    public void PlayGameOverSfx(AudioClip clipOverride = null)
    {
        AudioClip clipToPlay = clipOverride != null ? clipOverride : gameOverSfx;

        if (clipToPlay == null)
        {
            clipToPlay = Resources.Load<AudioClip>("snakegameover");
        }

        if (clipToPlay != null && sfxSource != null)
            sfxSource.PlayOneShot(clipToPlay);
    }

    public void PlayButtonClickSfx()
    {
        if (buttonClickSfx != null && sfxSource != null)
            sfxSource.PlayOneShot(buttonClickSfx);
    }
}
