using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public event EventHandler OnSpellBookOpened;

    public void Interact(InputAction.CallbackContext context)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, 4f);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("SpellBook"))
            {
                OnSpellBookOpened?.Invoke(this, EventArgs.Empty);
                break;
            }
        }
    }
}
