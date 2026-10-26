using Unity.Mathematics;
using UnityEngine;

public class ColliderSphere : BaseCollider
{
    [SerializeField]
    float radius = 1f;

    [SerializeField]
    Vector3 centerLocal;

    Vector3 center => math.transform(transform.localToWorldMatrix, centerLocal);

    public override Vector3 Support(Vector3 dir)
    {
        return center + radius * dir.normalized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(center, radius);
    }
}
