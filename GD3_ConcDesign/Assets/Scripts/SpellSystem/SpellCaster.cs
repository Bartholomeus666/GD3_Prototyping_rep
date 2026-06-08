using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class SpellCaster : MonoBehaviour
{


    [Header("Patterns to recognise")]
    [Tooltip("Assign one or more SpellPatternData assets here.")]
    public List<SpellPatternData> spellPatterns = new();

    [Header("Visuals")]
    [Tooltip("Optional GestureRenderer that draws the line on the Canvas while casting.")]
    public GestureRenderer gestureRenderer;

    [Header("Matching")]
    [Tooltip("Minimum score (0–1) required for a successful match. " +
             "1 = perfect, 0 = no similarity. Try 0.7 as a starting point.")]
    [Range(0f, 1f)]
    public float matchThreshold = 0.7f;

    [Tooltip("Minimum number of pointer samples required before attempting a match.")]
    public int minSamples = 8;

    public Transform castOrigin;

    public System.Action<SpellPatternData> OnSpellCast;
    public System.Action                   OnCastFailed;

    private const int   NUM_POINTS       = 64;
    private const float SQUARE_SIZE      = 250f;   // reference square for scale normalisation
    private const float DIAGONAL         = 353.55f; // Mathf.Sqrt(2) * SQUARE_SIZE
    private const float HALF_DIAGONAL    = 176.78f;
    private const float ANGLE_RANGE      = 45f;    // degrees to search either side
    private const float ANGLE_PRECISION  = 2f;
    private const float PHI              = 1.61803f; // golden ratio


    private bool          _recording;
    private Vector2       _lastPointerPos;
    private List<Vector2> _recordedPoints = new();



    public void OnCast(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            _recording = true;
            _recordedPoints.Clear();
            _recordedPoints.Add(_lastPointerPos);
            gestureRenderer?.StartGesture();
            Debug.Log("[SpellCaster] Recording gesture…");
        }

        if (ctx.canceled)
        {
            _recording = false;
            gestureRenderer?.EndGesture();
            StopAndMatch();
        }
    }

    public void OnPointerMoved(InputAction.CallbackContext ctx)
    {
        _lastPointerPos = ctx.ReadValue<Vector2>();
        if (_recording)
        {
            _recordedPoints.Add(_lastPointerPos);
            gestureRenderer?.AddPoint(_lastPointerPos);
        }
    }

    private void StopAndMatch()
    {
        if (_recordedPoints.Count < minSamples)
        {
            Debug.Log("[SpellCaster] Too few samples – ignoring gesture.");
            return;
        }

        // Preprocess the input once, then compare against all patterns
        List<Vector2> input = Preprocess(_recordedPoints);

        SpellPatternData best      = null;
        float            bestScore = -1f;

        foreach (var pattern in spellPatterns)
        {
            if (pattern == null || pattern.Count < 2) continue;

            List<Vector2> template = Preprocess(new List<Vector2>(pattern.points));
            float score = Recognize(input, template);

            Debug.Log($"[SpellCaster] vs {pattern.name}: score={score:F3}");

            if (score > bestScore)
            {
                bestScore = score;
                best      = pattern;
            }
        }

        if (best != null && bestScore >= matchThreshold)
        {
            Debug.Log($"[SpellCaster] Cast: {best.name}  (score={bestScore:F3})");
            OnSpellCast?.Invoke(best);
            Instantiate(best.spellPrefab, castOrigin.position, castOrigin.rotation);
        }
        else
        {
            Debug.Log($"[SpellCaster] No match (best score={bestScore:F3}).");
            OnCastFailed?.Invoke();
        }
    }


    private List<Vector2> Preprocess(List<Vector2> pts)
    {
        pts = Resample(pts, NUM_POINTS);
        pts = ScaleToSquare(pts, SQUARE_SIZE);
        pts = TranslateToOrigin(pts);
        return pts;
    }

    private float Recognize(List<Vector2> input, List<Vector2> template)
    {
        float worstCase = Mathf.Sqrt(2f) * SQUARE_SIZE;

        // Try the gesture as-is, then mirrored horizontally and vertically.
        // This handles cases where the player starts from a different corner
        // without giving up directional sensitivity within each variant.
        float d1 = PathDistance(input, template);
        float d2 = PathDistance(FlipH(input), template);
        float d3 = PathDistance(FlipV(input), template);
        float d4 = PathDistance(FlipH(FlipV(input)), template);

        float best = Mathf.Min(d1, Mathf.Min(d2, Mathf.Min(d3, d4)));
        return 1f - best / worstCase;
    }

    private List<Vector2> FlipH(List<Vector2> pts)
    {
        var result = new List<Vector2>(pts.Count);
        foreach (var p in pts) result.Add(new Vector2(-p.x, p.y));
        return result;
    }

    private List<Vector2> FlipV(List<Vector2> pts)
    {
        var result = new List<Vector2>(pts.Count);
        foreach (var p in pts) result.Add(new Vector2(p.x, -p.y));
        return result;
    }


    private float GoldenSectionSearch(List<Vector2> pts, List<Vector2> tmpl,
                                       float a, float b, float threshold)
    {
        float x1 = PHI * a + (2f - PHI) * b;
        float x2 = (2f - PHI) * a + PHI * b;
        float f1 = PathDistance(Rotate(pts, x1), tmpl);
        float f2 = PathDistance(Rotate(pts, x2), tmpl);

        int iterations = 0;
        while (Mathf.Abs(b - a) > threshold && iterations < 100)
        {
            iterations++;
            if (f1 < f2)
            {
                b  = x2;
                x2 = x1; f2 = f1;
                x1 = PHI * a + (2f - PHI) * b;
                f1 = PathDistance(Rotate(pts, x1), tmpl);
            }
            else
            {
                a  = x1;
                x1 = x2; f1 = f2;
                x2 = (2f - PHI) * a + PHI * b;
                f2 = PathDistance(Rotate(pts, x2), tmpl);
            }
        }

        return Mathf.Min(f1, f2);
    }


    private List<Vector2> Resample(List<Vector2> pts, int n)
    {
        float length = PathLength(pts);
        if (length < 0.001f)
        {
            var flat = new List<Vector2>(n);
            for (int i = 0; i < n; i++) flat.Add(pts[0]);
            return flat;
        }

        float interval    = length / (n - 1);
        var   result      = new List<Vector2>(n) { pts[0] };
        float accumulated = 0f;
        Vector2 prev      = pts[0];

        for (int i = 1; i < pts.Count; i++)
        {
            Vector2 curr = pts[i];
            float d = Vector2.Distance(prev, curr);

            while (accumulated + d >= interval && result.Count < n)
            {
                float t = (interval - accumulated) / d;
                Vector2 q = Vector2.Lerp(prev, curr, t);
                result.Add(q);
                // Start the next sub-segment from q
                d          -= interval - accumulated;
                accumulated = 0f;
                prev        = q;
            }

            accumulated += d;
            prev         = curr;
        }

        while (result.Count < n)
            result.Add(pts[pts.Count - 1]);

        return result;
    }

    /// <summary>Rotate the gesture so its indicative angle (centroid → first point) is 0°.</summary>
    private List<Vector2> RotateToZero(List<Vector2> pts)
    {
        Vector2 c     = Centroid(pts);
        float   angle = Mathf.Atan2(pts[0].y - c.y, pts[0].x - c.x);
        return Rotate(pts, -angle);
    }

    private List<Vector2> Rotate(List<Vector2> pts, float radians)
    {
        Vector2 c   = Centroid(pts);
        float   cos = Mathf.Cos(radians);
        float   sin = Mathf.Sin(radians);
        var     result = new List<Vector2>(pts.Count);
        foreach (var p in pts)
        {
            float dx = p.x - c.x, dy = p.y - c.y;
            result.Add(new Vector2(dx * cos - dy * sin + c.x,
                                   dx * sin + dy * cos + c.y));
        }
        return result;
    }

    private List<Vector2> ScaleToSquare(List<Vector2> pts, float size)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in pts)
        {
            if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y; if (p.y > maxY) maxY = p.y;
        }
        // Uniform scale so the longer axis fills the square
        float scale = size / Mathf.Max(maxX - minX, maxY - minY, 0.0001f);
        var result = new List<Vector2>(pts.Count);
        foreach (var p in pts)
            result.Add(new Vector2(p.x * scale, p.y * scale));
        return result;
    }

    private List<Vector2> TranslateToOrigin(List<Vector2> pts)
    {
        Vector2 c      = Centroid(pts);
        var     result = new List<Vector2>(pts.Count);
        foreach (var p in pts)
            result.Add(p - c);
        return result;
    }

    private Vector2 Centroid(List<Vector2> pts)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in pts) sum += p;
        return sum / pts.Count;
    }

    private float PathDistance(List<Vector2> a, List<Vector2> b)
    {
        float d = 0f;
        for (int i = 0; i < Mathf.Min(a.Count, b.Count); i++)
            d += Vector2.Distance(a[i], b[i]);
        return d / a.Count;
    }

    private float PathLength(List<Vector2> pts)
    {
        float len = 0f;
        for (int i = 1; i < pts.Count; i++)
            len += Vector2.Distance(pts[i - 1], pts[i]);
        return Mathf.Max(len, 0.0001f);
    }
}
