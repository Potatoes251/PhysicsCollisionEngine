using Unity.VisualScripting;
using UnityEngine;

public class ColliderBox : BaseCollider
{
    [SerializeField] Vector3 center = Vector3.zero; // local space
    [SerializeField] Vector3 size = Vector3.one;  // local space, before scale

    public Vector3 WorldCenter => transform.TransformPoint(center);
    public Vector3 WorldSize => Vector3.Scale(size, transform.lossyScale);

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

        Gizmos.DrawWireCube(WorldCenter, WorldSize);
    }
}
