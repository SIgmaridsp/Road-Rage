using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-builds the score HUD at runtime using standard Unity UI Text —
/// no TextMeshPro / TMP import required.
///
/// Just add this component (and ScoreManager) to any GameObject in the scene.
///
/// Layout (Screen Space – Overlay, 1920×1080 reference):
///   Top-centre  →  SCORE  12,345
///   Below score →  x3  ████████░░░░   (gold combo-timer bar)
///   Mid-screen  →  +600  x3 COMBO!   (pop-up, fades out)
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    [Header("Popup timing")]
    [SerializeField] private float popupHold = 1.4f;
    [SerializeField] private float popupFade = 0.4f;
    [Header("Combo bar")]
    [SerializeField] private int barSegments = 18;

    private Text scoreText;
    private Text comboText;
    private Text hitText;
    private Coroutine popupRoutine;

    void Start()
    {
        BuildCanvas();
        StartCoroutine(Subscribe());
    }

    IEnumerator Subscribe()
    {
        yield return null; // wait one frame for ScoreManager.Awake

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
        if (sm != null) sm.OnComboEnd -= HandleComboEnd;
    }

    // ── event handlers ──────────────────────────────────────────────────────

    private void HandleHit(int pts, int combo, ScoreManager sm)
    {
        UpdateTotal(sm.TotalScore);

        hitText.text = combo > 1
            ? $"+{pts:N0}  x{combo} COMBO!"
            : $"+{pts:N0}";

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupFade());
    }

    private void HandleComboEnd() => SetAlpha(comboText, 0f);

    // ── coroutines ──────────────────────────────────────────────────────────

    IEnumerator TickBar(ScoreManager sm)
    {
        while (true)
        {
            if (sm.Combo > 0)
            {
                float pct    = Mathf.Clamp01(sm.ComboTimeLeft / sm.ComboWindow);
                int   filled = Mathf.RoundToInt(pct * barSegments);
                string bar   = new string('\u2588', filled)          // █
                             + new string('\u2591', barSegments - filled); // ░
                comboText.text = $"x{sm.Combo}  {bar}";
                SetAlpha(comboText, 1f);
            }
            else
            {
                SetAlpha(comboText, 0f);
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

    // ── canvas construction ─────────────────────────────────────────────────

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("ScoreCanvas");
        DontDestroyOnLoad(canvasGO);

        var canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler                    = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // SCORE — top-centre
        scoreText = MakeText(canvasGO, "TotalScore",
            anchorMin:   new Vector2(0.5f, 1f),
            anchorMax:   new Vector2(0.5f, 1f),
            anchPos:     new Vector2(0f, -54f),
            size:        new Vector2(700f, 70f),
            text:        "SCORE  0",
            fontSize:    40,
            bold:        true,
            color:       Color.white);
        scoreText.alignment = TextAnchor.MiddleCenter;
        AddOutline(scoreText);

        // COMBO BAR — below score, gold
        comboText = MakeText(canvasGO, "ComboBar",
            anchorMin:   new Vector2(0.5f, 1f),
            anchorMax:   new Vector2(0.5f, 1f),
            anchPos:     new Vector2(0f, -112f),
            size:        new Vector2(700f, 50f),
            text:        "",
            fontSize:    26,
            bold:        true,
            color:       new Color(1f, 0.85f, 0.1f));
        comboText.alignment = TextAnchor.MiddleCenter;
        SetAlpha(comboText, 0f);
        AddOutline(comboText);

        // HIT POP-UP — mid-screen
        hitText = MakeText(canvasGO, "HitPopup",
            anchorMin:   new Vector2(0.5f, 0.5f),
            anchorMax:   new Vector2(0.5f, 0.5f),
            anchPos:     new Vector2(0f, 100f),
            size:        new Vector2(700f, 120f),
            text:        "",
            fontSize:    52,
            bold:        true,
            color:       Color.white);
        hitText.alignment = TextAnchor.MiddleCenter;
        SetAlpha(hitText, 0f);
        AddOutline(hitText, width: 2);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private void UpdateTotal(int score)
    {
        if (scoreText != null) scoreText.text = $"SCORE  {score:N0}";
    }

    private static Text MakeText(
        GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchPos, Vector2 size,
        string text, int fontSize, bool bold, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt             = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchPos;
        rt.sizeDelta       = size;

        var t              = go.AddComponent<Text>();
        t.text             = text;
        t.fontSize         = fontSize;
        t.fontStyle        = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color            = color;
        t.supportRichText  = false;
        t.resizeTextForBestFit = false;
        t.font             = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return t;
    }

    private static void AddOutline(Text t, int width = 1)
    {
        var o       = t.gameObject.AddComponent<Outline>();
        o.effectColor    = new Color(0, 0, 0, 0.8f);
        o.effectDistance = new Vector2(width, -width);
    }

    private static void SetAlpha(Text t, float a)
    {
        if (t == null) return;
        var c = t.color; c.a = a; t.color = c;
    }
}
