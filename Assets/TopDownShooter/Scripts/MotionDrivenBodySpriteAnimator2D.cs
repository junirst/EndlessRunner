using UnityEngine;

[DisallowMultipleComponent]
public class MotionDrivenBodySpriteAnimator2D : MonoBehaviour
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
        frameTimer = 0f;
        currentFrame = 0;
        isPlaying = false;

        if (targetRenderer && frames.Length > 0 && frames[0])
        {
            targetRenderer.sprite = frames[0];
        }
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
            currentFrame = (currentFrame + 1) % frames.Length;
            ApplyFrame();

            if (currentFrame == frames.Length - 1)
            {
                isPlaying = false;
                currentFrame = 0;
                frameTimer = 0f;
                ApplyFrame();
                return;
            }
        }
    }

    public void TriggerAttack()
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

        Sprite frame = frames[currentFrame % frames.Length];
        if (frame)
        {
            targetRenderer.sprite = frame;
        }
    }
}
