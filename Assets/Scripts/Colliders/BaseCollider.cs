using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(RigidBody))]
public abstract class BaseCollider : MonoBehaviour
{
    protected AABB localBounds;

    [HideInInspector]
    public AABB bounds => ToWorldBounds();

    [SerializeField]
    protected bool debug = false;

    [HideInInspector]
    public RigidBody attachedBody = null;

    private AABB ToWorldBounds()
    {
        float4x4 localToWorld = transform.localToWorldMatrix;

        float3 center = math.transform(localToWorld, localBounds.center);

        // Rotate/scale the extents using the absolute value of the 3x3 part
        float3x3 abs3 = new float3x3(
            math.abs(localToWorld.c0.xyz),
            math.abs(localToWorld.c1.xyz),
            math.abs(localToWorld.c2.xyz));

        float3 extents = math.mul(abs3, localBounds.extents);

        return new AABB { center = center, extents = extents };
    }

    protected virtual void Awake()
    {
        RigidBody body = GetComponent<RigidBody>();

        if (body != null)
        {
            attachedBody = body;
        }
    }

    public abstract Vector3 Support(Vector3 dir);

    protected static Vector3 InverseScale(Vector3 s) =>
        new(s.x != 0 ? 1f / s.x : 0, s.y != 0 ? 1f / s.y : 0, s.z != 0 ? 1f / s.z : 0);
}
