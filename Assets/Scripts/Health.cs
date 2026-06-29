using System;
using UnityEngine;

/// <summary>
/// Generic HP. Use it on the car (player health) and optionally on NPCs.
/// Subscribe to OnChanged for a health bar, and OnDeath for game-over / respawn.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    /// <summary>(current, max) — wire this to a UI Slider.</summary>
    public event Action<float, float> OnChanged;
    public event Action OnDeath;

    void Awake() => Current = maxHealth;

    public void ResetHealth()
    {
        Current = maxHealth;
        OnChanged?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Max(0f, Current - amount);
        OnChanged?.Invoke(Current, maxHealth);
        if (Current <= 0f) OnDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        Current = Mathf.Min(maxHealth, Current + amount);
        OnChanged?.Invoke(Current, maxHealth);
    }
}
