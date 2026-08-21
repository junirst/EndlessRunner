using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileSpriteAnimator2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames = new Sprite[7];
    [SerializeField, Min(0.1f)] private float frameRate = 12f;

    private float frameTimer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        if (!targetRenderer)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (!targetRenderer)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }
    }

    private void OnEnable()
    {
        Play();
    }

    private void Update()
    {
        if (!isPlaying || !targetRenderer || frames == null || frames.Length == 0)
        {
            return;
        }

        frameTimer += Time.deltaTime;
        float secondsPerFrame = 1f / frameRate;

        while (frameTimer >= secondsPerFrame)
        {
            frameTimer -= secondsPerFrame;

            if (currentFrame >= frames.Length - 1)
            {
                isPlaying = false;
                currentFrame = frames.Length - 1;
                ApplyFrame();
                return;
            }

            currentFrame++;
            ApplyFrame();
        }
    }

    public void Play()
    {
        if (!targetRenderer || frames == null || frames.Length == 0)
        {
            return;
        }

        isPlaying = true;
        frameTimer = 0f;
        currentFrame = 0;
        targetRenderer.enabled = true;
        ApplyFrame();
    }

    private void ApplyFrame()
    {
        if (!targetRenderer || frames == null || frames.Length == 0)
        {
            return;
        }

        Sprite frame = frames[Mathf.Clamp(currentFrame, 0, frames.Length - 1)];
        if (frame)
        {
            targetRenderer.sprite = frame;
        }
    }
}
