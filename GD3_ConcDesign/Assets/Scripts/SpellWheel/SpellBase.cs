using UnityEngine;

/// <summary>
/// Base ScriptableObject for all spells. Create a new spell by inheriting from this.
/// </summary>
public abstract class SpellBase : ScriptableObject
{
    [Header("Identity")]
    public string spellName = "New Spell";
    public Color wheelColor = Color.white;
    public Sprite icon;

    [Header("Visuals")]
    public GameObject projectilePrefab;

    public abstract void Cast(Transform origin);
}
