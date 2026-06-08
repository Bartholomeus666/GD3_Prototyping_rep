using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single UI Graphic that draws all spell wheel slices as one donut mesh.
/// Fully transparent center — no shader tricks, no stencil, no coverage.
/// Just geometry: triangles only exist where the ring is, not in the hole.
///
/// Attach to an empty GameObject under your Screen Space Overlay canvas.
/// The GameObject should be anchored to center.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class SpellWheelDonutGraphic : MaskableGraphic
{
    [Header("Dimensions (px)")]
    public float outerRadius = 160f;
    public float innerRadius = 58f;

    [Header("Visuals")]
    [Range(0f, 1f)] public float sliceAlpha = 0.90f;
    [Tooltip("Gap between slices in degrees.")]
    public float gapDeg = 3f;
    [Tooltip("Smoothness — segments per slice.")]
    public int segmentsPerSlice = 32;

    [Header("Highlight")]
    public float highlightAlphaBoost = 0.10f;
    public float highlightScaleBoost = 1.04f;

    // Set by SpellWheelRenderer
    [HideInInspector] public SpellWheelConfig config;
    [HideInInspector] public int highlightedIndex = -1;

    // Override color property — we drive color per-vertex so disable the base tint
    public override Color color { get => Color.white; set {} }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (config == null || config.slots.Count == 0) return;

        float[] starts = config.GetStartAngles(); // degrees, 0=top, CW

        for (int i = 0; i < config.slots.Count; i++)
        {
            var   slot     = config.slots[i];
            float sweepDeg = slot.percent * 360f;
            float drawDeg  = Mathf.Max(0f, sweepDeg - gapDeg);
            if (drawDeg <= 0f) continue;

            float startDeg = starts[i];
            float alpha    = i == highlightedIndex
                             ? Mathf.Min(1f, sliceAlpha + highlightAlphaBoost)
                             : sliceAlpha;
            float scale    = i == highlightedIndex ? highlightScaleBoost : 1f;
            Color c        = new Color(slot.spell.wheelColor.r,
                                       slot.spell.wheelColor.g,
                                       slot.spell.wheelColor.b,
                                       alpha);

            float oR = outerRadius * scale;
            float iR = innerRadius; // don't scale the hole

            int baseVert = vh.currentVertCount;
            int segs     = Mathf.Max(3, segmentsPerSlice);

            for (int s = 0; s <= segs; s++)
            {
                float t      = (float)s / segs;
                // CW from top → standard math angle: subtract from 90°
                float angDeg = startDeg + drawDeg * t;
                float angRad = (90f - angDeg) * Mathf.Deg2Rad;
                float cos    = Mathf.Cos(angRad);
                float sin    = Mathf.Sin(angRad);

                // Outer vertex
                vh.AddVert(new Vector3(cos * oR, sin * oR, 0f), c,
                           new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
                // Inner vertex
                vh.AddVert(new Vector3(cos * iR, sin * iR, 0f), c,
                           new Vector2(cos * 0.5f * (iR/oR) + 0.5f,
                                       sin * 0.5f * (iR/oR) + 0.5f));
            }

            // Triangulate the strip
            for (int s = 0; s < segs; s++)
            {
                int b = baseVert + s * 2;
                // Two triangles per quad: outer-outer-inner / inner-outer-inner
                vh.AddTriangle(b,     b + 2, b + 3);
                vh.AddTriangle(b,     b + 3, b + 1);
            }
        }
    }

    // Called by SpellWheelRenderer after changing highlight or config
    public void Refresh() => SetVerticesDirty();
}
