using System.Collections;
using UnityEngine;

public class TitleAudioManager : MonoBehaviour
{
    public static TitleAudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("BGM")]
    [SerializeField] private AudioClip titleBgm;
    [SerializeField] private bool playBgmOnStart = true;

    [Header("SFX")]
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
        StartCoroutine(InitializeTitleAudio());
    }

    public void PlayBgm()
    {
        if (bgmSource == null || titleBgm == null)
        {
            return;
        }

        if (bgmSource.clip != titleBgm)
        {
            bgmSource.clip = titleBgm;
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

    public void PlayButtonClickSfx()
    {
        if (sfxSource == null || buttonClickSfx == null)
        {
            return;
        }

        sfxSource.PlayOneShot(buttonClickSfx);
    }

    private IEnumerator InitializeTitleAudio()
    {
        // Wait one frame so every object's Start has run.
        yield return null;

        StopKnownGameBgm();
        StopOtherAudioSources();

        // A second pass catches delayed audio starts.
        yield return new WaitForSecondsRealtime(0.1f);

        StopKnownGameBgm();
        StopOtherAudioSources();

        if (playBgmOnStart)
        {
            PlayBgm();
        }
    }

    private void StopKnownGameBgm()
    {
        ShooterAudioManager.Instance?.StopBgm();
        MiniGolfAudioManager.Instance?.StopBgm();
        AudioManager.Instance?.StopBgm();
    }

    private void StopOtherAudioSources()
    {
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>(true);

        for (int i = 0; i < allAudioSources.Length; i++)
        {
            AudioSource source = allAudioSources[i];
            if (source == null)
            {
                continue;
            }

            if (source == sfxSource)
            {
                continue;
            }

            if (source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}
