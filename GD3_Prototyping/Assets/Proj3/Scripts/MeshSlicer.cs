using System.Collections.Generic;
using UnityEngine;

public static class MeshSlicer
{
    public static GameObject[] Slice(GameObject target, Vector3 planePoint, Vector3 planeNormal)
    {
        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("MeshSlicer: target has no MeshFilter or mesh.");
            return null;
        }

        Mesh original = mf.sharedMesh;

        Transform t = target.transform;
        Vector3 localNormal = t.InverseTransformDirection(planeNormal).normalized;
        Vector3 localPoint = t.InverseTransformPoint(planePoint);
        Plane localPlane = new Plane(localNormal, localPoint);

        MeshData above = new MeshData();
        MeshData below = new MeshData();
        List<Vector3> cutPoints = new List<Vector3>();
        List<Vector2> cutUVs = new List<Vector2>();

        Vector3[] verts = original.vertices;
        Vector3[] norms = original.normals;
        Vector2[] uvs = original.uv;
        int[] tris = original.triangles;

        bool hasUVs = uvs != null && uvs.Length == verts.Length;
        if (!hasUVs) uvs = new Vector2[verts.Length];

        for (int i = 0; i < tris.Length; i += 3)
        {
            int iA = tris[i], iB = tris[i + 1], iC = tris[i + 2];
            SliceTriangle(
                verts[iA], verts[iB], verts[iC],
                norms[iA], norms[iB], norms[iC],
                uvs[iA], uvs[iB], uvs[iC],
                localPlane, above, below, cutPoints, cutUVs);
        }

        if (above.vertices.Count == 0 || below.vertices.Count == 0)
        {
            Debug.Log("MeshSlicer: plane does not intersect the mesh.");
            return null;
        }

        BuildCap(cutPoints, cutUVs, localPlane, above, below);

        GameObject aboveGO = CreateSlice(target, above, "Above");
        GameObject belowGO = CreateSlice(target, below, "Below");

        Object.Destroy(target);

