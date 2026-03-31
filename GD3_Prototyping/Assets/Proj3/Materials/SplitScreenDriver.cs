using UnityEngine;

public class SplitScreenDriver : MonoBehaviour
{
    [SerializeField] Material compositorMaterial;
    [SerializeField] Transform player1;
    [SerializeField] Transform player2;

    Camera cam;

    void Start() => cam = Camera.main;

    void Update()
    {
        Vector3 vp1 = cam.WorldToViewportPoint(player1.position);
        Vector3 vp2 = cam.WorldToViewportPoint(player2.position);

        Vector2 p1 = new Vector2(vp1.x, 1f - vp1.y); // flip Y
        Vector2 p2 = new Vector2(vp2.x, 1f - vp2.y); // flip Y

        Vector2 mid = (p1 + p2) * 0.5f;
        Vector2 dir = (p2 - p1).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        if (dir.sqrMagnitude < 0.0001f) return;

        compositorMaterial.SetVector("_SplitPoint", new Vector4(mid.x, mid.y, 0, 0));
        compositorMaterial.SetVector("_SplitNormal", new Vector4(normal.x, normal.y, 0, 0));
    }
}