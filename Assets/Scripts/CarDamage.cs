using UnityEngine;

/// <summary>
/// Damages the car when it slams into solid things (walls, other cars) at speed.
/// Pedestrians never hurt you — that's the point of the game. Put this on the car
/// alongside Health.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Health))]
public class CarDamage : MonoBehaviour
{
    [SerializeField] private float minDamageSpeed = 8f;  // m/s before a crash hurts
    [SerializeField] private float damagePerSpeed = 1.5f;

    private Health health;

    void Awake() => health = GetComponent<Health>();

    void OnCollisionEnter(Collision collision)
    {
        // Anything with a ragdoll is a pedestrian -> free, no damage.
        if (collision.collider.GetComponentInParent<RagdollController>() != null) return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < minDamageSpeed) return;

        health.TakeDamage((impact - minDamageSpeed) * damagePerSpeed);
    }
}
