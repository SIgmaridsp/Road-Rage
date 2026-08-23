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
        // Random palette
        if (palettes != null && palettes.Length > 0)
        {
            var smr = GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr != null)
                smr.material = palettes[Random.Range(0, palettes.Length)];
        }

        // Random scale ±15% so the crowd looks less cloned
        float s = Random.Range(0.85f, 1.15f);
        transform.localScale = new Vector3(s, s, s);
    }
}
