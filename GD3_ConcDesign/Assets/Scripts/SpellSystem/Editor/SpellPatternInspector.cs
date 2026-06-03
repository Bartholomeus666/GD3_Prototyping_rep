// ============================================================
// SpellPatternInspector.cs
// Place inside any Editor/ folder.
//
// Adds an "Open in Pattern Editor" button to the inspector
// whenever you select a SpellPatternData asset in the Project.
// ============================================================

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SpellPatternData))]
public class SpellPatternInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default fields (the 'points' list)
        DrawDefaultInspector();

        EditorGUILayout.Space(8);

        GUI.backgroundColor = new Color(0.3f, 0.65f, 1f);
        if (GUILayout.Button("Open in Pattern Editor", GUILayout.Height(30)))
            SpellPatternEditorWindow.OpenWithAsset((SpellPatternData)target);
        GUI.backgroundColor = Color.white;
    }
}
