using UnityEngine;

public class RigidBody : MonoBehaviour
{
    Vector3 velocity;
    Vector3 force;

    public float mass;
    public float invMass;

    public void AddForce(Vector3 force) { this.force += force; }

    public void AddImpulse(Vector3 force) { velocity += force; }
}
