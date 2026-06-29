using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Switches a humanoid between "alive" (animated) and "ragdoll" (physics).
/// Put this on the NPC ROOT. Ragdoll bones (from the Ragdoll Wizard) are children.
///
/// Pooling-ready: Revive() resets a corpse to a clean walker, and RagdollElapsed
/// lets a spawner recycle bodies that have been lying around too long.
/// </summary>
[DisallowMultipleComponent]
public class RagdollController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [Tooltip("The capsule collider used while walking (the thing the car hits).")]
    [SerializeField] private Collider mainCollider;

    [Header("Launch Feel")]
    [SerializeField] private float upwardBonus = 0.15f;
    [Range(0f, 1f)]
    [SerializeField] private float bodyForceShare = 0.3f;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private float ragdollStartTime;

    public bool IsRagdoll { get; private set; }
    /// <summary>Seconds this body has been ragdolling (0 while alive).</summary>
    public float RagdollElapsed => IsRagdoll ? Time.time - ragdollStartTime : 0f;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        var bodies = new List<Rigidbody>();
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            if (rb.gameObject != gameObject) bodies.Add(rb);
        ragdollBodies = bodies.ToArray();

        var cols = new List<Collider>();
        foreach (var c in GetComponentsInChildren<Collider>())
            if (c != mainCollider && c.gameObject != gameObject) cols.Add(c);
        ragdollColliders = cols.ToArray();

        SetRagdoll(false);
    }

    private void SetRagdoll(bool state)
    {
        IsRagdoll = state;
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = !state;
            rb.interpolation = state ? RigidbodyInterpolation.Interpolate
                                     : RigidbodyInterpolation.None;
        }
        foreach (var col in ragdollColliders)
            col.enabled = state;

        if (animator != null) animator.enabled = !state;
        if (mainCollider != null) mainCollider.enabled = !state;
    }

    public void Hit(Vector3 force, Vector3 hitPoint)
    {
        if (IsRagdoll) return;
        SetRagdoll(true);
        ragdollStartTime = Time.time;

        Rigidbody closest = null;
        float best = float.MaxValue;
        foreach (var rb in ragdollBodies)
        {
            float d = (rb.worldCenterOfMass - hitPoint).sqrMagnitude;
            if (d < best) { best = d; closest = rb; }
        }

        Vector3 launch = force + Vector3.up * force.magnitude * upwardBonus;
        foreach (var rb in ragdollBodies)
        {
            float share = (rb == closest) ? 1f : bodyForceShare;
            rb.AddForceAtPosition(launch * share, hitPoint, ForceMode.Impulse);
        }
    }

    /// <summary>Reset back to a clean animated state (for object pooling).</summary>
    public void Revive()
    {
        if (IsRagdoll)
        {
            foreach (var rb in ragdollBodies)
            {
                rb.linearVelocity = Vector3.zero;   // 2022/2023 LTS: rb.velocity
                rb.angularVelocity = Vector3.zero;
            }
        }
        SetRagdoll(false);
    }
}
