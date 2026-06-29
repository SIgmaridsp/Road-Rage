using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Makes an NPC stroll the NavMesh, pausing now and then so the crowd reads as
/// people living their lives rather than robots on rails. Walk speed / radius
/// come from the prefab — a "cyclist" prefab is just this with a bike mesh, a
/// faster NavMeshAgent.speed, and a bigger wanderRadius.
///
/// Put on the NPC root alongside NavMeshAgent + RagdollController.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class NPCWander : MonoBehaviour
{
    [SerializeField] private float wanderRadius = 25f;
    [Tooltip("Random pause range (seconds) when an NPC reaches a spot.")]
    [SerializeField] private Vector2 idlePause = new Vector2(0f, 4f);

    private NavMeshAgent agent;
    private RagdollController ragdoll;
    private float waitTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        ragdoll = GetComponent<RagdollController>();
    }

    void OnEnable() => waitTimer = 0f; // fresh start each time it's pulled from the pool

    void Update()
    {
        if (ragdoll != null && ragdoll.IsRagdoll)
        {
            if (agent.enabled) agent.enabled = false; // stop steering a corpse
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh || agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                agent.SetDestination(NextPoint());
                waitTimer = Random.Range(idlePause.x, idlePause.y);
            }
        }
    }

    private Vector3 NextPoint()
    {
        Vector3 candidate = transform.position + Random.insideUnitSphere * wanderRadius;
        if (NavMesh.SamplePosition(candidate, out var hit, wanderRadius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }
}
