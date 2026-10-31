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

    protected override void SetInertia()
    {
        Vector3 s = transform.lossyScale;
        float r = radius * Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z));
        float h = 2f * halfHeight * Mathf.Abs(s.y);          // straight cylinder length
        float m = attachedBody.mass;

        // Split mass by volume (uniform density)
        float vCyl = Mathf.PI * r * r * h;
        float vSph = 4f / 3f * Mathf.PI * r * r * r;
        float mCyl = m * vCyl / (vCyl + vSph);
        float mHemi = (m - mCyl) * 0.5f;                     // each hemisphere
        float r2 = r * r;

        // About the capsule axis (Y)
        float iAxis = 0.5f * mCyl * r2 + 2f * mHemi * 0.4f * r2;

        // Perpendicular to the axis (X and Z)
        float iCylPerp = mCyl * (3f * r2 + h * h) / 12f;
        float hemiCom = 3f * r / 8f;                         // hemisphere COM from its flat face
        float iHemiCom = 0.4f * mHemi * r2 - mHemi * hemiCom * hemiCom;
        float d = h * 0.5f + hemiCom;                        // hemisphere COM to capsule center
        float iPerp = iCylPerp + 2f * (iHemiCom + mHemi * d * d);

        Vector3 inertia = new(iPerp, iAxis, iPerp);

        // Parallel axis theorem for the collider's center offset (diagonal terms only)
        Vector3 c = Vector3.Scale(center, s);
        inertia.x += m * (c.y * c.y + c.z * c.z);
        inertia.y += m * (c.x * c.x + c.z * c.z);
        inertia.z += m * (c.x * c.x + c.y * c.y);

        attachedBody.SetLocalInertia(new(1f / inertia.x, 1f / inertia.y, 1f / inertia.z));
    }

    public override Vector3 Support(Vector3 dir)
    {
        Vector3 n = dir.normalized;
        Vector3 tip = Vector3.Dot(n, Axis) < 0f ? p0 : p1;
        return tip + radius * n;
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Handles.matrix = transform.localToWorldMatrix;

        Vector3 top = Vector3.up * halfHeight;
        Vector3 bot = Vector3.down * halfHeight;

        // Rings where the caps meet the cylinder
        Handles.DrawWireDisc(top, Vector3.up, radius);
        Handles.DrawWireDisc(bot, Vector3.up, radius);

        // Top hemisphere arcs
        Handles.DrawWireArc(top, Vector3.forward, Vector3.right, 180f, radius);
        Handles.DrawWireArc(top, Vector3.right, Vector3.back, 180f, radius);

        // Bottom hemisphere arcs
        Handles.DrawWireArc(bot, Vector3.forward, Vector3.left, 180f, radius);
        Handles.DrawWireArc(bot, Vector3.right, Vector3.forward, 180f, radius);

        // Side lines
        Handles.DrawLine(top + Vector3.right * radius, bot + Vector3.right * radius);
        Handles.DrawLine(top + Vector3.left * radius, bot + Vector3.left * radius);
        Handles.DrawLine(top + Vector3.forward * radius, bot + Vector3.forward * radius);
        Handles.DrawLine(top + Vector3.back * radius, bot + Vector3.back * radius);
    }
}
