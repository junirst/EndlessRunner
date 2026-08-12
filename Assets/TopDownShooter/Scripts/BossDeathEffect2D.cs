using System.Collections;
using UnityEngine;

// Boss-specific death effect: plays a short body frame animation, then holds
// the corpse on the map with a chosen sprite for a few seconds before destroying it.
[DisallowMultipleComponent]
public class BossDeathEffect2D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer legRenderer;
    [SerializeField] private Sprite[] deathBodyFrames;
    [SerializeField, Min(0.1f)] private float deathFrameRate = 10f;
    [SerializeField] private Sprite finalDeathSprite;
    [SerializeField, Min(0f)] private float corpseLifetime = 3f;

    private Collider2D[] colliders;
    private Rigidbody2D[] rigidbodies;
    private Behaviour[] behaviours;
    private bool isDying;

    private void Awake()
    {
        if (!bodyRenderer)
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
        }

        if (!bodyRenderer)
        {
            bodyRenderer = GetComponentInChildren<SpriteRenderer>(true);
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
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetBehavioursEnabled(false);
        SetPhysicsEnabled(false);

        if (HasDeathFrames())
        {
            yield return PlayDeathFrameAnimation();
        }

        ApplyFinalSprite();

        if (corpseLifetime > 0f)
        {
            yield return new WaitForSecondsRealtime(corpseLifetime);
        }

        Destroy(gameObject);
    }

    private bool HasDeathFrames()
    {
        return bodyRenderer != null && deathBodyFrames != null && deathBodyFrames.Length > 0;
    }

    private IEnumerator PlayDeathFrameAnimation()
    {
        float secondsPerFrame = 1f / deathFrameRate;

        for (int i = 0; i < deathBodyFrames.Length; i++)
        {
            if (deathBodyFrames[i])
            {
                bodyRenderer.sprite = deathBodyFrames[i];
            }

            yield return new WaitForSecondsRealtime(secondsPerFrame);
        }
    }

    private void ApplyFinalSprite()
    {
        if (!finalDeathSprite)
        {
            return;
        }

        if (bodyRenderer)
        {
            bodyRenderer.sprite = finalDeathSprite;
        }

        if (legRenderer)
        {
            legRenderer.sprite = finalDeathSprite;
        }
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
