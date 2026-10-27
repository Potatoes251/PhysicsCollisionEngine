using System.Collections.Generic;
using UnityEngine;

public class PhysicsManager : MonoBehaviour
{
    DynamicTree dynamicTree;

    [SerializeField]
    private Vector3 gravity = new(0f, -9.81f, 0f);

    private void FixedUpdate()
    {
        foreach (BaseCollider collider in dynamicTree.colliders)
        {
            RigidBody b = collider.attachedBody;
            if (b == null) continue;

            b.AddForce(gravity * b.mass);
        }

        List<(int x, int y)> pairs = dynamicTree.GetPairs();

        foreach ((int x, int y) in pairs)
        {

        }
    }
}
