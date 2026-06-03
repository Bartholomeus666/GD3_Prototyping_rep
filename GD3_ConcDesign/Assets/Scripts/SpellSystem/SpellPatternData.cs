using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ScriptableObject asset that stores the ordered list of 2D points
/// defining a spell gesture pattern.
///
/// Create via:  Assets > Create > Spell System > Spell Pattern
/// </summary>
[CreateAssetMenu(
    fileName = "NewSpellPattern",
    menuName  = "Spell System/Spell Pattern",
    order     = 1)]
public class SpellPatternData : ScriptableObject
{
    [Tooltip("Ordered list of normalised 2D points (each component in [0,1]) " +
             "that define the gesture path.")]
    public List<Vector2> points = new List<Vector2>();
    public GameObject spellPrefab;

    public List<Vector2> GetScaledPoints(float width, float height)
    {
        var result = new List<Vector2>(points.Count);
        foreach (var p in points)
            result.Add(new Vector2(p.x * width, p.y * height));
        return result;
    }

    /// <summary>
    /// Quick read-only indexer so runtime code can do  pattern[i]  directly.
    /// </summary>
    public Vector2 this[int i] => points[i];

    /// <summary>Number of points in the pattern.</summary>
    public int Count => points.Count;
}
