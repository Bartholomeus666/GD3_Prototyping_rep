using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spell Wheel/Wheel Config")]
public class SpellWheelConfig : ScriptableObject
{
    [Serializable]
    public class SpellSlot
    {
        public SpellBase spell;
        [Range(0.05f, 1f)] public float percent = 0.25f;
    }

    public List<SpellSlot> slots = new();

    public float[] GetStartAngles()
    {
        var a = new float[slots.Count];
        float cursor = 0f;
        for (int i = 0; i < slots.Count; i++) { a[i] = cursor * 360f; cursor += slots[i].percent; }
        return a;
    }

    public int GetSliceIndexAtAngle(float angleDeg)
    {
        float norm = ((angleDeg % 360f) + 360f) % 360f / 360f;
        float cursor = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            cursor += slots[i].percent;
            if (norm < cursor) return i;
        }
        return slots.Count - 1;
    }

    public SpellBase GetSpellAtAngle(float angleDeg) =>
        slots.Count == 0 ? null : slots[GetSliceIndexAtAngle(angleDeg)].spell;

    public void Normalize()
    {
        float sum = 0f;
        foreach (var s in slots) { s.percent = Mathf.Max(0.05f, s.percent); sum += s.percent; }
        if (sum > 0f) foreach (var s in slots) s.percent /= sum;
    }
}
