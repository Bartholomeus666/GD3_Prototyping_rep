using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the spell wheel: shows/hides it, drives the DonutGraphic,
/// and places labels over each slice.
///
/// Setup:
///   1. Canvas (Screen Space – Overlay)
///   2. WheelRoot (empty child, anchored center) — attach this script
///   3. Create a child GameObject "Donut", attach SpellWheelDonutGraphic
///      and assign it to donutGraphic below
/// </summary>
public class SpellWheelRenderer : MonoBehaviour
{
    [Header("References")]
    public SpellWheelDonutGraphic donutGraphic;

    [Header("Labels")]
    public Font  labelFont;
    public int   labelFontSize   = 18;
    public Color labelColor      = Color.white;

    private SpellWheelConfig  _config;
    private readonly List<Text> _labels = new();

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show(SpellWheelConfig config)
    {
        _config = config;
        donutGraphic.config = config;
        gameObject.SetActive(true);
        Rebuild();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        donutGraphic.highlightedIndex = -1;
    }

    public void UpdateHighlight(Vector2 drag)
    {
        int idx = drag.magnitude < 12f
                  ? -1
                  : _config.GetSliceIndexAtAngle(DragToAngle(drag));

        if (idx == donutGraphic.highlightedIndex) return;
        donutGraphic.highlightedIndex = idx;
        donutGraphic.Refresh();
        UpdateLabelColors(idx);
    }

    public void Rebuild()
    {
        ClearLabels();
        donutGraphic.Refresh();
        BuildLabels();
    }

    // ── Private ───────────────────────────────────────────────────────────────

    void BuildLabels()
    {
        if (_config == null || labelFont == null) return;
        float[] starts = _config.GetStartAngles();
        float outerR   = donutGraphic.outerRadius;
        float innerR   = donutGraphic.innerRadius;

        for (int i = 0; i < _config.slots.Count; i++)
        {
            float sweepDeg = _config.slots[i].percent * 360f;
            float midDeg   = starts[i] + sweepDeg * 0.5f;
            float midRad   = (90f - midDeg) * Mathf.Deg2Rad;
            float labelR   = (outerR + innerR) * 0.52f;

            var lgo = new GameObject($"Label_{i}", typeof(RectTransform), typeof(Text));
            lgo.transform.SetParent(transform, false);
            var rt = lgo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * 0.5f;
            rt.sizeDelta = new Vector2(110f, 52f);
            rt.anchoredPosition = new Vector2(
                labelR * Mathf.Cos(midRad),
                labelR * Mathf.Sin(midRad));

            var t = lgo.GetComponent<Text>();
            t.font      = labelFont;
            t.fontSize  = labelFontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color     = labelColor;
            t.text      = _config.slots[i].spell.spellName;
            _labels.Add(t);
        }
    }

    void UpdateLabelColors(int highlighted)
    {
        for (int i = 0; i < _labels.Count; i++)
            _labels[i].color = i == highlighted ? Color.white : new Color(1, 1, 1, 0.65f);
    }

    void ClearLabels()
    {
        foreach (var l in _labels) if (l) Destroy(l.gameObject);
        _labels.Clear();
    }

    static float DragToAngle(Vector2 drag)
    {
        float deg = Mathf.Atan2(drag.y, drag.x) * Mathf.Rad2Deg;
        return ((90f - deg) % 360f + 360f) % 360f;
    }
}
