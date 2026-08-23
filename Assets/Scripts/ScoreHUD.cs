using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-builds the entire score HUD at runtime — no prefab or manual scene
/// wiring needed. Just add this component to any GameObject in the scene
/// alongside a ScoreManager.
///
/// Layout (Screen Space – Overlay):
///   Top-centre  →  SCORE  12,345
///   Below score →  x3  [||||||||        ]   (combo timer bar, gold)
///   Mid-screen  →  +600  x3 COMBO!         (pop-up, fades out)
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    // ── tuneable in Inspector ──────────────────────────────────────────────
    [Header("Popup timing")]
    [SerializeField] private float popupHold = 1.4f;
    [SerializeField] private float popupFade = 0.4f;
    [Header("Combo timer bar")]
    [SerializeField] private int barSegments = 18;

    // ── runtime refs ───────────────────────────────────────────────────────
    private TextMeshProUGUI totalText;
    private TextMeshProUGUI multiText;
    private TextMeshProUGUI hitText;
    private Coroutine popupRoutine;

    // ── lifecycle ──────────────────────────────────────────────────────────

    void Start()
    {
        BuildCanvas();

        // Wait one frame so ScoreManager.Instance is guaranteed to be set
        StartCoroutine(Subscribe());
    }

    IEnumerator Subscribe()
    {
        yield return null;

        var sm = ScoreManager.Instance;
        if (sm == null) { Debug.LogWarning("ScoreHUD: ScoreManager not found."); yield break; }

        UpdateTotal(sm.TotalScore);

        sm.OnHit      += (pts, combo) => HandleHit(pts, combo, sm);
        sm.OnComboEnd += HandleComboEnd;

        StartCoroutine(TickBar(sm));
    }

    void OnDestroy()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;
        sm.OnComboEnd -= HandleComboEnd;
    }

    // ── event handlers ─────────────────────────────────────────────────────

    private void HandleHit(int pts, int combo, ScoreManager sm)
    {
        UpdateTotal(sm.TotalScore);

        hitText.text = combo > 1
            ? $"<b>+{pts:N0}</b>\n<size=65%>x{combo} COMBO!</size>"
            : $"<b>+{pts:N0}</b>";

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupFade());
    }

    private void HandleComboEnd()
    {
        SetAlpha(multiText, 0f);
    }

    // ── coroutines ─────────────────────────────────────────────────────────

    /// Runs every frame while alive, refreshing the combo multiplier bar.
    IEnumerator TickBar(ScoreManager sm)
    {
        while (true)
        {
            if (sm.Combo > 0)
            {
                float pct    = Mathf.Clamp01(sm.ComboTimeLeft / sm.ComboWindow);
                int   filled = Mathf.RoundToInt(pct * barSegments);
                string bar   = new string('█', filled) + new string('░', barSegments - filled);
                multiText.text = $"x{sm.Combo}  {bar}";
                SetAlpha(multiText, 1f);
            }
            else
            {
                SetAlpha(multiText, 0f);
            }
            yield return null;
        }
    }

    IEnumerator PopupFade()
    {
        SetAlpha(hitText, 1f);
        yield return new WaitForSeconds(popupHold);
        for (float t = 0f; t < popupFade; t += Time.deltaTime)
        {
            SetAlpha(hitText, 1f - t / popupFade);
            yield return null;
        }
        SetAlpha(hitText, 0f);
        popupRoutine = null;
    }

    // ── canvas construction ────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("ScoreCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── SCORE (top-centre) ───────────────────────────────────────────
        totalText = MakeText(canvasGO,
            name:          "TotalScore",
            anchorMin:     new Vector2(0.5f, 1f),
            anchorMax:     new Vector2(0.5f, 1f),
            anchoredPos:   new Vector2(0f, -54f),
            size:          new Vector2(700f, 80f),
            text:          "SCORE  0",
            fontSize:      42f,
            style:         FontStyles.Bold,
            color:         Color.white);
        totalText.alignment = TextAlignmentOptions.Center;
        AddOutline(totalText);

        // ── COMBO MULTIPLIER BAR (just below score) ──────────────────────
        multiText = MakeText(canvasGO,
            name:          "ComboBar",
            anchorMin:     new Vector2(0.5f, 1f),
            anchorMax:     new Vector2(0.5f, 1f),
            anchoredPos:   new Vector2(0f, -110f),
            size:          new Vector2(600f, 50f),
            text:          "",
            fontSize:      26f,
            style:         FontStyles.Bold,
            color:         new Color(1f, 0.85f, 0.1f)); // gold
        multiText.alignment = TextAlignmentOptions.Center;
        SetAlpha(multiText, 0f);
        AddOutline(multiText);

        // ── HIT POP-UP (mid-screen) ──────────────────────────────────────
        hitText = MakeText(canvasGO,
            name:          "HitPopup",
            anchorMin:     new Vector2(0.5f, 0.5f),
            anchorMax:     new Vector2(0.5f, 0.5f),
            anchoredPos:   new Vector2(0f, 100f),
            size:          new Vector2(600f, 180f),
            text:          "",
            fontSize:      58f,
            style:         FontStyles.Bold,
            color:         Color.white);
        hitText.alignment = TextAlignmentOptions.Center;
        SetAlpha(hitText, 0f);
        AddOutline(hitText, thickness: 0.4f);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private void UpdateTotal(int score)
    {
        if (totalText != null)
            totalText.text = $"SCORE  {score:N0}";
    }

    private static TextMeshProUGUI MakeText(
        GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 size,
        string text, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt          = go.AddComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.pivot        = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta    = size;

        var tmp              = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = style;
        tmp.color            = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        return tmp;
    }

    private static void AddOutline(TextMeshProUGUI tmp, float thickness = 0.25f)
    {
        tmp.outlineWidth = thickness;
        tmp.outlineColor = new Color32(0, 0, 0, 200);
    }

    private static void SetAlpha(TextMeshProUGUI tmp, float a)
    {
        if (tmp == null) return;
        var c = tmp.color; c.a = a; tmp.color = c;
    }
}
