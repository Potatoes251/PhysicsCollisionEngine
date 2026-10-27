using System.Collections.Generic;
using UnityEngine;
using static DynamicTree;
using static UnityEngine.PlayerLoop.PreUpdate;

public class PhysicsManager : MonoBehaviour
{
    [SerializeField]
    DynamicTree dynamicTree;

    [SerializeField]
    private Vector3 gravity = new(0f, -9.81f, 0f);

    [SerializeField]
    private bool enableGravity = true;
    [SerializeField]
    private bool enableDamping = true;

    private void ResolveCollision(RigidBody body1, RigidBody body2, HitInfo hitInfo)
    {
        float sumInvMass = (body1.invMass + body2.invMass);

        if (sumInvMass == 0f) return;

        float dot = Vector3.Dot((body2.velocity - body1.velocity), hitInfo.normal);

        if (dot < 0f)
        {
            Vector3 r1xn = Vector3.Cross(hitInfo.hitPoint - body1.transform.position, hitInfo.normal);
            Vector3 r2xn = Vector3.Cross(hitInfo.hitPoint - body2.transform.position, hitInfo.normal);
            float k = sumInvMass
                    + Vector3.Dot(r1xn, body1.ApplyInvInertia(r1xn))
                    + Vector3.Dot(r2xn, body2.ApplyInvInertia(r2xn));
            if (k == 0f) return;
            
            float bounciness = (body1.bounciness + body2.bounciness) * .5f;
                
            float impulse = -(1 + bounciness) * dot / k;

            body1.AddImpulse(-impulse * hitInfo.normal, hitInfo.hitPoint - body1.transform.position);
            body2.AddImpulse(impulse * hitInfo.normal, hitInfo.hitPoint - body2.transform.position);
        }

        float correction = Mathf.Max(hitInfo.depth, 0f) / sumInvMass;
        body1.transform.position -= correction * body1.invMass * hitInfo.normal;
        body2.transform.position += correction * body2.invMass * hitInfo.normal;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        foreach (BaseCollider collider in dynamicTree.colliders)
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

        dynamicTree.UpdateTree();

        List<(int x, int y)> pairs = dynamicTree.GetPairs();

        foreach ((int, int) pair in pairs)
        {
            if (dynamicTree.GJK(pair))
            {
                HitInfo info = dynamicTree.EPA(pair);

                (int x, int y) = pair;

                RigidBody body1 = dynamicTree.colliders[x].attachedBody;
                RigidBody body2 = dynamicTree.colliders[y].attachedBody;

                ResolveCollision(body1, body2, info);
            }
        }
    }
}
