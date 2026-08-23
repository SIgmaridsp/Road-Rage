using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Auto-builds a styled score HUD using TextMeshPro.
///
/// REQUIRES: Window → TextMeshPro → Import TMP Essential Resources (one-time).
///
/// Layout (1920×1080 reference):
///   Top-left panel  →  SCORE label + large number
///   Top-right panel →  x3 COMBO + draining bar  (only when active)
///   Centre screen   →  +600  x3 COMBO!  pop-up flash
/// </summary>
public class ScoreHUD : MonoBehaviour
{
    [Header("Popup timing")]
    [SerializeField] private float popupHold = 1.2f;
    [SerializeField] private float popupFade = 0.35f;
    [Header("Combo bar")]
    [SerializeField] private int barSegments = 16;

    // runtime text refs
    private TextMeshProUGUI scoreNumberText;
    private TextMeshProUGUI comboHeaderText;
    private TextMeshProUGUI comboBarText;
    private TextMeshProUGUI hitPopupText;

    private Coroutine popupRoutine;

    // ── colours ──────────────────────────────────────────────────────────────
    static readonly Color PanelBg      = new Color(0f,   0f,   0f,   0.55f);
    static readonly Color AccentYellow = new Color(1f,   0.85f, 0.1f, 1f);
    static readonly Color AccentOrange = new Color(1f,   0.45f, 0.05f, 1f);
    static readonly Color White        = Color.white;

    // ── lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        BuildCanvas();
        StartCoroutine(Subscribe());
    }

    IEnumerator Subscribe()
    {
        yield return null;
        var sm = ScoreManager.Instance;
        if (sm == null) { Debug.LogWarning("ScoreHUD: no ScoreManager found."); yield break; }

        RefreshScore(sm.TotalScore);
        sm.OnHit      += (pts, combo) => HandleHit(pts, combo, sm);
        sm.OnComboEnd += HandleComboEnd;
        StartCoroutine(TickCombo(sm));
    }

    void OnDestroy()
    {
        var sm = ScoreManager.Instance;
        if (sm != null) sm.OnComboEnd -= HandleComboEnd;
    }

    // ── event handlers ────────────────────────────────────────────────────────

    void HandleHit(int pts, int combo, ScoreManager sm)
    {
        RefreshScore(sm.TotalScore);

        hitPopupText.text = combo > 1
            ? $"<color=#FFDD00>+{pts:N0}</color>\n<size=62%>✦ x{combo} COMBO ✦</size>"
            : $"<color=#FFDD00>+{pts:N0}</color>";

        if (popupRoutine != null) StopCoroutine(popupRoutine);
        popupRoutine = StartCoroutine(PopupFade());
    }

    void HandleComboEnd()
    {
        SetPanelActive(comboHeaderText.transform.parent.gameObject, false);
    }

    // ── coroutines ────────────────────────────────────────────────────────────

    IEnumerator TickCombo(ScoreManager sm)
    {
        var comboPanel = comboHeaderText.transform.parent.gameObject;
        while (true)
        {
            bool active = sm.Combo > 0;
            SetPanelActive(comboPanel, active);
            if (active)
            {
                float pct    = Mathf.Clamp01(sm.ComboTimeLeft / sm.ComboWindow);
                int   filled = Mathf.RoundToInt(pct * barSegments);
                string bar   = "<color=#FF7700>" + new string('█', filled) + "</color>"
                             + "<color=#444444>" + new string('█', barSegments - filled) + "</color>";
                comboHeaderText.text = $"x{sm.Combo}  COMBO";
                comboBarText.text    = bar;
            }
            yield return null;
        }
    }

    IEnumerator PopupFade()
    {
        SetAlpha(hitPopupText, 1f);
        // punch scale
        hitPopupText.transform.localScale = Vector3.one * 1.15f;
        float scaleT = 0f;
        while (scaleT < 0.12f)
        {
            scaleT += Time.deltaTime;
            hitPopupText.transform.localScale = Vector3.Lerp(
                Vector3.one * 1.15f, Vector3.one, scaleT / 0.12f);
            yield return null;
        }
        hitPopupText.transform.localScale = Vector3.one;

        yield return new WaitForSeconds(popupHold);

        for (float t = 0f; t < popupFade; t += Time.deltaTime)
        {
            SetAlpha(hitPopupText, 1f - t / popupFade);
            yield return null;
        }
        SetAlpha(hitPopupText, 0f);
        popupRoutine = null;
    }

    // ── canvas construction ───────────────────────────────────────────────────

    void BuildCanvas()
    {
        var root = new GameObject("ScoreCanvas");
        DontDestroyOnLoad(root);

        var canvas         = root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── SCORE PANEL (top-left) ────────────────────────────────────────────
        var scorePanelGO = MakePanel(root,
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
            pivot:     new Vector2(0f, 1f),
            anchPos:   new Vector2(20f, -20f),
            size:      new Vector2(310f, 110f));

        var scoreLabel = MakeTMP(scorePanelGO, "ScoreLabel",
            anchMin: Vector2.zero, anchMax: Vector2.one,
            pivot:   new Vector2(0f, 1f),
            anchPos: new Vector2(18f, -10f),
            size:    new Vector2(0f, 0f),
            text:    "SCORE", fontSize: 20f,
            style:   FontStyles.Bold | FontStyles.UpperCase,
            color:   new Color(0.6f, 0.6f, 0.6f, 1f));
        scoreLabel.alignment = TextAlignmentOptions.TopLeft;

        scoreNumberText = MakeTMP(scorePanelGO, "ScoreNumber",
            anchMin: Vector2.zero, anchMax: Vector2.one,
            pivot:   new Vector2(0f, 1f),
            anchPos: new Vector2(14f, -38f),
            size:    new Vector2(0f, 0f),
            text:    "0", fontSize: 54f,
            style:   FontStyles.Bold,
            color:   White);
        scoreNumberText.alignment         = TextAlignmentOptions.TopLeft;
        scoreNumberText.characterSpacing  = 2f;
        AddTMPOutline(scoreNumberText, new Color(0f, 0f, 0f, 0.5f), 0.15f);

        // ── COMBO PANEL (top-right) ───────────────────────────────────────────
        var comboPanel = MakePanel(root,
            anchorMin: new Vector2(1f, 1f), anchorMax: new Vector2(1f, 1f),
            pivot:     new Vector2(1f, 1f),
            anchPos:   new Vector2(-20f, -20f),
            size:      new Vector2(310f, 110f));
        comboPanel.SetActive(false); // hidden until first combo

        comboHeaderText = MakeTMP(comboPanel, "ComboHeader",
            anchMin: Vector2.zero, anchMax: Vector2.one,
            pivot:   new Vector2(1f, 1f),
            anchPos: new Vector2(-14f, -10f),
            size:    new Vector2(0f, 0f),
            text:    "x1  COMBO", fontSize: 26f,
            style:   FontStyles.Bold | FontStyles.UpperCase,
            color:   AccentYellow);
        comboHeaderText.alignment = TextAlignmentOptions.TopRight;
        AddTMPOutline(comboHeaderText, new Color(0.5f, 0.3f, 0f, 0.7f), 0.2f);

        comboBarText = MakeTMP(comboPanel, "ComboBar",
            anchMin: Vector2.zero, anchMax: Vector2.one,
            pivot:   new Vector2(1f, 1f),
            anchPos: new Vector2(-14f, -50f),
            size:    new Vector2(0f, 0f),
            text:    "", fontSize: 20f,
            style:   FontStyles.Normal,
            color:   AccentOrange);
        comboBarText.alignment = TextAlignmentOptions.TopRight;

        // ── HIT POP-UP (centre) ───────────────────────────────────────────────
        hitPopupText = MakeTMP(root, "HitPopup",
            anchMin: new Vector2(0.5f, 0.5f), anchMax: new Vector2(0.5f, 0.5f),
            pivot:   new Vector2(0.5f, 0.5f),
            anchPos: new Vector2(0f, 120f),
            size:    new Vector2(700f, 200f),
            text:    "", fontSize: 64f,
            style:   FontStyles.Bold,
            color:   White);
        hitPopupText.alignment = TextAlignmentOptions.Center;
        AddTMPOutline(hitPopupText, new Color(0f, 0f, 0f, 0.8f), 0.25f);
        SetAlpha(hitPopupText, 0f);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    void RefreshScore(int score) =>
        scoreNumberText.text = score.ToString("N0");

    static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active)
            panel.SetActive(active);
    }

    static GameObject MakePanel(GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchPos, Vector2 size)
    {
        var go = new GameObject("Panel");
        go.transform.SetParent(parent.transform, false);

        var rt         = go.AddComponent<RectTransform>();
        rt.anchorMin   = anchorMin;
        rt.anchorMax   = anchorMax;
        rt.pivot       = pivot;
        rt.anchoredPosition = anchPos;
        rt.sizeDelta   = size;

        var img        = go.AddComponent<Image>();
        img.color      = PanelBg;

        // Rounded look via a simple border radius trick: use sprite slicing
        // — without a sprite we just use the flat colour; good enough.
        return go;
    }

    static TextMeshProUGUI MakeTMP(
        GameObject parent, string name,
        Vector2 anchMin, Vector2 anchMax, Vector2 pivot,
        Vector2 anchPos, Vector2 size,
        string text, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rt               = go.AddComponent<RectTransform>();
        rt.anchorMin         = anchMin;
        rt.anchorMax         = anchMax;
        rt.pivot             = pivot;
        rt.anchoredPosition  = anchPos;
        rt.sizeDelta         = size;

        var tmp              = go.AddComponent<TextMeshProUGUI>();
        tmp.text             = text;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = style;
        tmp.color            = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode     = TextOverflowModes.Overflow;
        tmp.richText         = true;
        return tmp;
    }

    static void AddTMPOutline(TextMeshProUGUI tmp, Color color, float width)
    {
        tmp.outlineColor = color;
        tmp.outlineWidth = width;
    }

    static void SetAlpha(TextMeshProUGUI tmp, float a)
    {
        if (tmp == null) return;
        var c = tmp.color; c.a = a; tmp.color = c;
    }
}
