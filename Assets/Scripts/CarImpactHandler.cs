using UnityEngine;

/// <summary>
/// Lives on the car alongside the car controller. Detects collisions with
/// anything carrying a RagdollController, then launches it and spawns effects.
/// (Hit-stop time-freeze removed — it was causing the game to freeze.)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarImpactHandler : MonoBehaviour
{
    [Header("Impact")]
    [Tooltip("Scales car momentum into ragdoll launch force. Tune to taste.")]
    [SerializeField] private float forceMultiplier = 4f;

    [Tooltip("Minimum car speed (m/s) before a hit registers.")]
    [SerializeField] private float minImpactSpeed = 1f;

    [Header("Effects")]
    [Tooltip("Optional burst particle prefab spawned at the contact point.")]
    [SerializeField] private ParticleSystem impactEffectPrefab;

    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnCollisionEnter(Collision collision)
    {
        var ragdoll = collision.collider.GetComponentInParent<RagdollController>();
        if (ragdoll == null || ragdoll.IsRagdoll) return;

        float speed = rb.linearVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 force = rb.linearVelocity.normalized * speed * forceMultiplier;

        ragdoll.Hit(force, contact.point);
        SpawnEffect(contact.point, contact.normal);
    }

    private void SpawnEffect(Vector3 point, Vector3 normal)
    {
        if (impactEffectPrefab == null) return;
        var fx = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));
        fx.Play();
        Destroy(fx.gameObject, 3f);
    }
}