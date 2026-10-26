using Unity.Mathematics;
using UnityEngine;

public abstract class BaseCollider : MonoBehaviour
{
    private AABB localBounds;

    [HideInInspector]
    public AABB bounds => ToWorldBounds();


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

    private void Awake()
    {
        FitToRenderers();
    }

    public abstract Vector3 Support(Vector3 dir);

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

    protected static Vector3 InverseScale(Vector3 s) =>
        new(s.x != 0 ? 1f / s.x : 0, s.y != 0 ? 1f / s.y : 0, s.z != 0 ? 1f / s.z : 0);
}
