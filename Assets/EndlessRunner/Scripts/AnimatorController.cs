using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    #region Animation States
    public enum AnimationState
    {
        Idle,
        Running,
        Crouching,
        Dead
    }
    #endregion

    #region Inspector Fields
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float animationFrameRate = 0.1f;

    [SerializeField] private Sprite[] idleFrames = new Sprite[4];
    [SerializeField] private Sprite[] runningFrames = new Sprite[16];
    [SerializeField] private Sprite[] crouchingFrames = new Sprite[8];
    [SerializeField] private Sprite[] deadFrames = new Sprite[4];
    #endregion

    #region Private Variables
    private AnimationState currentState = AnimationState.Idle;
    private AnimationState previousState = AnimationState.Idle;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private bool isLocked = false;
    #endregion

    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (currentState != previousState)
        {
            currentFrameIndex = 0;
            frameTimer = 0f;
            previousState = currentState;
        }

        Sprite[] currentFrames = GetCurrentFrameArray();
        if (currentFrames.Length == 0)
        {
            return;
        }

        spriteRenderer.sprite = currentFrames[currentFrameIndex];

        frameTimer += Time.deltaTime;

        if (frameTimer >= animationFrameRate)
        {
            frameTimer = 0f;
            currentFrameIndex++;

            if (currentFrameIndex >= currentFrames.Length)
            {
                currentFrameIndex = 0;
            }

            spriteRenderer.sprite = currentFrames[currentFrameIndex];
        }
    }

    private Sprite[] GetCurrentFrameArray()
    {
        return currentState switch
        {
            AnimationState.Idle => idleFrames,
            AnimationState.Running => runningFrames,
            AnimationState.Crouching => crouchingFrames,
            AnimationState.Dead => deadFrames,
            _ => idleFrames
        };
    }

    #region Public Methods
    public void SetAnimationState(AnimationState state)
    {
        if (isLocked)
        {
            return;
        }

        currentState = state;
    }

    public void LockAnimationState(AnimationState state)
    {
        isLocked = true;
        currentState = state;
        previousState = state;
        currentFrameIndex = 0;
        frameTimer = 0f;

        Sprite[] currentFrames = GetCurrentFrameArray();
        if (spriteRenderer != null && currentFrames.Length > 0)
        {
            spriteRenderer.sprite = currentFrames[0];
        }
    }

    public void ResetAnimationState(AnimationState state = AnimationState.Idle)
    {
        isLocked = false;
        currentState = state;
        previousState = state;
        currentFrameIndex = 0;
        frameTimer = 0f;

        Sprite[] currentFrames = GetCurrentFrameArray();
        if (spriteRenderer != null && currentFrames.Length > 0)
        {
            spriteRenderer.sprite = currentFrames[0];
        }
    }

    public float GetAnimationDuration(AnimationState state)
    {
        Sprite[] frames = state switch
        {
            AnimationState.Idle => idleFrames,
            AnimationState.Running => runningFrames,
            AnimationState.Crouching => crouchingFrames,
            AnimationState.Dead => deadFrames,
            _ => idleFrames
        };

        return frames.Length * animationFrameRate;
    }

    public AnimationState GetCurrentAnimationState()
    {
        return currentState;
    }
    #endregion
}
