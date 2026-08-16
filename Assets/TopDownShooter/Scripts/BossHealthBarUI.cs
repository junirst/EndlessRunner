using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Keep this component's own GameObject active at all times; visibility is
// driven by a CanvasGroup fade so the bar can auto-show/hide with the boss
// even if a designer previously disabled the panel in the Hierarchy.
[DisallowMultipleComponent]
public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI bossNameText;
    [SerializeField] private TextMeshProUGUI healthValueText;

    [Header("Show / Hide")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

    [Header("Style")]
    [SerializeField] private Gradient healthGradient = CreateDefaultGradient();
    [SerializeField, Min(1f)] private float fillLerpSpeed = 6f;
    [SerializeField] private bool pulseOnDamage = true;
    [SerializeField, Min(1f)] private float pulseScale = 1.12f;
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.15f;

    private BossEnemy activeBoss;
    private Health activeBossHealth;
    private float displayedHealth;
    private float targetHealth;
    private Coroutine fadeRoutine;
    private Coroutine pulseRoutine;
    private RectTransform pulseTarget;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>(true);
        }

        if (fillImage == null && healthSlider != null && healthSlider.fillRect != null)
        {
            fillImage = healthSlider.fillRect.GetComponent<Image>();
        }

        pulseTarget = healthSlider != null ? (RectTransform)healthSlider.transform : (RectTransform)transform;

        SetVisibleImmediate(false);
    }

    private void OnEnable()
    {
        BossEnemy.BossSpawned += HandleBossSpawned;
        BossEnemy.BossDespawned += HandleBossDespawned;

        if (BossEnemy.CurrentBoss != null)
        {
            BindBoss(BossEnemy.CurrentBoss);
        }
    }

    private void OnDisable()
    {
        BossEnemy.BossSpawned -= HandleBossSpawned;
        BossEnemy.BossDespawned -= HandleBossDespawned;
        UnbindBoss();
    }

    private void HandleBossSpawned(BossEnemy boss)
    {
        BindBoss(boss);
    }

    private void HandleBossDespawned(BossEnemy boss)
    {
        if (boss == activeBoss)
        {
            UnbindBoss();
            HideBar();
        }
    }

    private void BindBoss(BossEnemy boss)
    {
        UnbindBoss();

        activeBoss = boss;
        if (activeBoss == null)
        {
            HideBar();
            return;
        }

        activeBossHealth = activeBoss.Health;
        if (activeBossHealth == null)
        {
            HideBar();
            return;
        }

        activeBossHealth.HealthChanged += HandleBossHealthChanged;

        if (bossNameText != null)
        {
            bossNameText.text = activeBoss.BossDisplayName;
        }

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = activeBossHealth.MaxHealth;
        }

        displayedHealth = activeBossHealth.CurrentHealth;
        targetHealth = activeBossHealth.CurrentHealth;

        ShowBar();
        UpdateTarget(activeBossHealth);
    }

    private void UnbindBoss()
    {
        if (activeBossHealth != null)
        {
            activeBossHealth.HealthChanged -= HandleBossHealthChanged;
        }

        activeBoss = null;
        activeBossHealth = null;
    }

    private void HandleBossHealthChanged(Health health)
    {
        UpdateTarget(health);

        if (health != null && health.CurrentHealth <= 0f)
        {
            HideBar();
        }
    }

    private void Update()
    {
        if (healthSlider == null)
        {
            return;
        }

        displayedHealth = Mathf.MoveTowards(displayedHealth, targetHealth, healthSlider.maxValue * fillLerpSpeed * Time.deltaTime);
        healthSlider.value = displayedHealth;
        ApplyFillColor();
    }

    private void UpdateTarget(Health health)
    {
        if (health == null || healthSlider == null)
        {
            return;
        }

        bool tookDamage = targetHealth > health.CurrentHealth;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = health.MaxHealth;
        targetHealth = health.CurrentHealth;

        if (healthValueText != null)
        {
            healthValueText.text = health.GetHealthText();
        }

        if (tookDamage && pulseOnDamage)
        {
            PlayPulse();
        }
    }

    private void ApplyFillColor()
    {
        if (fillImage == null || healthSlider == null || healthSlider.maxValue <= 0f)
        {
            return;
        }

        float normalized = Mathf.Clamp01(displayedHealth / healthSlider.maxValue);
        fillImage.color = healthGradient.Evaluate(normalized);
    }

    private void PlayPulse()
    {
        if (pulseTarget == null)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
        }

        pulseRoutine = StartCoroutine(PulseRoutine());
    }

    private IEnumerator PulseRoutine()
    {
        Vector3 baseScale = Vector3.one;
        float half = pulseDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            pulseTarget.localScale = Vector3.Lerp(baseScale, baseScale * pulseScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            pulseTarget.localScale = Vector3.Lerp(baseScale * pulseScale, baseScale, t);
            yield return null;
        }

        pulseTarget.localScale = baseScale;
        pulseRoutine = null;
    }

    private void ShowBar()
    {
        SetVisible(true);
    }

    private void HideBar()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(visible));
    }

    private void SetVisibleImmediate(bool visible)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private IEnumerator FadeRoutine(bool visible)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = visible ? 1f : 0f;

        if (visible)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;

        if (!visible)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            displayedHealth = 0f;
            targetHealth = 0f;

            if (healthSlider != null)
            {
                healthSlider.value = 0f;
            }

            if (healthValueText != null)
            {
                healthValueText.text = string.Empty;
            }
        }

        fadeRoutine = null;
    }

    private static Gradient CreateDefaultGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.15f, 0.15f), 0f),
                new GradientColorKey(new Color(0.95f, 0.75f, 0.15f), 0.5f),
                new GradientColorKey(new Color(0.2f, 0.8f, 0.3f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });

        return gradient;
    }
}
