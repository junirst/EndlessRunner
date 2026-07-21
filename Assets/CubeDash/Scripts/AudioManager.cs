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
    [SerializeField] private AudioClip crouchSfx;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    [SerializeField] private AudioClip buttonHoverSfx;
    [SerializeField] private AudioClip powerUpPickupSfx;

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

    public void PlayCrouchSfx()
    {
        PlaySfx(crouchSfx);
    }

    public void PlayGameOverSfx(AudioClip clipOverride = null)
    {
        PlaySfx(GetGameOverClip(clipOverride));
    }

    private AudioClip GetGameOverClip(AudioClip clipOverride)
    {
        if (clipOverride != null)
        {
            return clipOverride;
        }

        if (gameOverSfx != null)
        {
            return gameOverSfx;
        }

        AudioClip loadedClip = Resources.Load<AudioClip>("cubedashgameover");
        if (loadedClip == null)
        {
            loadedClip = Resources.Load<AudioClip>("CubeDash/cubedashgameover");
        }

        if (loadedClip == null)
        {
            loadedClip = Resources.Load<AudioClip>("CubeDash/Resources/cubedashgameover");
        }

        return loadedClip;
    }

    public void PlayButtonClickSfx()
    {
        PlaySfx(buttonClickSfx);
    }

    public void PlayButtonHoverSfx()
    {
        PlaySfx(buttonHoverSfx);
    }

    public void PlayPowerUpPickupSfx()
    {
        PlaySfx(powerUpPickupSfx);
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