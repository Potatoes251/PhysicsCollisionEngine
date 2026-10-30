using UnityEngine;

public class ColliderBox : BaseCollider
{
    [SerializeField] Vector3 center = Vector3.zero; // local space
    [SerializeField] Vector3 size = Vector3.one;  // local space, before scale

    protected override void Awake()
    {
        base.Awake();
        localBounds.center = center;
        localBounds.extents = size * .5f;
    }

    protected override void SetInertia()
    {
        Vector3 worldSize = Vector3.Scale(transform.lossyScale, size);

        float x2 = worldSize.x * worldSize.x;
        float y2 = worldSize.y * worldSize.y;
        float z2 = worldSize.z * worldSize.z;
        float k = attachedBody.mass / 12f;
        attachedBody.SetLocalInertia(new Vector3(1f / (k * (y2 + z2)), 1f / (k * (x2 + z2)), 1f / (k * (x2 + y2))));
    }

    public override Vector3 Support(Vector3 dir)
    {
        Vector3 localDir = transform.InverseTransformDirection(dir);

        Vector3 half = size * 0.5f;
        Vector3 localPoint = center + new Vector3(
            localDir.x >= 0 ? half.x : -half.x,
            localDir.y >= 0 ? half.y : -half.y,
            localDir.z >= 0 ? half.z : -half.z);

        return transform.TransformPoint(localPoint);
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(center, size);
    }
}
