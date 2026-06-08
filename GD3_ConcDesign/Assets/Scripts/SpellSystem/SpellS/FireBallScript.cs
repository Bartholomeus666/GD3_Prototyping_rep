using UnityEngine;

public class FireBallScript : MonoBehaviour
{
    [SerializeField] private GameObject explosionEffectPrefab;

        private Rigidbody _rb;
    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }
    private void Update()
    {
        _rb.AddForce(transform.forward * 1f, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Fireball collided with: " + collision.gameObject.name);

        if (collision.gameObject.layer == 6)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }
}
