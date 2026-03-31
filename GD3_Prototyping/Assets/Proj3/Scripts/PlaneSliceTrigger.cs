using UnityEngine;
using UnityEngine.InputSystem;

public class PlaneSliceTrigger : MonoBehaviour
{
    public enum KeepSide { Below, Above }

    public InputAction sliceAction;
    public KeepSide keepSide = KeepSide.Below;

    void OnEnable()  => sliceAction.Enable();
    void OnDisable() => sliceAction.Disable();

    void Update()
    {
        if (sliceAction.WasPressedThisFrame())
            SliceAll();
    }

    void SliceAll()
    {
        Plane plane = new Plane(transform.up, transform.position);

        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            GameObject go = mf.gameObject;

            if (go == gameObject) continue;
            if (!IntersectsPlane(mf, plane)) continue;

            GameObject[] halves = MeshSlicer.Slice(go, transform.position, transform.up);
            if (halves == null) continue;

            GameObject keep    = keepSide == KeepSide.Above ? halves[0] : halves[1];
            GameObject discard = keepSide == KeepSide.Above ? halves[1] : halves[0];

            Destroy(discard);
        }
    }

    static bool IntersectsPlane(MeshFilter mf, Plane plane)
    {
        Vector3[] verts = mf.sharedMesh.vertices;
        Transform t     = mf.transform;

        bool hasAbove = false, hasBelow = false;

        foreach (Vector3 v in verts)
        {
            if (plane.GetSide(t.TransformPoint(v))) hasAbove = true;
            else                                    hasBelow = true;

            if (hasAbove && hasBelow) return true;
        }

        return false;
    }
}
