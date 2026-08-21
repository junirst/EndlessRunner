using UnityEngine;

public class ShooterAudioManager : MonoBehaviour
{
    public static ShooterAudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip playerShootSfx;
    [SerializeField] private AudioClip playerMovementSfx;
    [SerializeField] private AudioClip enemyShootSfx;
    [SerializeField] private AudioClip enemyMovementSfx;
    [SerializeField] private AudioClip bossAttackSfx;
    [SerializeField] private AudioClip bossMovementSfx;
    [SerializeField] private AudioClip enemyDeathSfx;
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

    public void PlayPlayerShootSfx()
    {
        PlaySfx(playerShootSfx);
    }

    public void SetPlayerMovementSfx(GameObject actor, bool isMoving)
    {
        SetMovementSfx(actor, playerMovementSfx, isMoving);
    }

    public void PlayEnemyShootSfx()
    {
        PlaySfx(enemyShootSfx);
    }

    public void SetEnemyMovementSfx(GameObject actor, bool isMoving)
    {
        SetMovementSfx(actor, enemyMovementSfx, isMoving);
    }

    public void PlayBossAttackSfx()
    {
        PlaySfx(bossAttackSfx);
    }

    public void SetBossMovementSfx(GameObject actor, bool isMoving)
    {
        SetMovementSfx(actor, bossMovementSfx, isMoving);
    }

    public void PlayEnemyDeathSfx()
    {
        PlaySfx(enemyDeathSfx);
    }

    public void PlayGameOverSfx(AudioClip clipOverride = null)
    {
        AudioClip clipToPlay = clipOverride != null ? clipOverride : gameOverSfx;

        if (clipToPlay == null)
        {
            clipToPlay = Resources.Load<AudioClip>("topdownshootergameover");
        }

        PlaySfx(clipToPlay);
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

    private void SetMovementSfx(GameObject actor, AudioClip clip, bool isMoving)
    {
        if (!actor || clip == null)
        {
            return;
        }

        AudioSource movementSource = actor.GetComponent<AudioSource>();
        if (movementSource == null)
        {
            movementSource = actor.AddComponent<AudioSource>();
            movementSource.playOnAwake = false;
            movementSource.loop = true;
            movementSource.spatialBlend = 0f;
        }

        movementSource.clip = clip;
        if (isMoving)
        {
            if (!movementSource.isPlaying)
            {
                movementSource.Play();
            }
        }
        else if (movementSource.isPlaying)
        {
            movementSource.Stop();
        }
    }
}
