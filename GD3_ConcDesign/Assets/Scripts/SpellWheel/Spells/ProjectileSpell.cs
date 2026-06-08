using UnityEngine;

[CreateAssetMenu(menuName = "Spells/ProjectileSpell")]
public class ProjectileSpell : SpellBase
{
    public override void Cast(Transform origin)
    {
        if (projectilePrefab == null) { Debug.LogWarning($"{base.name}: no projectile prefab assigned."); return; }

        GameObject go = Instantiate(projectilePrefab, origin.position, origin.rotation);
        

        Object.Destroy(go, 6f); // safety cleanup
    }
}
