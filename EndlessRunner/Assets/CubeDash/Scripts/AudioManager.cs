using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip jumpSfx;
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;

    [Header("BGM")]
    [SerializeField] private AudioClip gameplayBgm;
    [SerializeField] private bool playBgmOnStart = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playBgmOnStart)
        {
            PlayBgm();
        }
    }

    public void PlayJumpSfx()
    {
        PlaySfx(jumpSfx);
    }

    public void PlayHitSfx()
    {
        PlaySfx(hitSfx);
    }

    public void PlayGameOverSfx()
    {
        PlaySfx(gameOverSfx);
    }

    public void PlayButtonClickSfx()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayBgm()
    {
        if (bgmSource == null || gameplayBgm == null)
        {
            return;
        }

        if (bgmSource.clip != gameplayBgm)
        {
            bgmSource.clip = gameplayBgm;
        }

        bgmSource.loop = true;
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
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