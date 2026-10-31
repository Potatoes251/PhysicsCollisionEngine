using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using static DynamicTree;

public class PhysicsManager : MonoBehaviour
{
    [SerializeField]
    private Vector3 gravity = new(0f, -9.81f, 0f);

    [Header("Debug")]
    [SerializeField]
    private bool enableGravity = true;
    [SerializeField]
    private bool enableDamping = true;
    [SerializeField]
    private float timeMult = 1f;

    private readonly List<HitInfo> hitResults = new();
    private List<(int x, int y)> pairs = new();

    private readonly Benchmark updateTree = new("updateTree");
    private readonly Benchmark broad = new("Broad");
    private readonly Benchmark pairCount = new("pairCount");
    private readonly Benchmark narrow = new("Narrow");
    private readonly Benchmark fps = new("FPS");

    private void Start()
    {
        fps.countToPrint = 3000;
    }

    private void ResolveCollision(RigidBody body1, RigidBody body2, HitInfo hitInfo)
    {
        float sumInvMass = (body1.invMass + body2.invMass);

        if (sumInvMass == 0f) return;

        float dot = Vector3.Dot(body2.velocity - body1.velocity, hitInfo.normal);

        if (dot < 0f)
        {
            Vector3 r1xn = Vector3.Cross(hitInfo.hitPoint - body1.transform.position, hitInfo.normal);
            Vector3 r2xn = Vector3.Cross(hitInfo.hitPoint - body2.transform.position, hitInfo.normal);
            float denom = sumInvMass
                    + Vector3.Dot(r1xn, body1.ApplyInvInertia(r1xn))
                    + Vector3.Dot(r2xn, body2.ApplyInvInertia(r2xn));
            if (denom == 0f) return;
            
            float bounciness = (body1.bounciness + body2.bounciness) * .5f; // average of both bodies
                
            float impulse = -(1 + bounciness) * dot / denom;

            body1.AddImpulse(-impulse * hitInfo.normal, hitInfo.hitPoint - body1.transform.position);
            body2.AddImpulse(impulse * hitInfo.normal, hitInfo.hitPoint - body2.transform.position);
        }

        // apply correction
        float correction = Mathf.Max(hitInfo.depth, 0f) / sumInvMass;
        body1.transform.position -= correction * body1.invMass * hitInfo.normal;
        body2.transform.position += correction * body2.invMass * hitInfo.normal;
    }

    private void Update()
    {
        fps.Add(1f / Time.deltaTime);
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime * timeMult;
        foreach (BaseCollider collider in DynamicTree.instance.colliders)
        {
            RigidBody b = collider.attachedBody;

            if (b == null || b.invMass == 0) continue;

            if (enableGravity)
                b.AddForce(gravity * b.mass);
            b.ApplyForce(dt);
            if (enableDamping)
                b.ApplyDamping();

            b.IntegrateTransform(dt);
        }

        var sw = Stopwatch.StartNew();
        
        DynamicTree.instance.UpdateTree();

        sw.Stop();
        updateTree.Add(sw.Elapsed.TotalMilliseconds);
        sw.Restart();

        DynamicTree.instance.GetPairs(ref pairs);

        sw.Stop();
        broad.Add(sw.Elapsed.TotalMilliseconds);
        pairCount.Add(pairs.Count);
        sw.Restart();

        hitResults.Clear();

        foreach ((int, int) pair in pairs)
        {
            if (DynamicTree.instance.GJK(pair))
            {
                HitInfo info = DynamicTree.instance.EPA(pair);

                if (info.depth != float.MaxValue)
                    hitResults.Add(info);
            }
        }

        foreach (HitInfo hit in hitResults)
        {
            RigidBody body1 = DynamicTree.instance.colliders[hit.colliderA].attachedBody;
            RigidBody body2 = DynamicTree.instance.colliders[hit.colliderB].attachedBody;

            ResolveCollision(body1, body2, hit);
        }
        sw.Stop();
        narrow.Add(sw.Elapsed.TotalMilliseconds);
    }
}
