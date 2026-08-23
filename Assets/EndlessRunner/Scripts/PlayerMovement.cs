using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform GFX;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform feetPos;
    [SerializeField] private float groundDistance = 0.25f;
    [SerializeField] private float jumpTime = 0.3f;
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private AnimatorController animatorController;
    [SerializeField] private float doubleJumpDuration = 8f;

    private bool isGrounded = false;
    private bool isJumping = false;
    private bool wasCrouching = false;
    private float jumpTimer;
    private float doubleJumpTimeRemaining;
    private int jumpsUsed;

    private void Start()
    {
        if (animatorController == null)
        {
            animatorController = GetComponent<AnimatorController>();
        }

        UpdateAnimationState(false);
    }

    private void Update()
    {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundDistance, groundLayer);
        bool isStableGrounded = isGrounded && rb.velocity.y <= 0.05f;

        if (isStableGrounded)
        {
            jumpsUsed = 0;
            isJumping = false;
            jumpTimer = 0f;
        }

        if (doubleJumpTimeRemaining > 0f)
        {
            doubleJumpTimeRemaining -= Time.deltaTime;

            if (doubleJumpTimeRemaining <= 0f)
            {
                doubleJumpTimeRemaining = 0f;
            }
        }

        if (CubeGameManager.Instance == null || !CubeGameManager.Instance.isPlaying)
        {
            UpdateAnimationState(false);
            return;
        }

        #region JUMPING

        int maxJumps = doubleJumpTimeRemaining > 0f ? 2 : 1;

        if (Input.GetButtonDown("Jump") && jumpsUsed < maxJumps)
        {
            isJumping = true;
            jumpsUsed++;
            jumpTimer = 0f;
            rb.velocity = Vector2.up * jumpForce;
            AudioManager.Instance?.PlayJumpSfx();
        }
        if (isJumping && Input.GetButton("Jump"))
        {
            if (jumpTimer < jumpTime)
            {
                rb.velocity = Vector2.up * jumpForce;
                jumpTimer += Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
        if (Input.GetButtonUp("Jump"))
        {
            isJumping = false;
            jumpTimer = 0f;
        }

        #endregion
        
        #region CROUCHING

        bool isCrouching = isStableGrounded && Input.GetButton("Crouch");

        if (isCrouching && !wasCrouching)
        {
            AudioManager.Instance?.PlayCrouchSfx();
        }

        if (isCrouching)
        {
            GFX.localScale = new Vector3(GFX.localScale.x, crouchHeight, GFX.localScale.z);
            if (isJumping)
            {
                GFX.localScale = new Vector3(GFX.localScale.x, 1f, GFX.localScale.z);
            }
        }

        if (Input.GetButtonUp("Crouch"))
        {
            GFX.localScale = new Vector3(GFX.localScale.x, 1f, GFX.localScale.z);
        }

        wasCrouching = isCrouching;

        #endregion

        #region ANIMATION

        UpdateAnimationState(isCrouching);

        #endregion
    }

    private void UpdateAnimationState(bool isCrouching)
    {
        if (animatorController == null)
            return;

        bool isPlaying = CubeGameManager.Instance != null && CubeGameManager.Instance.isPlaying;

        if (!isPlaying)
        {
            animatorController.SetAnimationState(AnimatorController.AnimationState.Idle);
            return;
        }

        if (isCrouching)
        {
            animatorController.SetAnimationState(AnimatorController.AnimationState.Crouching);
        }
        else
        {
            animatorController.SetAnimationState(AnimatorController.AnimationState.Running);
        }
    }

    public void ApplyDoubleJump(float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        doubleJumpTimeRemaining = Mathf.Max(doubleJumpTimeRemaining, duration);
    }
}


