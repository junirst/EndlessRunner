using UnityEngine;

[DisallowMultipleComponent]
public class MotionDrivenSpriteAnimator2D : MonoBehaviour
{
    [SerializeField] private Rigidbody2D movementBody;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames = new Sprite[7];
    [SerializeField, Min(0.1f)] private float frameRate = 12f;
    [SerializeField, Min(0f)] private float idleThreshold = 0.01f;
    [SerializeField, Min(0f)] private float stopGraceDuration = 0.15f;

    private bool wasMoving;
    private float frameTimer;
    private int currentFrame;
    private float lastMovingTime = float.NegativeInfinity;

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
        }
    }

    private void Update()
    {
        // Physics can momentarily zero the velocity on contact with the player/props;
        // hold the moving state briefly so the legs don't vanish on every collision.
        bool physicallyMoving = movementBody && movementBody.velocity.sqrMagnitude > idleThreshold * idleThreshold;
        if (physicallyMoving)
        {
            lastMovingTime = Time.time;
        }

        bool isMoving = Time.time - lastMovingTime <= stopGraceDuration;

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
                targetRenderer.enabled = false;
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
