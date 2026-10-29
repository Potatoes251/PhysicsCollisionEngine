using Unity.Mathematics;
using UnityEngine;

public class ColliderSphere : BaseCollider
{
    [SerializeField]
    float radius = 1f;

    [SerializeField]
    Vector3 centerLocal;

    Vector3 center => math.transform(transform.localToWorldMatrix, centerLocal);

    protected override void Awake()
    {
        base.Awake();

        localBounds.center = centerLocal;
        localBounds.extents = Vector3.one * radius;
    }

    public override Vector3 Support(Vector3 dir)
    {
        return center + radius * dir.normalized;
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;

        Gizmos.DrawWireSphere(center, radius);
    }
}
