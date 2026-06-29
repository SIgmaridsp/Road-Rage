using UnityEngine;

/// <summary>
/// TEMPORARY diagnostic. Put this on the Cube. Press Play, click the Game view,
/// and hold W or arrow keys. Watch the Console:
///   - If you see "INPUT: ..." lines with non-zero numbers -> input IS working,
///     the problem is in the car controller / physics.
///   - If the numbers stay 0 (or no lines appear) -> the legacy Input axes are
///     not registered. We fix that in Project Settings.
/// Also nudges the object directly with the transform so we can SEE if keys reach it
/// at all, bypassing Rigidbody physics entirely.
/// </summary>
public class InputTest : MonoBehaviour
{
    void Update()
    {
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(v) > 0.01f || Mathf.Abs(h) > 0.01f)
        {
            Debug.Log("INPUT WORKS  ->  Vertical: " + v + "   Horizontal: " + h);
            // Move the object directly, no physics, so you can SEE keys reach it.
            transform.position += new Vector3(h, 0f, v) * 10f * Time.deltaTime;
        }
    }
}
