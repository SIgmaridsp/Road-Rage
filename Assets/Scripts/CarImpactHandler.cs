using UnityEngine;

/// <summary>
/// Lives on the car alongside ArcadeCarController. Detects collisions with
/// anything carrying a RagdollController, then launches it and spawns effects.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarImpactHandler : MonoBehaviour
{
    [Header("Impact")]
    [Tooltip("Scales car momentum into ragdoll launch force. Tune to taste.")]
    [SerializeField] private float forceMultiplier = 80f;

    [Tooltip("Minimum car speed (m/s) before a hit registers. Stops gentle nudges from launching people.")]
    [SerializeField] private float minImpactSpeed = 3f;

    [Header("Effects")]
    [Tooltip("Burst particle system prefab spawned at the contact point (debris/shrapnel).")]
    [SerializeField] private ParticleSystem impactEffectPrefab;

    [SerializeField] private float hitStopDuration = 0.05f; // brief freeze for 'crunch' feel

    private Rigidbody rb;

    void Awake() => rb = GetComponent<Rigidbody>();

    void OnCollisionEnter(Collision collision)
    {
        // GetComponentInParent because we may hit a bone collider, not the root.
        var ragdoll = collision.collider.GetComponentInParent<RagdollController>();
        if (ragdoll == null || ragdoll.IsRagdoll) return;

        float speed = rb.linearVelocity.magnitude; // 2022/2023 LTS: rb.velocity
        if (speed < minImpactSpeed) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 force = rb.linearVelocity.normalized * speed * forceMultiplier;

        ragdoll.Hit(force, contact.point);
        SpawnEffect(contact.point, contact.normal);

        if (hitStopDuration > 0f) StartCoroutine(HitStop());
    }

    private void SpawnEffect(Vector3 point, Vector3 normal)
    {
        if (impactEffectPrefab == null) return;
        var fx = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));
        fx.Play();
        Destroy(fx.gameObject, 3f);
        // CameraShake.Instance?.Shake(0.3f); // wire this to your camera shaker
    }

    private System.Collections.IEnumerator HitStop()
    {
        float original = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = original;
    }
}
