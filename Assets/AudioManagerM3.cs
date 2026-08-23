using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManagerM3 : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip match3Sfx;
    [SerializeField] private AudioClip match4Sfx;
    [SerializeField] private AudioClip match5Sfx;
    [SerializeField] private AudioClip gameOverSfx;

    [Header("BGM")]
    [SerializeField] private AudioClip gameplayBgm;
    [SerializeField] private bool playBgmOnStart = false;

    // Start is called before the first frame update
    void Start()
    {
        if (playBgmOnStart)
        {
            bgmSource.clip = gameplayBgm;
            bgmSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxSource.PlayOneShot(clip);
    }

    // Helper to play the correct match SFX based on number of matched pieces
    public void PlayMatchSfx(int matchCount)
    {
        AudioClip clipToPlay = match3Sfx;
        if (matchCount >= 5)
        {
            clipToPlay = match5Sfx;
        }
        else if (matchCount == 4)
        {
            clipToPlay = match4Sfx;
        }

        if (clipToPlay != null)
        {
            Debug.Log($"AudioManagerM3: Playing match SFX for matchCount={matchCount}, clip={clipToPlay.name}");
            PlaySFX(clipToPlay);
        }
    }
}
