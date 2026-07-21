using UnityEngine;

public class Match3AudioManager : MonoBehaviour
{
    public static Match3AudioManager Instance;

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip gameOverSfx;

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

    public void PlayGameOverSfx(AudioClip clipOverride = null)
    {
        AudioClip clipToPlay = clipOverride != null ? clipOverride : gameOverSfx;

        if (clipToPlay == null)
        {
            clipToPlay = Resources.Load<AudioClip>("match3gameover");
        }

        PlaySfx(clipToPlay);
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
