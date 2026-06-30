using System;
using UnityEngine;

/// <summary>
/// Tracks total score and combo multiplier.
/// Each hit within comboWindow seconds stacks the multiplier.
/// Points per hit = baseScore * combo * speed factor.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring")]
    [SerializeField] private int baseScore = 100;
    [Tooltip("Seconds between hits before combo resets.")]
    [SerializeField] private float comboWindow = 3f;

    public int TotalScore  { get; private set; }
    public int Combo       { get; private set; }
    public float ComboTimeLeft => comboTimer;
    public float ComboWindow   => comboWindow;

    /// Fired on every hit: (pointsThisHit, currentCombo)
    public event Action<int, int> OnHit;
    /// Fired when the combo window expires
    public event Action OnComboEnd;

    private float comboTimer;

    void Awake() => Instance = this;

    void Update()
    {
        if (Combo <= 0) return;
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f) EndCombo();
    }

    /// Call this from CarImpactHandler when a pedestrian is hit.
    public void RegisterHit(float speed)
    {
        Combo++;
        comboTimer = comboWindow;

        // Speed bonus: 1x at 5 m/s, caps at 5x at 25+ m/s
        float speedMult = Mathf.Clamp(speed / 5f, 1f, 5f);
        int pts = Mathf.RoundToInt(baseScore * Combo * speedMult);
        TotalScore += pts;

        OnHit?.Invoke(pts, Combo);
    }

    private void EndCombo()
    {
        Combo = 0;
        comboTimer = 0f;
        OnComboEnd?.Invoke();
    }
}
