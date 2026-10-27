using UnityEngine;

public class RigidBody : MonoBehaviour
{
    [HideInInspector]
    public Vector3 velocity;
    [HideInInspector]
    public Vector3 force;

    public float mass;
    [HideInInspector]
    public float invMass;
    [Range(0f,1f)]
    public float bounciness;

    private void Awake()
    {
        invMass = mass == 0 ? 0 : 1 / mass;
    }

    public void ApplyForce(float dt) 
    {
        velocity += dt * invMass * force;
        force = Vector3.zero;
    }
    public void AddForce(Vector3 force) { this.force += force; }

    public void AddImpulse(Vector3 force) { velocity += force; }
}
