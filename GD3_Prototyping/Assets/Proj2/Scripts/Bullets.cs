using System;
using UnityEngine;

public class Bullets : MonoBehaviour
{
    public EventHandler onBulletHit;

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bullet collided with: " + collision.gameObject.name);
        if (collision.gameObject.CompareTag("Player"))
        {
            onBulletHit.Invoke(this, EventArgs.Empty);
            // Handle player hit logic here, e.g., reduce health
            Debug.Log("Player hit!");
        }
        if (collision.gameObject.CompareTag("Turret"))
        {
            Destroy(collision.gameObject);
        }
        Destroy(gameObject);
    }
}
