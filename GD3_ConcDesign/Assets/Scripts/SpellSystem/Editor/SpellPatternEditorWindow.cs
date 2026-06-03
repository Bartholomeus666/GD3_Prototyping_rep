// Place this file anywhere inside an  Editor  folder, e.g.:
//   Assets/SpellSystem/Editor/SpellPatternEditorWindow.cs

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor window for visually editing <see cref="SpellPatternData"/> assets.
///
/// Open via:  Window > Spell System > Pattern Editor
/// </summary>
public class SpellPatternEditorWindow : EditorWindow
{
    // ------------------------------------------------------------------
    // Constants / layout
    // ------------------------------------------------------------------
    private const float CANVAS_PADDING   = 16f;
    private const float POINT_RADIUS     = 8f;
    private const float HIT_RADIUS       = 12f;   // larger hit area for ease of click
    private const float TOOLBAR_HEIGHT   = 28f;
    private const float SIDE_PANEL_WIDTH = 220f;

    private static readonly Color COLOR_BG          = new Color(0.15f, 0.15f, 0.18f);
    private static readonly Color COLOR_GRID        = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color COLOR_LINE        = new Color(0.35f, 0.75f, 1f, 0.9f);
    private static readonly Color COLOR_POINT       = new Color(0.35f, 0.75f, 1f);
    private static readonly Color COLOR_POINT_SEL   = new Color(1f, 0.85f, 0.25f);
    private static readonly Color COLOR_POINT_FIRST = new Color(0.4f, 1f, 0.5f);
    private static readonly Color COLOR_POINT_LAST  = new Color(1f, 0.4f, 0.4f);
    private static readonly Color COLOR_INDEX_TEXT  = Color.white;

    // ------------------------------------------------------------------
    // State
    // ------------------------------------------------------------------
    private SpellPatternData _target;
    private int              _selectedIndex = -1;
    private bool             _isDragging    = false;
    private Vector2          _scrollPos;

    // Cached canvas rect (recalculated each repaint)
    private Rect _canvasRect;

    // ------------------------------------------------------------------
    // Menu entry
    // ------------------------------------------------------------------
    [MenuItem("Window/Spell System/Pattern Editor")]
    public static void OpenWindow()
    {
        var window = GetWindow<SpellPatternEditorWindow>("Spell Pattern Editor");
        window.minSize = new Vector2(600, 460);
        window.Show();
    }

    // Convenience: open with a specific asset already loaded
    public static void OpenWithAsset(SpellPatternData data)
    {
        OpenWindow();
        var window = GetWindow<SpellPatternEditorWindow>();
        window._target = data;
        window._selectedIndex = -1;
    }

    // ------------------------------------------------------------------
    // GUI entry point
    // ------------------------------------------------------------------
    private void OnGUI()
    {
        DrawToolbar();

        if (_target == null)
        {
            DrawEmptyState();
            return;
        }

        // Main layout: canvas on the left, side panel on the right
        Rect bodyRect = new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT);

        _canvasRect = new Rect(
            bodyRect.x,
            bodyRect.y,
            bodyRect.width - SIDE_PANEL_WIDTH,
            bodyRect.height);

        Rect sidePanelRect = new Rect(
            bodyRect.x + bodyRect.width - SIDE_PANEL_WIDTH,
            bodyRect.y,
            SIDE_PANEL_WIDTH,
            bodyRect.height);

        DrawCanvas(_canvasRect);
        DrawSidePanel(sidePanelRect);

