using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class CarControllerFIXED : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] private float enginePower = 3000f;
    [SerializeField] private float maxSpeed    = 25f;
    [SerializeField] private float turnStrength = 80f;

    [Header("Grip / Feel")]
    [Range(0f,1f)]
    [SerializeField] private float grip = 0.9f;
    [SerializeField] private float centerOfMassDrop = 1.0f;
    [SerializeField] private float downForce = 20f;

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
        float speed = rb.linearVelocity.magnitude;

        if (Mathf.Abs(throttle) > 0.01f && speed < maxSpeed)
        {
            rb.AddForce(transform.forward * throttle * enginePower * Time.fixedDeltaTime, ForceMode.VelocityChange);
            // Clamp forward speed immediately so VelocityChange can't overshoot maxSpeed
            float fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            if (Mathf.Abs(fwdSpeed) > maxSpeed)
                rb.linearVelocity -= transform.forward * (fwdSpeed - Mathf.Sign(fwdSpeed) * maxSpeed);
        }

        if (Mathf.Abs(steer) > 0.01f && speed > 0.5f)
        {
            float turn = steer * turnStrength * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        Vector3 forwardVel = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 rightVel   = transform.right   * Vector3.Dot(rb.linearVelocity, transform.right);
        // Dampen upward velocity each tick so collision impulses don't launch the car
        float vertY = rb.linearVelocity.y > 0f ? rb.linearVelocity.y * 0.75f : rb.linearVelocity.y;
        rb.linearVelocity = forwardVel + rightVel * (1f - grip) + Vector3.up * vertY;

        if (jumpQueued)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpQueued = false;
        }

        rb.AddForce(Vector3.down * downForce, ForceMode.Acceleration);
    }
}