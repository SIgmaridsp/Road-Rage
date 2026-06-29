using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Recycles NPCs instead of Instantiate/Destroy (ragdolls are heavy).
/// Supports MULTIPLE prefab types (pedestrian, cyclist, etc.) — each prefab
/// gets its own internal queue, and Despawn returns instances to the right one.
/// </summary>
public class NPCPool : MonoBehaviour
{
    public static NPCPool Instance { get; private set; }

    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> sourceOf = new(); // instance -> prefab

    void Awake() => Instance = this;

    /// <summary>Optional: pre-build some instances so the first spawns don't hitch.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        var q = GetQueue(prefab);
        for (int i = 0; i < count; i++) q.Enqueue(CreateInactive(prefab));
    }

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var q = GetQueue(prefab);
        GameObject npc = q.Count > 0 ? q.Dequeue() : CreateInactive(prefab);

        npc.transform.SetPositionAndRotation(position, rotation);
        npc.SetActive(true);

        npc.GetComponent<RagdollController>()?.Revive();

        var agent = npc.GetComponent<NavMeshAgent>();
        if (agent != null) { agent.enabled = true; agent.Warp(position); }

        npc.GetComponent<Health>()?.ResetHealth();
        return npc;
    }

    public void Despawn(GameObject npc)
    {
        npc.SetActive(false);
        if (sourceOf.TryGetValue(npc, out var prefab)) GetQueue(prefab).Enqueue(npc);
        else Destroy(npc);
    }

    private Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out var q)) { q = new(); pools[prefab] = q; }
        return q;
    }

    private GameObject CreateInactive(GameObject prefab)
    {
        var go = Instantiate(prefab, transform);
        sourceOf[go] = prefab;
        go.SetActive(false);
        return go;
    }
}
