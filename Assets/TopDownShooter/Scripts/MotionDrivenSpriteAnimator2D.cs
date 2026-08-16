using UnityEngine;

[DisallowMultipleComponent]
public class MotionDrivenSpriteAnimator2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D movementBody;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames = new Sprite[7];
    [SerializeField, Min(0.1f)] private float frameRate = 12f;
    [SerializeField, Min(0f)] private float idleThreshold = 0.01f;

    private bool wasMoving;
    private float frameTimer;
    private int currentFrame;

    private void Awake()
    {
        if (!movementBody)
        {
            movementBody = GetComponentInParent<Rigidbody2D>();
        }

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
        wasMoving = false;
        frameTimer = 0f;
        currentFrame = 0;

        if (targetRenderer && frames.Length > 0 && frames[0])
        {
            targetRenderer.sprite = frames[0];
            targetRenderer.enabled = true;
        }
    }

    private void Update()
    {
        bool isMoving = movementBody && movementBody.velocity.sqrMagnitude > idleThreshold * idleThreshold;

        if (!isMoving)
        {
            if (wasMoving)
            {
                wasMoving = false;
                frameTimer = 0f;
                currentFrame = 0;
            }

            if (targetRenderer)
            {
                targetRenderer.enabled = true;
            }

            return;
        }

        if (!wasMoving)
        {
            wasMoving = true;
            frameTimer = 0f;
            currentFrame = 0;
            ApplyFrame();
        }

        if (targetRenderer)
        {
            targetRenderer.enabled = true;
        }

        if (frames == null || frames.Length == 0 || !targetRenderer)
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
        }
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
