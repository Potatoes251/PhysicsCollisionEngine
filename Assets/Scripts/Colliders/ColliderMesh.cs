using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class ColliderMesh : BaseCollider
{
    private readonly List<Vector3> vertices = new();
    private readonly List<Mesh> meshes = new();

    protected override void Awake()
    {
        base.Awake();

        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;
        bool has = false;

        foreach (MeshFilter filter in GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null) continue;
            meshes.Add(mesh);

            Matrix4x4 m = worldToLocal * filter.transform.localToWorldMatrix;

            List<Vector3> tmpVerts = new();

            mesh.GetVertices(tmpVerts);
            for (int i = 0; i < tmpVerts.Count; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(tmpVerts[i]);
                vertices.Add(p);

                if (!has) 
                { 
                    localBounds = new AABB { center = p, extents = Vector3.zero }; 
                    has = true; 
                }
                else localBounds.Encapsulate(p);
            }
        }

        if (!has) localBounds = new AABB { center = Vector3.zero, extents = Vector3.one };
    }

    public void FitToRenderers()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            localBounds = new AABB { center = Vector3.zero, extents = Vector3.one * 0.5f };
            return;
        }

        // Combine world bounds of all renderers, then convert to local space
        Bounds combined = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combined.Encapsulate(renderers[i].bounds);

        localBounds = new AABB
        {
            center = transform.InverseTransformPoint(combined.center),
            extents = Vector3.Scale(combined.extents, InverseScale(transform.lossyScale))
        };
    }

    protected override void SetInertia()
    {
        Vector3 s = transform.lossyScale;
        float m = attachedBody.mass;

        float volume = 0f;
        // Second moments (integral of p * p^T) about the body origin, unit density
        float xx = 0f, yy = 0f, zz = 0f;

        foreach (Mesh mesh in meshes)
        {
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = Vector3.Scale(verts[tris[i]], s);
                Vector3 b = Vector3.Scale(verts[tris[i + 1]], s);
                Vector3 c = Vector3.Scale(verts[tris[i + 2]], s);

                // Signed volume of the tetrahedron (origin, a, b, c)
                float v = Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                volume += v;

                Vector3 sum = a + b + c;
                float w = v / 20f;
                xx += w * (a.x * a.x + b.x * b.x + c.x * c.x + sum.x * sum.x);
                yy += w * (a.y * a.y + b.y * b.y + c.y * c.y + sum.y * sum.y);
                zz += w * (a.z * a.z + b.z * b.z + c.z * c.z + sum.z * sum.z);
            }
        }

        Vector3 inertia = Vector3.zero;

        if (Mathf.Abs(volume) > 1e-9f)
        {
            float density = m / volume;   // sign cancels with negative scale / flipped winding
                                          // Inertia about the body origin (the point your body rotates about)
            inertia = new Vector3(
                density * (yy + zz),
                density * (xx + zz),
                density * (xx + yy));
        }

        attachedBody.SetLocalInertia(new(1f / inertia.x, 1f / inertia.y, 1f / inertia.z));
    }

    public override Vector3 Support(Vector3 dir)
    {
        Vector3 support = new();
        float maxProjection = float.MinValue;
        
        foreach (Vector3 v in vertices)
        {
            Vector3 vertex = transform.TransformPoint(v);
            float projection = Vector3.Dot(vertex, dir);
            if (projection > maxProjection)
            {
                maxProjection = projection;
                support = vertex;
            }
        }
        return support;
    }

    void OnDrawGizmos()
    {
        if (!debug) return;

        foreach (Mesh mesh in meshes)
        {
            Gizmos.DrawWireMesh(mesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