        HandleCanvasEvents(_canvasRect);
    }

    // ------------------------------------------------------------------
    // Toolbar
    // ------------------------------------------------------------------
    private void DrawToolbar()
    {
        Rect toolbarRect = new Rect(0, 0, position.width, TOOLBAR_HEIGHT);
        EditorGUI.DrawRect(toolbarRect, new Color(0.2f, 0.2f, 0.23f));

        GUILayout.BeginArea(new Rect(4, 2, position.width - 8, TOOLBAR_HEIGHT - 4));
        GUILayout.BeginHorizontal();

        GUILayout.Label("Asset:", GUILayout.Width(42));
        var newTarget = (SpellPatternData)EditorGUILayout.ObjectField(
            _target, typeof(SpellPatternData), false, GUILayout.Width(200));

        if (newTarget != _target)
        {
            _target        = newTarget;
            _selectedIndex = -1;
        }

        if (GUILayout.Button("New Asset", GUILayout.Width(80)))
            CreateNewAsset();

        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("💾  Save", GUILayout.Width(80)))
            SaveAsset();
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ------------------------------------------------------------------
    // Empty state
    // ------------------------------------------------------------------
    private void DrawEmptyState()
    {
        var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(
            new Rect(0, TOOLBAR_HEIGHT, position.width, position.height - TOOLBAR_HEIGHT),
            "Select or create a Spell Pattern asset to begin editing.",
            style);
    }

    // ------------------------------------------------------------------
    // Canvas drawing
    // ------------------------------------------------------------------
    private void DrawCanvas(Rect rect)
    {
        // Background
        EditorGUI.DrawRect(rect, COLOR_BG);

        // Grid
        DrawGrid(rect, 40f);

        var pts = _target.points;
        if (pts.Count == 0)
        {
            var hint = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(rect, "Click 'Add Point' or click inside the canvas to place points.", hint);
            return;
        }

        // Lines between consecutive points
        Handles.BeginGUI();
        Handles.color = COLOR_LINE;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a = NormToCanvas(pts[i],     rect);
            Vector2 b = NormToCanvas(pts[i + 1], rect);
            Handles.DrawAAPolyLine(3f, a, b);
        }

        // Arrowhead on each segment midpoint to show direction
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a   = NormToCanvas(pts[i],     rect);
            Vector2 b   = NormToCanvas(pts[i + 1], rect);
            DrawArrow(a, b);
        }
        Handles.EndGUI();

        // Points
        for (int i = 0; i < pts.Count; i++)
        {
            Vector2 screen = NormToCanvas(pts[i], rect);
            bool    isSel  = (i == _selectedIndex);

            Color fill = isSel          ? COLOR_POINT_SEL  :
                         i == 0         ? COLOR_POINT_FIRST :
                         i == pts.Count - 1 ? COLOR_POINT_LAST :
                                          COLOR_POINT;

            DrawPoint(screen, fill, isSel ? POINT_RADIUS + 3 : POINT_RADIUS);

            // Index label
            var labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 9,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = COLOR_INDEX_TEXT }
            };
            GUI.Label(new Rect(screen.x - 10, screen.y - POINT_RADIUS - 16, 20, 14),
                      i.ToString(), labelStyle);
        }

        // Legend
        DrawLegend(rect);
    }

    private void DrawGrid(Rect rect, float spacing)
    {
        Handles.BeginGUI();
        Handles.color = COLOR_GRID;

        for (float x = rect.x + spacing; x < rect.xMax; x += spacing)
            Handles.DrawLine(new Vector2(x, rect.y), new Vector2(x, rect.yMax));
        for (float y = rect.y + spacing; y < rect.yMax; y += spacing)
            Handles.DrawLine(new Vector2(rect.x, y), new Vector2(rect.xMax, y));

        Handles.EndGUI();
    }

    private void DrawPoint(Vector2 pos, Color color, float radius)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawSolidDisc(pos, Vector3.forward, radius);
        Handles.color = Color.white;
        Handles.DrawWireDisc(pos, Vector3.forward, radius);
        Handles.EndGUI();
    }

    private void DrawArrow(Vector2 from, Vector2 to)
    {
        Vector2 mid = (from + to) * 0.5f;
        Vector2 dir = (to - from).normalized;
        if (dir == Vector2.zero) return;

        float   size  = 7f;
        Vector2 right = new Vector2(-dir.y, dir.x);

        Vector2 tip  = mid + dir  * size;
        Vector2 left = mid - dir  * size * 0.5f + right * size * 0.5f;
        Vector2 rght = mid - dir  * size * 0.5f - right * size * 0.5f;

        Handles.color = COLOR_LINE;
        Handles.DrawAAConvexPolygon(tip, left, rght);
    }

    private void DrawLegend(Rect canvasRect)
    {
        float px = canvasRect.x + 8;
        float py = canvasRect.yMax - 56;

        var s = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 1f, 1f, 0.5f) }
        };

        GUI.Label(new Rect(px, py,      200, 14), "● First point",        s);
        GUI.Label(new Rect(px, py + 14, 200, 14), "● Last point",         s);
        GUI.Label(new Rect(px, py + 28, 200, 14), "● Selected point",     s);
        GUI.Label(new Rect(px, py + 42, 200, 14), "Click canvas to add / drag to move", s);

        // Colour dots
        DrawTinyDot(new Vector2(px - 2, py + 7),      COLOR_POINT_FIRST);
        DrawTinyDot(new Vector2(px - 2, py + 21),     COLOR_POINT_LAST);
        DrawTinyDot(new Vector2(px - 2, py + 35),     COLOR_POINT_SEL);
    }

    private void DrawTinyDot(Vector2 pos, Color color)
    {
        Handles.BeginGUI();
        Handles.color = color;
        Handles.DrawSolidDisc(pos, Vector3.forward, 4f);
        Handles.EndGUI();
    }

    // ------------------------------------------------------------------
    // Side panel
    // ------------------------------------------------------------------
    private void DrawSidePanel(Rect rect)
    {
        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.21f));

        // Thin separator line
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), new Color(0.3f, 0.3f, 0.35f));

        GUILayout.BeginArea(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16));

        // ---- Title ----
        GUILayout.Label(_target.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ---- Add / Delete ----
        GUI.backgroundColor = new Color(0.3f, 0.65f, 1f);
        if (GUILayout.Button("＋  Add Point", GUILayout.Height(28)))
            AddPointCenter();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(4);

        bool hasSelection = _selectedIndex >= 0 && _selectedIndex < _target.points.Count;

        GUI.enabled = hasSelection;
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("✕  Delete Selected", GUILayout.Height(28)))
            DeleteSelected();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Clear All", GUILayout.Height(22)))
        {
            if (EditorUtility.DisplayDialog("Clear All Points",
                    "Remove all points from this pattern?", "Clear", "Cancel"))
            {
                Undo.RecordObject(_target, "Clear Spell Pattern");
                _target.points.Clear();
                _selectedIndex = -1;
                EditorUtility.SetDirty(_target);
            }
        }

        EditorGUILayout.Space(10);

        // ---- Selected point numeric editor ----
        if (hasSelection)
        {
            GUILayout.Label($"Point {_selectedIndex}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Vector2 v = _target.points[_selectedIndex];
            v.x = EditorGUILayout.Slider("X (normalised)", v.x, 0f, 1f);
            v.y = EditorGUILayout.Slider("Y (normalised)", v.y, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_target, "Move Spell Point");
                _target.points[_selectedIndex] = v;
                EditorUtility.SetDirty(_target);
            }

            EditorGUILayout.Space(4);

            // Move up / down in list
            GUILayout.BeginHorizontal();
            GUI.enabled = _selectedIndex > 0;
            if (GUILayout.Button("▲ Up"))
            {
                Undo.RecordObject(_target, "Reorder Spell Point");
                var tmp = _target.points[_selectedIndex - 1];
                _target.points[_selectedIndex - 1] = _target.points[_selectedIndex];
                _target.points[_selectedIndex]     = tmp;
                _selectedIndex--;
                EditorUtility.SetDirty(_target);
            }
            GUI.enabled = _selectedIndex < _target.points.Count - 1;
            if (GUILayout.Button("▼ Down"))
            {
                Undo.RecordObject(_target, "Reorder Spell Point");
                var tmp = _target.points[_selectedIndex + 1];
                _target.points[_selectedIndex + 1] = _target.points[_selectedIndex];
                _target.points[_selectedIndex]     = tmp;
                _selectedIndex++;
                EditorUtility.SetDirty(_target);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No point selected.\nClick a point on the canvas\nor drag to reposition it.",
                EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.Space(10);

        // ---- Point list ----
        GUILayout.Label($"All Points ({_target.points.Count})", EditorStyles.boldLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
            GUILayout.Height(Mathf.Min(_target.points.Count * 22 + 8, 150)));

        for (int i = 0; i < _target.points.Count; i++)
        {
            bool sel = (i == _selectedIndex);
            GUI.backgroundColor = sel ? new Color(0.35f, 0.75f, 1f, 0.3f) : Color.clear;

            if (GUILayout.Button(
                    $"{i}: ({_target.points[i].x:F2}, {_target.points[i].y:F2})",
                    EditorStyles.miniButton))
                _selectedIndex = sel ? -1 : i;
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // ---- Save ----
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("💾  Save Asset", GUILayout.Height(32)))
            SaveAsset();
        GUI.backgroundColor = Color.white;

        GUILayout.EndArea();
    }

    // ------------------------------------------------------------------
    // Canvas interaction
    // ------------------------------------------------------------------
    private void HandleCanvasEvents(Rect canvasRect)
    {
        Event e = Event.current;

        // Only process events that are within the canvas area
        if (!canvasRect.Contains(e.mousePosition) && !_isDragging)
            return;

        switch (e.type)
        {
            case EventType.MouseDown when e.button == 0:
            {
                int hit = HitTest(e.mousePosition, canvasRect);
                if (hit >= 0)
                {
                    _selectedIndex = hit;
                    _isDragging    = true;
                    GUI.FocusControl(null);
                }
                else
                {
                    // Click on empty canvas space → add point
                    Vector2 norm = CanvasToNorm(e.mousePosition, canvasRect);
                    Undo.RecordObject(_target, "Add Spell Point");
                    _target.points.Add(norm);
                    _selectedIndex = _target.points.Count - 1;
                    _isDragging    = true;
                    EditorUtility.SetDirty(_target);
                }
                e.Use();
                Repaint();
                break;
            }

            case EventType.MouseDrag when e.button == 0 && _isDragging:
            {
                if (_selectedIndex >= 0 && _selectedIndex < _target.points.Count)
                {
                    Undo.RecordObject(_target, "Move Spell Point");
                    Vector2 norm = CanvasToNorm(e.mousePosition, canvasRect);
                    norm.x = Mathf.Clamp01(norm.x);
                    norm.y = Mathf.Clamp01(norm.y);
                    _target.points[_selectedIndex] = norm;
                    EditorUtility.SetDirty(_target);
                }
                e.Use();
                Repaint();
                break;
            }

            case EventType.MouseUp when e.button == 0:
            {
                _isDragging = false;
                e.Use();
                Repaint();
                break;
            }

            case EventType.KeyDown when e.keyCode == KeyCode.Delete:
            {
                DeleteSelected();
                e.Use();
                break;
            }
        }
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>Returns the index of the first point under the mouse, or -1.</summary>
    private int HitTest(Vector2 mousePos, Rect canvasRect)
    {
        for (int i = 0; i < _target.points.Count; i++)
        {
            Vector2 screen = NormToCanvas(_target.points[i], canvasRect);
            if (Vector2.Distance(mousePos, screen) <= HIT_RADIUS)
                return i;
        }
        return -1;
    }

    /// <summary>Normalised [0,1] → screen pixel inside canvasRect.</summary>
    private Vector2 NormToCanvas(Vector2 norm, Rect rect)
    {
        return new Vector2(
            rect.x + CANVAS_PADDING + norm.x * (rect.width  - CANVAS_PADDING * 2),
            rect.y + CANVAS_PADDING + norm.y * (rect.height - CANVAS_PADDING * 2));
    }

    /// <summary>Screen pixel inside canvasRect → normalised [0,1].</summary>
    private Vector2 CanvasToNorm(Vector2 screenPos, Rect rect)
    {
        return new Vector2(
            (screenPos.x - rect.x - CANVAS_PADDING) / (rect.width  - CANVAS_PADDING * 2),
            (screenPos.y - rect.y - CANVAS_PADDING) / (rect.height - CANVAS_PADDING * 2));
    }

    private void AddPointCenter()
    {
        Undo.RecordObject(_target, "Add Spell Point");
        // Place new point slightly offset from the last one (or centre if empty)
        Vector2 pos = _target.points.Count > 0
            ? _target.points[_target.points.Count - 1] + new Vector2(0.05f, 0.05f)
            : new Vector2(0.5f, 0.5f);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        _target.points.Add(pos);
        _selectedIndex = _target.points.Count - 1;
        EditorUtility.SetDirty(_target);
        Repaint();
    }

    private void DeleteSelected()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _target.points.Count) return;
        Undo.RecordObject(_target, "Delete Spell Point");
        _target.points.RemoveAt(_selectedIndex);
        _selectedIndex = Mathf.Clamp(_selectedIndex - 1, -1, _target.points.Count - 1);
        EditorUtility.SetDirty(_target);
        Repaint();
    }

    private void SaveAsset()
    {
        if (_target == null) return;
        EditorUtility.SetDirty(_target);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SpellPatternEditor] Saved '{_target.name}'.");
    }

    private void CreateNewAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Spell Pattern", "NewSpellPattern", "asset",
            "Choose where to save the new Spell Pattern asset.");

        if (string.IsNullOrEmpty(path)) return;

        var asset = CreateInstance<SpellPatternData>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        _target        = asset;
        _selectedIndex = -1;
        Repaint();
    }
}
