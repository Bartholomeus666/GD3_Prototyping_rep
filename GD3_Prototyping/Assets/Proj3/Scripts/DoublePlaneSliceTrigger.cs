using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoublePlaneSliceTrigger : MonoBehaviour
{
    public InputAction sliceAction;
    public Transform planeA;
    public Transform planeB;

    public EventHandler<TSliceEventArgs> SliceEvent;

    void OnEnable() => sliceAction.Enable();
    void OnDisable() => sliceAction.Disable();

    void Update()
    {
        if (sliceAction.WasPressedThisFrame())
            SliceAll();
    }

    void SliceAll()
    {
        if (planeA == null || planeB == null)
        {
            Debug.LogWarning("DoublePlaneSliceTrigger: assign both planes.");
            return;
        }

        Plane pA = new Plane(planeA.up, planeA.position);
        Plane pB = new Plane(planeB.up, planeB.position);

        // "Outside A" is the side of pA that does NOT contain planeB
        bool outsideAIsAbove = !pA.GetSide(planeB.position);
        bool outsideBIsAbove = !pB.GetSide(planeA.position);

        List<GameObject> sideA = new List<GameObject>();
        List<GameObject> sideB = new List<GameObject>();

        List<MeshFilter> targets = new List<MeshFilter>(
            FindObjectsByType<MeshFilter>(FindObjectsSortMode.None));

        foreach (MeshFilter mf in targets)
        {
            if (mf == null) continue;

            GameObject go = mf.gameObject;
            if (go == planeA.gameObject || go == planeB.gameObject) continue;

            ProcessObject(go,
                pA, planeA.position, planeA.up,
                pB, planeB.position, planeB.up,
                outsideAIsAbove, outsideBIsAbove,
                sideA, sideB);
        }

        GroupUnderParent(sideA, "Side_A");
        GroupUnderParent(sideB, "Side_B");


        Debug.Log($"{sideA[0].transform.parent.position}, {sideB[0].transform.parent.position}");
        SliceEvent?.Invoke(this, new TSliceEventArgs(sideA[0].transform.parent.gameObject, sideB[0].transform.parent.gameObject));

    }

    static void ProcessObject(
        GameObject go,
        Plane pA, Vector3 posA, Vector3 normalA,
        Plane pB, Vector3 posB, Vector3 normalB,
        bool outsideAIsAbove, bool outsideBIsAbove,
        List<GameObject> sideA, List<GameObject> sideB)
    {
        GameObject outsideA = null;
        GameObject insideA = null;

        if (!IntersectsPlane(go, pA))
        {
            if (pA.GetSide(BoundsCenter(go)) == outsideAIsAbove) outsideA = go;
            else insideA = go;
        }
        else
        {
            GameObject[] halvesA = MeshSlicer.Slice(go, posA, normalA);
            if (halvesA != null)
            {
                outsideA = outsideAIsAbove ? halvesA[0] : halvesA[1];
                insideA = outsideAIsAbove ? halvesA[1] : halvesA[0];
            }
            else
            {
                if (pA.GetSide(BoundsCenter(go)) == outsideAIsAbove) outsideA = go;
                else insideA = go;
            }
        }

        if (outsideA != null) sideA.Add(outsideA);
        if (insideA == null) return;

        if (!IntersectsPlane(insideA, pB))
        {
            if (pB.GetSide(BoundsCenter(insideA)) == outsideBIsAbove) sideB.Add(insideA);
            else UnityEngine.Object.Destroy(insideA);
            return;
        }

        GameObject[] halvesB = MeshSlicer.Slice(insideA, posB, normalB);
        if (halvesB == null)
        {
            if (pB.GetSide(BoundsCenter(insideA)) == outsideBIsAbove) sideB.Add(insideA);
            else UnityEngine.Object.Destroy(insideA);
            return;
        }

        GameObject outsideB = outsideBIsAbove ? halvesB[0] : halvesB[1];
        GameObject middle = outsideBIsAbove ? halvesB[1] : halvesB[0];

        sideB.Add(outsideB);
        UnityEngine.Object.Destroy(middle);
    }

    static void GroupUnderParent(List<GameObject> pieces, string name)
    {
        if (pieces.Count == 0) return;

        GameObject parent = new GameObject(name);

        foreach (GameObject piece in pieces)
            piece.transform.SetParent(parent.transform, worldPositionStays: true);
    }

    static bool IntersectsPlane(GameObject go, Plane plane)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null) return false;

        Transform t = go.transform;
        bool hasAbove = false, hasBelow = false;

        foreach (Vector3 v in mf.sharedMesh.vertices)
        {
            if (plane.GetSide(t.TransformPoint(v))) hasAbove = true;
            else hasBelow = true;
            if (hasAbove && hasBelow) return true;
        }

        return false;
    }

    static Vector3 BoundsCenter(GameObject go)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            return go.transform.TransformPoint(mf.sharedMesh.bounds.center);
        return go.transform.position;
    }
}

public class TSliceEventArgs: EventArgs
{
    public GameObject SideA;
    public GameObject SideB;

    public TSliceEventArgs(GameObject sideA, GameObject sideB)
    {
        SideA = sideA; 
        SideB = sideB;
    }
}