using UnityEngine;

/// <summary>
/// Simple, punchy arcade car. No WheelColliders — it's a Rigidbody pushed
/// around with forces, which is much easier to tune for a "road rage" feel
/// and ideal for mowing through crowds. Put this on the car body, which
/// needs a Rigidbody and a Collider.
///
/// Assumes a roughly flat drivable surface. For hills/suspension you'd add
/// ground-raycasts, but this is plenty for a city map.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ArcadeCarController : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] private float motorForce   = 4000f;
    [SerializeField] private float reverseForce = 2000f;
    [SerializeField] private float maxSpeed     = 40f;   // m/s (~144 km/h)

    [Header("Steering")]
    [SerializeField] private float turnSpeed = 120f;     // deg/sec at full grip
    [SerializeField] private float grip      = 6f;       // kills sideways slide

    [Header("Stability")]
    [SerializeField] private float downForce        = 100f;
    [SerializeField] private float centerOfMassDrop = 0.6f; // lower COM = fewer flips

    private Rigidbody rb;
    private float throttle; // -1..1
    private float steer;    // -1..1

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += Vector3.down * centerOfMassDrop;
    }

    void Update()
    {
        // Legacy Input Manager. Swap these two lines for the new Input System if you use it.
        throttle = Input.GetAxis("Vertical");
        steer    = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        Drive();
        Steer();
        KillSideSlip();
        rb.AddForce(-transform.up * downForce);
    }

    // Unity 6+: linearVelocity. On 2022/2023 LTS or older use rb.velocity instead.
    private float ForwardSpeed => Vector3.Dot(rb.linearVelocity, transform.forward);

    private void Drive()
    {
        if (Mathf.Abs(throttle) < 0.01f) return;
        if (Mathf.Abs(ForwardSpeed) >= maxSpeed) return;

        float force = throttle > 0 ? motorForce : -reverseForce;
        rb.AddForce(transform.forward * force * Mathf.Abs(throttle));
    }

    private void Steer()
    {
        // Scale turn rate with speed so it feels grounded, and reverse it when backing up.
        float speedFactor = Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / 5f);
        float dir = ForwardSpeed < -0.1f ? -1f : 1f;
        float yaw = steer * turnSpeed * speedFactor * dir * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, yaw, 0f));
    }

    private void KillSideSlip()
    {
        Vector3 sideways = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
        rb.AddForce(-sideways * grip, ForceMode.Acceleration);
    }
}
