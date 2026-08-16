using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float currentHealth = 100f;

    public event Action<Health> HealthChanged;
    public event Action<Health> Died;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float NormalizedHealth => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);

        if (currentHealth <= 0f || currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void SetMaxHealth(float value, bool refillHealth = true)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = refillHealth ? maxHealth : Mathf.Min(currentHealth, maxHealth);
        HealthChanged?.Invoke(this);
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        HealthChanged?.Invoke(this);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        HealthChanged?.Invoke(this);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        HealthChanged?.Invoke(this);

        if (currentHealth <= 0f)
        {
            Died?.Invoke(this);
        }
    }

    public string GetHealthText()
    {
        return Mathf.CeilToInt(currentHealth) + "/" + Mathf.CeilToInt(maxHealth);
    }
}