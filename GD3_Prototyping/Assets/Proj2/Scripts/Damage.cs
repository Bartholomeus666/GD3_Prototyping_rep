using TMPro;
using UnityEngine;

public class Damage : MonoBehaviour
{
    public int AttackLayer;
    public float MinimumDamageVelocity = 5f;

    public TMP_Text DamageText;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == AttackLayer)
        {
            if(Vector3.Magnitude(collision.gameObject.GetComponent<Rigidbody>().linearVelocity) > MinimumDamageVelocity)
            {
                DamageText.text = Vector3.Magnitude(collision.gameObject.GetComponent<Rigidbody>().linearVelocity).ToString("F2");
            }
        }
    }
}
