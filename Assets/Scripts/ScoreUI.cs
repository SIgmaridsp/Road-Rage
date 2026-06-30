using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the top-centre score HUD.
///
/// Canvas setup (create once in the scene):
///   Canvas (Screen Space - Overlay)
///     └─ ScoreUIRoot  ← put this script here
///          ├─ HitPopup      (TMP_Text, large, centred)   ← hitPopup
///          ├─ ComboTimer    (TMP_Text, smaller)           ← comboTimerText
///          └─ TotalScore    (TMP_Text, top-right corner)  ← totalScoreText
///
/// Wire the three text refs in the Inspector, then wire ScoreManager to this.
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("Text References")]
    [SerializeField] private TMP_Text hitPopup;
    [SerializeField] private TMP_Text comboTimerText;
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Popup timing")]
    [SerializeField] private float holdTime  = 1.4f;
    [SerializeField] private float fadeTime  = 0.4f;

    private Coroutine popupRoutine;

    void Start()
    {
        if (ScoreManager.Instance == null) { enabled = false; return; }

        ScoreManager.Instance.OnHit      += HandleHit;
        ScoreManager.Instance.OnComboEnd += HandleComboEnd;

        SetAlpha(hitPopup,      0f);
        SetAlpha(comboTimerText, 0f);
        UpdateTotal(0);
    }

    void Update()
    {
        if (ScoreManager.Instance == null) return;
        int combo = ScoreManager.Instance.Combo;
        if (combo > 0)
        {
            float t = ScoreManager.Instance.ComboTimeLeft;
            SetAlpha(comboTimerText, 1f);
            comboTimerText.text = $"x{combo} COMBO  {t:F1}s";
        }
        else
        {
            SetAlpha(comboTimerText, 0f);
        }
    }

    private void HandleHit(int pts, int combo)
    {
        if (popupRoutine != null) StopCoroutine(popupRoutine);
        hitPopup.text = combo > 1
            ? $"+{pts:N0}\n<size=70%>x{combo} COMBO!</size>"
            : $"+{pts:N0}";
        popupRoutine = StartCoroutine(PopupFade());
        UpdateTotal(ScoreManager.Instance.TotalScore);
    }

    private void HandleComboEnd()
    {
        SetAlpha(comboTimerText, 0f);
        UpdateTotal(ScoreManager.Instance.TotalScore);
    }

    private void UpdateTotal(int score)
    {
        if (totalScoreText != null)
            totalScoreText.text = $"SCORE  {score:N0}";
    }

    private IEnumerator PopupFade()
    {
        SetAlpha(hitPopup, 1f);
        yield return new WaitForSeconds(holdTime);
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            SetAlpha(hitPopup, 1f - t / fadeTime);
            yield return null;
        }
        SetAlpha(hitPopup, 0f);
        popupRoutine = null;
    }

    private static void SetAlpha(TMP_Text text, float a)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = a;
        text.color = c;
    }
}
