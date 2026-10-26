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
        MeshFilter[] meshes = transform.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter filter in meshes)
        {
            if (filter == null || filter.mesh == null) continue;

            Mesh mesh = filter.mesh;
            this.meshes.Add(mesh);
            vertices.AddRange(mesh.vertices);
        }
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
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
