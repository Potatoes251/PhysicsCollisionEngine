using System.Collections.Generic;
using UnityEngine;
using static DynamicTree;

public class PhysicsManager : MonoBehaviour
{
    [SerializeField]
    DynamicTree dynamicTree;

    [SerializeField]
    private Vector3 gravity = new(0f, -9.81f, 0f);

    private void ResolveCollision(RigidBody body1, RigidBody body2, HitInfo hitInfo)
    {
        float sumInvMass = (body1.invMass + body2.invMass);

        if (sumInvMass == 0f) return;

        float bounciness = (body1.bounciness + body2.bounciness) * .5f;
        float dot = Vector3.Dot((body2.velocity - body1.velocity), hitInfo.normal);

        if (dot < 0f)
        {
            float impulse = -(1 + bounciness) * dot / sumInvMass;

            body1.AddImpulse(-impulse * body1.invMass * hitInfo.normal);
            body2.AddImpulse(impulse * body2.invMass * hitInfo.normal);
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

            b.AddForce(gravity * b.mass);
        }

        foreach (BaseCollider collider in dynamicTree.colliders)
        {
            RigidBody b = collider.attachedBody;

            if (b == null || b.invMass == 0) continue;

            b.ApplyForce(dt);
            b.transform.position += b.velocity * dt;
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
