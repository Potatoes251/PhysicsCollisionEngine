using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

            // child local -> this object's local (pre-scale space)
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
