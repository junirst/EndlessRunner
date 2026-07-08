using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollison : MonoBehaviour
{
    private AnimatorController animatorController;
    private PlayerMovement playerMovement;
    private Rigidbody2D playerRigidbody;
    private Collider2D playerCollider;
    private bool isDying = false;
    [SerializeField] private GameObject shieldVisual;
    [SerializeField] private SpriteRenderer[] blinkRenderers;
    [SerializeField] private float invulnerabilityDuration = 2f;
    [SerializeField] private float blinkInterval = 0.15f;
    private bool hasShield = false;
    private bool isInvulnerable = false;
    private Coroutine invulnerabilityCoroutine;

    private void Start()
    {
        animatorController = GetComponent<AnimatorController>();
        playerMovement = GetComponent<PlayerMovement>();
        playerRigidbody = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        CubeGameManager.Instance.onPlay.AddListener(ActivatePlayer);
    }
    private void ActivatePlayer () 
    {
        isDying = false;
        hasShield = false;
        isInvulnerable = false;

        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }

        SetBlinkState(true);

        if (animatorController != null)
        {
            animatorController.ResetAnimationState(AnimatorController.AnimationState.Idle);
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.simulated = true;
            playerRigidbody.velocity = Vector2.zero;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }

        UpdateShieldVisual();

        gameObject.SetActive(true);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDying || collision.gameObject.tag != "Obstacle" || isInvulnerable)
        {
            return;
        }

        if (hasShield)
        {
            ConsumeShield();
            return;
        }

        isDying = true;
        AudioManager.Instance?.PlayHitSfx();

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerRigidbody != null)
        {
            playerRigidbody.velocity = Vector2.zero;
            playerRigidbody.simulated = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }

        if (animatorController != null)
        {
            animatorController.LockAnimationState(AnimatorController.AnimationState.Dead);
        }

        StartCoroutine(HandleDeath());
        CubeGameManager.Instance.GameOver();
    }

    private IEnumerator HandleDeath()
    {
        float deathDuration = 0.4f;

        if (animatorController != null)
        {
            deathDuration = animatorController.GetAnimationDuration(AnimatorController.AnimationState.Dead);
        }

        yield return new WaitForSeconds(deathDuration);
        gameObject.SetActive(false);
    }

    public void ApplyShield()
    {
        hasShield = true;
        UpdateShieldVisual();
    }

    private void ConsumeShield()
    {
        hasShield = false;
        UpdateShieldVisual();
        StartInvulnerability();
    }

    private void StartInvulnerability()
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }

        invulnerabilityCoroutine = StartCoroutine(HandleInvulnerability());
    }

    private IEnumerator HandleInvulnerability()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        bool visible = true;

        while (elapsed < invulnerabilityDuration)
        {
            visible = !visible;
            SetBlinkState(visible);
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        isInvulnerable = false;
        SetBlinkState(true);
        invulnerabilityCoroutine = null;
    }

    private void UpdateShieldVisual()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(hasShield);
        }
    }

    private void SetBlinkState(bool visible)
    {
        if (blinkRenderers == null)
        {
            return;
        }

        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (blinkRenderers[i] != null)
            {
                blinkRenderers[i].enabled = visible;
            }
        }
    }
}

