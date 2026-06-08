using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class GestureRenderer : Graphic
{
    [Header("Line appearance")]
    [Tooltip("Thickness of the gesture line in pixels.")]
    public float lineWidth = 6f;

    [Tooltip("Colour of the line while drawing.")]
    public Color drawColor = new Color(0.4f, 0.8f, 1f, 0.9f);

    [Tooltip("How quickly the line fades out after casting. 0 = no fade.")]
    public float fadeDuration = 0.4f;

    [Tooltip("Optional gradient along the line length (overrides drawColor if enabled).")]
    public bool useGradient = true;
    public Gradient gradient;

    private List<Vector2> _points    = new();
    private bool          _fading    = false;
    private float         _fadeTimer = 0f;
    private float         _alpha     = 1f;

    protected override void Awake()
    {
        base.Awake();

        // Default gradient: cyan → magenta
        if (gradient == null || gradient.colorKeys.Length == 0)
        {
            gradient = new Gradient();
            gradient.SetKeys(
                new[] {
                    new GradientColorKey(new Color(0.3f, 0.9f, 1f), 0f),
                    new GradientColorKey(new Color(0.8f, 0.3f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (_points.Count < 2) return;

        // Convert screen-space points to local RectTransform space
        var localPts = new List<Vector2>(_points.Count);
        foreach (var p in _points)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform, p, null, out Vector2 local);
            localPts.Add(local);
        }

        // Build a quad strip along the polyline
        for (int i = 0; i < localPts.Count - 1; i++)
        {
            Vector2 a = localPts[i];
            Vector2 b = localPts[i + 1];
            Vector2 dir = (b - a).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);

            float t0 = (float)i       / (localPts.Count - 1);
            float t1 = (float)(i + 1) / (localPts.Count - 1);

            Color c0 = useGradient ? gradient.Evaluate(t0) : drawColor;
            Color c1 = useGradient ? gradient.Evaluate(t1) : drawColor;
            c0.a *= _alpha;
            c1.a *= _alpha;

            int baseIdx = vh.currentVertCount;

            // 4 vertices per segment (a quad)
            vh.AddVert(a - perp, c0, Vector2.zero);  // 0 bottom-left
            vh.AddVert(a + perp, c0, Vector2.zero);  // 1 top-left
            vh.AddVert(b + perp, c1, Vector2.zero);  // 2 top-right
            vh.AddVert(b - perp, c1, Vector2.zero);  // 3 bottom-right

            vh.AddTriangle(baseIdx,     baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx,     baseIdx + 2, baseIdx + 3);
        }

        // Round caps: draw a filled circle at each end
        DrawCap(vh, localPts[0],                    localPts.Count > 1 ? (localPts[1] - localPts[0]).normalized : Vector2.up,
                useGradient ? gradient.Evaluate(0f) : drawColor);
        DrawCap(vh, localPts[localPts.Count - 1],   (localPts[localPts.Count - 1] - localPts[localPts.Count - 2]).normalized,
                useGradient ? gradient.Evaluate(1f) : drawColor);
    }

    private void DrawCap(VertexHelper vh, Vector2 center, Vector2 dir, Color col)
    {
        const int SEGMENTS = 8;
        float r = lineWidth * 0.5f;
        Color c = col;
        c.a *= _alpha;

        int baseIdx = vh.currentVertCount;
        vh.AddVert(center, c, Vector2.zero);

        for (int s = 0; s <= SEGMENTS; s++)
        {
            float angle = s * Mathf.PI / SEGMENTS;
            // Rotate around the endpoint outward
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            Vector2 offset = (-dir * cos + perp * sin) * r;
            vh.AddVert(center + offset, c, Vector2.zero);
        }

        for (int s = 0; s < SEGMENTS; s++)
            vh.AddTriangle(baseIdx, baseIdx + s + 1, baseIdx + s + 2);
    }

    private void Update()
    {
        if (!_fading) return;

        _fadeTimer += Time.deltaTime;
        _alpha      = Mathf.Clamp01(1f - _fadeTimer / fadeDuration);
        SetVerticesDirty();

        if (_fadeTimer >= fadeDuration)
        {
            _fading = false;
            _points.Clear();
            SetVerticesDirty();
        }
    }

    public void StartGesture()
    {
        _points.Clear();
        _fading    = false;
        _fadeTimer = 0f;
        _alpha     = 1f;
        SetVerticesDirty();
    }

    /// <summary>Add a screen-space point to the current gesture line.</summary>
    public void AddPoint(Vector2 screenPos)
    {
        // Skip duplicate or nearly identical points to keep the mesh clean
        if (_points.Count > 0 && Vector2.Distance(_points[_points.Count - 1], screenPos) < 2f)
            return;

        _points.Add(screenPos);
        SetVerticesDirty();
    }

    /// <summary>Stop drawing and trigger the fade-out (or clear immediately if fadeDuration is 0).</summary>
    public void EndGesture()
    {
        if (fadeDuration > 0f)
            _fading = true;
        else
        {
            _points.Clear();
            SetVerticesDirty();
        }
    }
}
