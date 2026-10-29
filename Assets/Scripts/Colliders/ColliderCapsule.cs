using UnityEditor;
using UnityEngine;

public class ColliderCapsule : BaseCollider
{
    [SerializeField]
    float radius = .5f;

    [SerializeField]
    float halfHeight = .5f;

    [SerializeField]
    Vector3 center;

    public Vector3 worldCenter => transform.TransformPoint(center);

    public Vector3 p0 => worldCenter - halfHeight * transform.up;
    public Vector3 p1 => worldCenter + halfHeight * transform.up;

    Vector3 Axis => transform.up;

    protected override void Awake()
    {
        base.Awake();

        localBounds.center = center;
        localBounds.extents = new Vector3(radius, halfHeight + radius, radius);
    }

    public override Vector3 Support(Vector3 dir)
    {
        Vector3 n = dir.normalized;
        Vector3 tip = Vector3.Dot(n, Axis) < 0f ? p0 : p1;
        return tip + radius * n;
    }

    //private void OnDrawGizmos()
    //{
    //    if (!debug) return;

    //    Handles.matrix = transform.localToWorldMatrix;

    //    Vector3 top = Vector3.up * halfHeight;
    //    Vector3 bot = Vector3.down * halfHeight;

    //    // Rings where the caps meet the cylinder
    //    Handles.DrawWireDisc(top, Vector3.up, radius);
    //    Handles.DrawWireDisc(bot, Vector3.up, radius);

    //    // Top hemisphere arcs
    //    Handles.DrawWireArc(top, Vector3.forward, Vector3.right, 180f, radius);
    //    Handles.DrawWireArc(top, Vector3.right, Vector3.back, 180f, radius);

    //    // Bottom hemisphere arcs
    //    Handles.DrawWireArc(bot, Vector3.forward, Vector3.left, 180f, radius);
    //    Handles.DrawWireArc(bot, Vector3.right, Vector3.forward, 180f, radius);

    //    // Side lines
    //    Handles.DrawLine(top + Vector3.right * radius, bot + Vector3.right * radius);
    //    Handles.DrawLine(top + Vector3.left * radius, bot + Vector3.left * radius);
    //    Handles.DrawLine(top + Vector3.forward * radius, bot + Vector3.forward * radius);
    //    Handles.DrawLine(top + Vector3.back * radius, bot + Vector3.back * radius);
    //}
}
