using Unity.VisualScripting;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    private void FixedUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x, transform.position.y, target.position.z);
    }
}
