using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ArcadeDeathEffect2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int flashCount = 4;
    [SerializeField, Min(0.01f)] private float flashInterval = 0.08f;
    [SerializeField, Min(0f)] private float destroyDelayAfterFlash = 0.08f;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField, Min(0.1f)] private float deathFrameRate = 12f;
    [SerializeField] private SpriteRenderer targetRenderer;

    private Collider2D[] colliders;
    private Rigidbody2D[] rigidbodies;
    private Behaviour[] behaviours;
    private Coroutine deathRoutine;
    private bool isDying;

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

        colliders = GetComponentsInChildren<Collider2D>(true);
        rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
        behaviours = GetComponentsInChildren<Behaviour>(true);
    }

    public void PlayAndDestroy()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;

        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        deathRoutine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetBehavioursEnabled(false);
        SetPhysicsEnabled(false);

        if (HasDeathFrames())
        {
            yield return PlayDeathFrameAnimation();
        }
        else
        {
            SetRenderersVisible(true);

            for (int i = 0; i < flashCount; i++)
            {
                SetRenderersVisible(false);
                yield return new WaitForSecondsRealtime(flashInterval);
                SetRenderersVisible(true);
                yield return new WaitForSecondsRealtime(flashInterval);
            }

            SetRenderersVisible(false);
        }

        if (destroyDelayAfterFlash > 0f)
        {
            yield return new WaitForSecondsRealtime(destroyDelayAfterFlash);
        }

        Destroy(gameObject);
    }

    private void SetRenderersVisible(bool isVisible)
    {
        if (!targetRenderer)
        {
            return;
        }

        targetRenderer.enabled = isVisible;
    }

    private IEnumerator PlayDeathFrameAnimation()
    {
        float secondsPerFrame = 1f / deathFrameRate;

        for (int i = 0; i < deathFrames.Length; i++)
        {
            if (targetRenderer)
            {
                targetRenderer.sprite = deathFrames[i];
            }

            SetRenderersVisible(true);
            yield return new WaitForSecondsRealtime(secondsPerFrame);
        }

        SetRenderersVisible(false);
    }

    private bool HasDeathFrames()
    {
        return deathFrames != null && deathFrames.Length > 0;
    }

    private void SetPhysicsEnabled(bool isEnabled)
    {
        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i])
                {
                    colliders[i].enabled = isEnabled;
                }
            }
        }

        if (rigidbodies != null)
        {
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (!rigidbodies[i])
                {
                    continue;
                }

                rigidbodies[i].velocity = Vector2.zero;
                rigidbodies[i].angularVelocity = 0f;
                rigidbodies[i].simulated = isEnabled;
            }
        }
    }

    private void SetBehavioursEnabled(bool isEnabled)
    {
        if (behaviours == null)
        {
            return;
        }

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] && behaviours[i] != this)
            {
                behaviours[i].enabled = isEnabled;
            }
        }
    }
}