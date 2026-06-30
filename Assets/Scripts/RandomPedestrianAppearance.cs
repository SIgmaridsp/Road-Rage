using UnityEngine;

/// <summary>
/// Randomises the palette material on the NPC's SkinnedMeshRenderer every time
/// it is (re-)spawned from the pool. Drag the 9 palette materials from
/// Assets/DavidJalbert/LowPolyPeople/FBX/Materials/ into the Palettes array.
/// </summary>
public class RandomPedestrianAppearance : MonoBehaviour
{
    [SerializeField] private Material[] palettes;

    // OnEnable is called each time the pooled object is activated.
    void OnEnable()
    {
        if (palettes == null || palettes.Length == 0) return;
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null) return;
        smr.material = palettes[Random.Range(0, palettes.Length)];
    }
}
