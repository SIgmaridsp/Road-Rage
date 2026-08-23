using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class CarControllerFIXED : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] private float acceleration = 89f;
    [SerializeField] private float maxSpeed     = 25f;
    [SerializeField] private float turnSmooth   = 12f;

    [Header("Grip / Feel")]
    [Range(0f,1f)]
    [SerializeField] private float grip = 0.72f;
    [SerializeField] private float centerOfMassDrop = 0.6f;
    [SerializeField] private float downForce = 100f;
    [SerializeField] private float groundDrag = 3f;

    [Header("Drift")]
    [Tooltip("Grip at full drift (lower = more slide)")]
    [Range(0f,1f)]
    [SerializeField] private float driftGrip = 0.05f;
    [Tooltip("Steering input (0-1) required to break traction and start drifting")]
    [Range(0f,1f)]
    [SerializeField] private float driftThreshold = 0.7f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    private Rigidbody rb;
    private float throttle;
    private float steer;
    private bool jumpQueued;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += Vector3.down * centerOfMassDrop;
    }

    void Update()
    {
        throttle = Input.GetAxis("Vertical");
        steer    = Input.GetAxis("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space))
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        // Flatten car axes onto XZ so a pitched car doesn't inject Y into driving forces
        Vector3 flatFwd   = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 flatRight = new Vector3(transform.right.x,   0f, transform.right.z  ).normalized;

        // Apply ground drag when on the ground, zero when airborne
        bool grounded = Physics.Raycast(transform.position, Vector3.down, 1.2f);
        rb.linearDamping = grounded ? groundDrag : 0f;

        float speed = rb.linearVelocity.magnitude;

        if (Mathf.Abs(throttle) > 0.01f && speed < maxSpeed)
        {
            rb.AddForce(flatFwd * throttle * acceleration * Time.fixedDeltaTime, ForceMode.VelocityChange);
            // Clamp forward speed immediately so VelocityChange can't overshoot maxSpeed
            float fwdSpeed = Vector3.Dot(rb.linearVelocity, flatFwd);
            if (Mathf.Abs(fwdSpeed) > maxSpeed)
                rb.linearVelocity -= flatFwd * (fwdSpeed - Mathf.Sign(fwdSpeed) * maxSpeed);
        }

        if (Mathf.Abs(steer) > 0.01f && speed > 0.5f)
        {
            float turn = steer * turnSmooth * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        // Drift: grip drops toward driftGrip when steering hard at speed,
        // letting the rear slide out into a drift
        float speedRatio    = Mathf.Clamp01(speed / maxSpeed);
        float driftBlend    = Mathf.Clamp01((Mathf.Abs(steer) - driftThreshold) / (1f - driftThreshold)) * speedRatio;
        float effectiveGrip = Mathf.Lerp(grip, driftGrip, driftBlend);

        // Reconstruct velocity using flat world axes so Y is never double-counted
        // when the car is pitched after a collision
        Vector3 forwardVel = flatFwd   * Vector3.Dot(rb.linearVelocity, flatFwd);
        Vector3 rightVel   = flatRight * Vector3.Dot(rb.linearVelocity, flatRight);
        float vertY = rb.linearVelocity.y > 0f ? rb.linearVelocity.y * 0.75f : rb.linearVelocity.y;
        rb.linearVelocity = forwardVel + rightVel * (1f - effectiveGrip) + Vector3.up * vertY;

        if (jumpQueued)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpQueued = false;
        }

        rb.AddForce(Vector3.down * downForce);
    }
}
