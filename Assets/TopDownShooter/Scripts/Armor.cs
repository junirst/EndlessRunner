using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Armor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float maxArmor = 100f;
    [SerializeField, Min(0f)] private float currentArmor = 100f;

    public event Action<Armor> ArmorChanged;
    public event Action<Armor> Depleted;

    public float MaxArmor => maxArmor;
    public float CurrentArmor => currentArmor;
    public float NormalizedArmor => maxArmor <= 0f ? 0f : currentArmor / maxArmor;

    private void Awake()
    {
        maxArmor = Mathf.Max(0f, maxArmor);
        currentArmor = Mathf.Clamp(currentArmor, 0f, maxArmor);
    }

    public void SetMaxArmor(float value, bool refillArmor = true)
    {
        maxArmor = Mathf.Max(0f, value);
        currentArmor = refillArmor ? maxArmor : Mathf.Min(currentArmor, maxArmor);
        ArmorChanged?.Invoke(this);
    }

    public void ResetArmor()
    {
        currentArmor = maxArmor;
        ArmorChanged?.Invoke(this);
    }

    public void Repair(float amount)
    {
        if (amount <= 0f || currentArmor >= maxArmor)
        {
            return;
        }

        currentArmor = Mathf.Min(maxArmor, currentArmor + amount);
        ArmorChanged?.Invoke(this);
    }

    // Returns remaining damage after armor absorption.
    public float AbsorbDamage(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        if (currentArmor <= 0f)
        {
            return amount;
        }

        float absorbed = Mathf.Min(currentArmor, amount);
        currentArmor -= absorbed;
        ArmorChanged?.Invoke(this);

        if (currentArmor <= 0f)
        {
            Depleted?.Invoke(this);
        }

        return amount - absorbed;
    }
}