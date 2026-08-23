using UnityEngine;

/// <summary>
/// Visually tilts the car body based on acceleration and turning, giving a
/// spring-suspension bounce feel without WheelColliders. Put this on the
/// physics root alongside CarControllerFIXED — it auto-finds the first child
/// named "Monster Truck" and tilts it.
/// </summary>
public class CarBodyTilt : MonoBehaviour
{
    [Header("Tilt Limits (degrees)")]
    [SerializeField] private float pitchMax   = 4f;
    [SerializeField] private float rollMax    = 5f;
    [SerializeField] private float smoothTime = 0.12f;

    private Rigidbody rb;
    private Transform visual;
    private float pitchVel, rollVel;
    private float pitch, roll;
    private Vector3 prevVelocity;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        foreach (Transform child in transform)
        {
            if (child.name.Contains("Monster Truck") || child.name.Contains("Truck"))
            {
                visual = child;
                break;
            }
        }
    }

    void Update()
    {
        if (rb == null || visual == null) return;

        // Derive acceleration from velocity delta
        Vector3 accel = (rb.linearVelocity - prevVelocity) / Mathf.Max(Time.deltaTime, 0.001f);
        prevVelocity  = rb.linearVelocity;

        // Pitch back on acceleration, forward on braking
        float targetPitch = Mathf.Clamp(-Vector3.Dot(accel, transform.forward) * 0.35f, -pitchMax, pitchMax);
        // Roll away from the turn
        float targetRoll  = Mathf.Clamp(-Vector3.Dot(accel, transform.right)   * 0.28f, -rollMax,  rollMax);

        pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVel, smoothTime);
        roll  = Mathf.SmoothDamp(roll,  targetRoll,  ref rollVel,  smoothTime);

        visual.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }
}
