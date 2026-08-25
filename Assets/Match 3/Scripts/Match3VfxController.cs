using UnityEngine;

public class Match3VfxController : MonoBehaviour
{
    private const string ExplosionSpriteResourcePath = "Match 3/Explostion effect art";
    private const float HintPulseSpeed = 1.1f;
    private const float HintBaseAlpha = 0.18f;
    private const float HintPulseAlpha = 0.12f;
    private const float HintBaseScale = 1.05f;
    private const float HintPulseScale = 0.08f;
    private const float ExplosionDuration = 0.32f;
    private const float ExplosionStartScale = 0.04f;
    private const float ExplosionEndScale = 0.25f;
    private const float ExplosionStartAlpha = 1f;
    private const float ExplosionEndAlpha = 0f;

    private readonly Color explosionTint = new Color(1f, 0.72f, 0.18f, 1f);
    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private bool isHint;

    /// <summary>Configures an instantiated hint effect as a subtle pulsing marker.</summary>
    public static void ConfigureHint(GameObject hintObject)
    {
        Match3VfxController controller = hintObject.GetComponent<Match3VfxController>();
        if (controller == null)
        {
            controller = hintObject.AddComponent<Match3VfxController>();
        }

        controller.isHint = true;
        controller.ConfigureHintRenderer();
    }

    /// <summary>Configures an instantiated destroy effect as a short expanding explosion.</summary>
    public static void ConfigureExplosion(GameObject explosionObject)
    {
        Match3VfxController controller = explosionObject.GetComponent<Match3VfxController>();
        if (controller == null)
        {
            controller = explosionObject.AddComponent<Match3VfxController>();
        }

        controller.isHint = false;
        controller.ConfigureExplosionRenderer();
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (isHint)
        {
            AnimateHint();
        }
        else
        {
            AnimateExplosion();
        }
    }

    private void ConfigureHintRenderer()
    {
        elapsed = 0f;
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = 0.38f;
            main.startColor = new Color(0.45f, 0.9f, 1f, 0.1f);

            var emission = particleSystem.emission;
            emission.rateOverTime = 1.25f;
            emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);
        }

        transform.localScale = Vector3.one * HintBaseScale;
        ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer particleRenderer in renderers)
        {
            particleRenderer.sortingOrder = 2;
        }
    }

    private void ConfigureExplosionRenderer()
    {
        elapsed = 0f;
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.gameObject.SetActive(false);
        }

        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer == null)
        {
            GameObject spriteObject = new GameObject("Explosion Sprite");
            spriteObject.transform.SetParent(transform, false);
            spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = Resources.Load<Sprite>(ExplosionSpriteResourcePath);
        spriteRenderer.color = explosionTint;
        spriteRenderer.sortingOrder = 10;
        transform.localScale = Vector3.one * ExplosionStartScale;
    }

    private void AnimateHint()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * HintPulseSpeed * Mathf.PI * 2f);
        float scale = HintBaseScale + pulse * HintPulseScale;
        transform.localScale = Vector3.one * scale;

        ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (ParticleSystemRenderer particleRenderer in renderers)
        {
            particleRenderer.material.color = new Color(0.45f, 0.9f, 1f, HintBaseAlpha + pulse * HintPulseAlpha);
        }
    }

    private void AnimateExplosion()
    {
        float normalizedTime = Mathf.Clamp01(elapsed / ExplosionDuration);
        float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);
        float scale = Mathf.Lerp(ExplosionStartScale, ExplosionEndScale, easedTime);
        transform.localScale = Vector3.one * scale;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = Mathf.Lerp(ExplosionStartAlpha, ExplosionEndAlpha, normalizedTime);
            spriteRenderer.color = color;
        }

        if (normalizedTime >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