        return new GameObject[] { aboveGO, belowGO };
    }

    static void SliceTriangle(
        Vector3 vA, Vector3 vB, Vector3 vC,
        Vector3 nA, Vector3 nB, Vector3 nC,
        Vector2 uA, Vector2 uB, Vector2 uC,
        Plane plane,
        MeshData above, MeshData below,
        List<Vector3> cutPoints, List<Vector2> cutUVs)
    {
        float dA = SignedDistance(plane, vA);
        float dB = SignedDistance(plane, vB);
        float dC = SignedDistance(plane, vC);

        bool aAbove = dA >= 0f, bAbove = dB >= 0f, cAbove = dC >= 0f;

        if (aAbove && bAbove && cAbove) { above.AddTriangle(vA, vB, vC, nA, nB, nC, uA, uB, uC); return; }
        if (!aAbove && !bAbove && !cAbove) { below.AddTriangle(vA, vB, vC, nA, nB, nC, uA, uB, uC); return; }

        Vector3 soloV, v1, v2, soloN, n1, n2;
        Vector2 soloU, u1, u2;
        float soloD, d1, d2;
        bool soloAbove;

        if (aAbove == bAbove)
        {
            soloV = vC; v1 = vA; v2 = vB; soloN = nC; n1 = nA; n2 = nB;
            soloU = uC; u1 = uA; u2 = uB; soloD = dC; d1 = dA; d2 = dB; soloAbove = cAbove;
        }
        else if (aAbove == cAbove)
        {
            soloV = vB; v1 = vA; v2 = vC; soloN = nB; n1 = nA; n2 = nC;
            soloU = uB; u1 = uA; u2 = uC; soloD = dB; d1 = dA; d2 = dC; soloAbove = bAbove;
        }
        else
        {
            soloV = vA; v1 = vB; v2 = vC; soloN = nA; n1 = nB; n2 = nC;
            soloU = uA; u1 = uB; u2 = uC; soloD = dA; d1 = dB; d2 = dC; soloAbove = aAbove;
        }

        float t1 = soloD / (soloD - d1);
        float t2 = soloD / (soloD - d2);
        Vector3 i1 = Vector3.Lerp(soloV, v1, t1);
        Vector3 i2 = Vector3.Lerp(soloV, v2, t2);
        Vector3 in1 = Vector3.Lerp(soloN, n1, t1).normalized;
        Vector3 in2 = Vector3.Lerp(soloN, n2, t2).normalized;
        Vector2 iu1 = Vector2.Lerp(soloU, u1, t1);
        Vector2 iu2 = Vector2.Lerp(soloU, u2, t2);

        cutPoints.Add(i1); cutUVs.Add(iu1);
        cutPoints.Add(i2); cutUVs.Add(iu2);

        if (soloAbove)
        {
            above.AddTriangle(soloV, i1, i2, soloN, in1, in2, soloU, iu1, iu2);
            below.AddTriangle(v1, v2, i2, n1, n2, in2, u1, u2, iu2);
            below.AddTriangle(v1, i2, i1, n1, in2, in1, u1, iu2, iu1);
        }
        else
        {
            below.AddTriangle(soloV, i1, i2, soloN, in1, in2, soloU, iu1, iu2);
            above.AddTriangle(v1, v2, i2, n1, n2, in2, u1, u2, iu2);
            above.AddTriangle(v1, i2, i1, n1, in2, in1, u1, iu2, iu1);
        }
    }

    static void BuildCap(
        List<Vector3> cutPoints, List<Vector2> cutUVs,
        Plane plane,
        MeshData above, MeshData below)
    {
        if (cutPoints.Count < 4) return;

        List<Vector3> loop = new List<Vector3>();
        List<Vector2> loopUVs = new List<Vector2>();

        List<(Vector3 pos, Vector2 uv)> remaining = new List<(Vector3, Vector2)>();
        for (int i = 0; i < cutPoints.Count; i += 2)
        {
            remaining.Add((cutPoints[i], cutUVs[i]));
            remaining.Add((cutPoints[i + 1], cutUVs[i + 1]));
        }

        loop.Add(remaining[0].pos);
        loopUVs.Add(remaining[0].uv);
        Vector3 next = remaining[1].pos;
        Vector2 nextUV = remaining[1].uv;
        remaining.RemoveRange(0, 2);

        while (remaining.Count > 0)
        {
            loop.Add(next);
            loopUVs.Add(nextUV);

            int closestIndex = -1;
            float closestDist = float.MaxValue;
            for (int i = 0; i < remaining.Count; i++)
            {
                float d = Vector3.SqrMagnitude(remaining[i].pos - next);
                if (d < closestDist) { closestDist = d; closestIndex = i; }
            }

            if (closestIndex == -1) break;

            int partnerIndex = (closestIndex % 2 == 0) ? closestIndex + 1 : closestIndex - 1;
            next = remaining[partnerIndex].pos;
            nextUV = remaining[partnerIndex].uv;

            int removeAt = Mathf.Min(closestIndex, partnerIndex);
            remaining.RemoveAt(removeAt + 1);
            remaining.RemoveAt(removeAt);
        }

        if (loop.Count < 3) return;

        Vector3 center = Vector3.zero;
        Vector2 centerUV = Vector2.zero;
        foreach (var v in loop) center += v;
        foreach (var uv in loopUVs) centerUV += uv;
        center /= loop.Count;
        centerUV /= loopUVs.Count;

        Vector3 capNormalAbove = plane.normal;
        Vector3 capNormalBelow = -plane.normal;

        for (int i = 0; i < loop.Count; i++)
        {
            int next2 = (i + 1) % loop.Count;
            Vector3 p0 = loop[i], p1 = loop[next2];
            Vector2 u0 = loopUVs[i], u1 = loopUVs[next2];

            above.AddTriangle(center, p0, p1, capNormalAbove, capNormalAbove, capNormalAbove, centerUV, u0, u1);
            below.AddTriangle(center, p1, p0, capNormalBelow, capNormalBelow, capNormalBelow, centerUV, u1, u0);
        }
    }

    static float SignedDistance(Plane plane, Vector3 point)
        => Vector3.Dot(plane.normal, point) + plane.distance;

    static GameObject CreateSlice(GameObject original, MeshData data, string suffix)
    {
        Mesh mesh = new Mesh();
        mesh.name = original.name + "_" + suffix;
        mesh.vertices = data.vertices.ToArray();
        mesh.normals = data.normals.ToArray();
        mesh.uv = data.uvs.ToArray();
        mesh.triangles = data.triangles.ToArray();
        mesh.RecalculateBounds();

        GameObject go = new GameObject(mesh.name);
        go.transform.SetPositionAndRotation(original.transform.position, original.transform.rotation);
        go.transform.localScale = original.transform.localScale;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer origRenderer = original.GetComponent<MeshRenderer>();
        MeshRenderer newRenderer = go.AddComponent<MeshRenderer>();
        if (origRenderer != null)
            newRenderer.sharedMaterials = origRenderer.sharedMaterials;

        go.AddComponent<MeshCollider>().sharedMesh = mesh;

        return go;
    }

    class MeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uvs = new List<Vector2>();
        public List<int> triangles = new List<int>();

        public void AddTriangle(
            Vector3 v0, Vector3 v1, Vector3 v2,
            Vector3 n0, Vector3 n1, Vector3 n2,
            Vector2 u0, Vector2 u1, Vector2 u2)
        {
            int baseIndex = vertices.Count;
            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2);
            normals.Add(n0); normals.Add(n1); normals.Add(n2);
            uvs.Add(u0); uvs.Add(u1); uvs.Add(u2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
        }
    }
}