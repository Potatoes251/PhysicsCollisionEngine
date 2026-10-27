using UnityEngine;

public class RigidBody : MonoBehaviour
{
    [HideInInspector]
    public Vector3 velocity = new();
    private Vector3 force = new();
    [HideInInspector]
    public Vector3 angularVelocity = new();
    private Vector3 torque = new();

    public float mass;

    [HideInInspector]
    public float invMass;
    [Range(0f,1f)]
    public float bounciness;

    [SerializeField]
    private float linearDamping;
    [SerializeField]
    private float angularDamping;

    public Vector3 localInertia = Vector3.one;
    private Vector3 invLocalInertia;

    private void Awake()
    {
        if (mass == 0f)
        {
            invMass = 0f;
            invLocalInertia = Vector3.zero;
        }
        else
        {
            invMass = 1f / mass;
            invLocalInertia = new Vector3(1f / localInertia.x, 1f / localInertia.y, 1f / localInertia.z);
        }
    }

    public void ApplyDamping()
    {
        if (linearDamping > 0)
        {
            velocity *= (1 - linearDamping * Time.fixedDeltaTime);
        }
        if (angularDamping > 1)
        {
            angularVelocity = Vector3.zero;
        }
        else if (angularDamping > 0)
        {
            angularVelocity -= angularDamping * angularVelocity;
        }
    }

    public void ApplyForce(float dt) 
    {
        velocity += dt * invMass * force;
        force = Vector3.zero;
        angularVelocity += dt * ApplyInvInertia(torque);
        torque = Vector3.zero;
    }

    public Vector3 ApplyInvInertia(Vector3 v)
    {
        Quaternion q = transform.rotation;
        Vector3 local = Vector3.Scale(Quaternion.Inverse(q) * v, invLocalInertia);
        return q * local;
    }

    public void AddForce(Vector3 force) { this.force += force; }

    public void AddForce(Vector3 force, Vector3 worldPoint) 
    { 
        this.force += force;
        torque += Vector3.Cross(worldPoint - transform.position, force);
    }

    public void AddImpulse(Vector3 force) { velocity += force; }

    public void AddImpulse(Vector3 force, Vector3 r) // r = contact - center
    { 
        velocity += force;
        torque += ApplyInvInertia(Vector3.Cross(r, force));
    }

    public void IntegrateTransform(float dt)
    {
        transform.position += velocity * dt;

        float angle = angularVelocity.magnitude * dt;
        if (angle > 1e-6f)
        {
            Quaternion dq = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, angularVelocity.normalized);
            transform.rotation = dq * transform.rotation;
        }
    }
}
