using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Stand Up")]
    [Tooltip("Seconds to stay limp on the ground before getting back up.")]
    [SerializeField] private float stayDownTime = 3f;
    [Tooltip("If checked, the NPC gets up by itself. Uncheck to stay down forever.")]
    [SerializeField] private bool autoStandUp = true;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Transform pelvis;
    private NavMeshAgent agent;
    private float ragdollStartTime;

    public bool IsRagdoll { get; private set; }
    public float RagdollElapsed => IsRagdoll ? Time.time - ragdollStartTime : 0f;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();

        var bodies = new List<Rigidbody>();
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            if (rb.gameObject != gameObject) bodies.Add(rb);
        ragdollBodies = bodies.ToArray();

        var cols = new List<Collider>();
        foreach (var c in GetComponentsInChildren<Collider>())
            if (c != mainCollider && c.gameObject != gameObject) cols.Add(c);
        ragdollColliders = cols.ToArray();

        float best = float.MaxValue;
        foreach (var rb in ragdollBodies)
        {
            float d = Vector3.SqrMagnitude(rb.transform.position - transform.position);
            if (d < best) { best = d; pelvis = rb.transform; }
        }

        SetRagdoll(false);
    }

    void Update()
    {
        if (IsRagdoll && autoStandUp && RagdollElapsed >= stayDownTime)
            StandUp();
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

        if (agent != null && agent.enabled) agent.enabled = false;

        // Tell the wander brain it was physically hit so it can react on recovery
        GetComponent<NPCWander>()?.OnHitByCar();

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

    public void StandUp()
    {
        if (!IsRagdoll) return;

        // Pelvis is mid-body — cast downward from it to find the actual ground
        Vector3 pelvisPos = pelvis != null ? pelvis.position : transform.position;

        foreach (var rb in ragdollBodies)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetRagdoll(false);

        Vector3 groundPos = FindGround(pelvisPos);
        transform.position = groundPos;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (agent != null)
        {
            agent.enabled = true;
            if (agent.isOnNavMesh) agent.Warp(groundPos);
        }

        // Let the NPCWander know we recovered so it can react
        GetComponent<NPCWander>()?.OnStoodUp();
    }

    public void Revive()
    {
        if (IsRagdoll)
        {
            foreach (var rb in ragdollBodies)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        SetRagdoll(false);

        // Snap to ground so pooled NPCs don't float
        Vector3 groundPos = FindGround(transform.position + Vector3.up * 2f);
        transform.position = groundPos;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        if (agent != null) agent.enabled = true;
    }

    /// Shoots a ray downward from <paramref name="from"/> to find the floor.
    /// Falls back to NavMesh sampling if nothing is hit.
    private static Vector3 FindGround(Vector3 from)
    {
        // Cast from a bit above in case 'from' is already inside geometry
        Vector3 origin = from + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 15f,
                            ~LayerMask.GetMask("Ignore Raycast")))
            return hit.point;

        if (NavMesh.SamplePosition(from, out NavMeshHit navHit, 5f, NavMesh.AllAreas))
            return navHit.position;

        return from;
    }
}