using UnityEngine;

public class MiniGolfAudioManager : MonoBehaviour
{
    public static MiniGolfAudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("BGM")]
    [SerializeField] private AudioClip defaultBgm;
    [SerializeField] private bool playBgmOnStart = true;

    [Header("SFX")]
    [SerializeField] private AudioClip shotSfx;
    [SerializeField] private AudioClip goalSfx;
    [SerializeField] private AudioClip levelCompleteSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private void Start()
    {
        if (playBgmOnStart)
        {
            PlayBgm(defaultBgm);
        }
    }

    public void PlayShotSfx()
    {
        PlaySfx(shotSfx);
    }

    public void PlayGoalSfx()
    {
        PlaySfx(goalSfx);
    }

    public void PlayLevelCompleteSfx()
    {
        PlaySfx(levelCompleteSfx);
    }

    public void PlayGameOverSfx()
    {
        PlaySfx(gameOverSfx);
    }

    public void PlayButtonClickSfx()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null || clip == null)
        {
            return;
        }

        if (bgmSource.clip != clip)
        {
            bgmSource.clip = clip;
        }

        bgmSource.loop = true;
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmSource == null || !bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.Stop();
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}