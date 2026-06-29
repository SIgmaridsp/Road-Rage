using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Populates the ENTIRE map with NPCs going about their lives — independent of
/// where the player is. Spawns them at uniformly-random points across the baked
/// NavMesh, mixes prefab types (walkers, cyclists, ...) by weight, keeps the
/// population steady, and recycles old corpses so the world never empties out.
///
/// Bake a NavMesh first (Window > AI > Navigation), then drop this in the scene.
/// </summary>
public class WorldPopulator : MonoBehaviour
{
    [System.Serializable]
    public struct NPCType
    {
        public GameObject prefab;
        [Tooltip("Relative chance this type is chosen. e.g. pedestrians 5, cyclists 1.")]
        public float weight;
    }

    [Header("Population")]
    [SerializeField] private NPCType[] npcTypes;
    [Tooltip("How many NPCs should be alive in the world at once.")]
    [SerializeField] private int targetPopulation = 120;

    [Header("Lifecycle")]
    [Tooltip("Seconds a ragdoll lies around before it's recycled and re-spawned elsewhere.")]
    [SerializeField] private float corpseLifetime = 12f;
    [SerializeField] private float tickInterval = 0.5f;
    [Tooltip("Max new NPCs per tick — ramps the crowd up smoothly instead of one big spike.")]
    [SerializeField] private int spawnsPerTick = 8;

    private readonly List<GameObject> active = new();
    private float timer;

    // Cached NavMesh triangulation for uniform random sampling across the whole map.
    private Vector3[] verts;
    private int[] indices;
    private float[] cumulativeArea;
    private float totalArea;

    void Start()
    {
        BuildNavMeshCache();

        // Pre-warm each type's pool a little so the first wave doesn't stutter.
        if (npcTypes != null)
            foreach (var t in npcTypes)
                if (t.prefab != null)
                    NPCPool.Instance.Prewarm(t.prefab, Mathf.CeilToInt(targetPopulation * 0.3f / npcTypes.Length));
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tickInterval;

        RecycleOldCorpses();
        TopUpPopulation();
    }

    private void RecycleOldCorpses()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var npc = active[i];
            if (npc == null) { active.RemoveAt(i); continue; }

            var rag = npc.GetComponent<RagdollController>();
            if (rag != null && rag.IsRagdoll && rag.RagdollElapsed > corpseLifetime)
            {
                NPCPool.Instance.Despawn(npc);
                active.RemoveAt(i);
            }
        }
    }

    private void TopUpPopulation()
    {
        int budget = spawnsPerTick;
        while (active.Count < targetPopulation && budget-- > 0)
        {
            if (!TryRandomPoint(out Vector3 pos)) break;
            var prefab = PickPrefab();
            if (prefab == null) break;

            var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            active.Add(NPCPool.Instance.Spawn(prefab, pos, rot));
        }
    }

    private GameObject PickPrefab()
    {
        if (npcTypes == null || npcTypes.Length == 0) return null;

        float total = 0f;
        foreach (var t in npcTypes) total += Mathf.Max(0f, t.weight);
        if (total <= 0f) return npcTypes[0].prefab;

        float r = Random.value * total;
        foreach (var t in npcTypes)
        {
            r -= Mathf.Max(0f, t.weight);
            if (r <= 0f) return t.prefab;
        }
        return npcTypes[npcTypes.Length - 1].prefab;
    }

    // --- Uniform random point over the entire baked NavMesh ---------------------

    private void BuildNavMeshCache()
    {
        var tri = NavMesh.CalculateTriangulation();
        verts = tri.vertices;
        indices = tri.indices;

        int triCount = indices.Length / 3;
        cumulativeArea = new float[triCount];
        totalArea = 0f;

        for (int i = 0; i < triCount; i++)
        {
            Vector3 a = verts[indices[i * 3]];
            Vector3 b = verts[indices[i * 3 + 1]];
            Vector3 c = verts[indices[i * 3 + 2]];
            totalArea += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            cumulativeArea[i] = totalArea;
        }

        if (triCount == 0)
            Debug.LogWarning("WorldPopulator: NavMesh has no triangles — did you bake it?");
    }

    private bool TryRandomPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (cumulativeArea == null || cumulativeArea.Length == 0) return false;

        // Pick a triangle weighted by its area (binary search the cumulative table).
        float r = Random.value * totalArea;
        int lo = 0, hi = cumulativeArea.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (cumulativeArea[mid] < r) lo = mid + 1; else hi = mid;
        }

        Vector3 va = verts[indices[lo * 3]];
        Vector3 vb = verts[indices[lo * 3 + 1]];
        Vector3 vc = verts[indices[lo * 3 + 2]];

        // Random barycentric point inside the triangle.
        float u = Random.value, v = Random.value;
        if (u + v > 1f) { u = 1f - u; v = 1f - v; }
        Vector3 candidate = va + u * (vb - va) + v * (vc - va);

        if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }
        return false;
    }
}
