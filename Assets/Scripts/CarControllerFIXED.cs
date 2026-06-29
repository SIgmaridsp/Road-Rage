using UnityEngine;

/// <summary>
/// THE CORRECT ONE. World-relative arcade car: WASD always points the SAME world
/// direction no matter which way the car faces. W = forward, S = back, A/D = strafe.
/// The car body rotates to face wherever it's driving.
///
/// Put on the car. Needs a Rigidbody and a Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CarControllerFIXED : MonoBehaviour
{
    [Header("Drive")]
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float maxSpeed     = 25f;
    [SerializeField] private float turnSmooth   = 12f;

    [Header("Stability")]
    [SerializeField] private float downForce        = 100f;
    [SerializeField] private float centerOfMassDrop = 0.6f;
    [SerializeField] private float groundDrag       = 3f;

    private Rigidbody rb;
    private Vector3 input;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += Vector3.down * centerOfMassDrop;
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        // Fixed WORLD directions — never relative to the car's facing.
        input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude > 1f) input.Normalize();
    }

    void FixedUpdate()
    {
        if (input.sqrMagnitude > 0.01f)
        {
            rb.AddForce(input * acceleration, ForceMode.Acceleration);

            Quaternion targetRot = Quaternion.LookRotation(input, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, turnSmooth * Time.fixedDeltaTime));
        }

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.magnitude > maxSpeed)
        {
            flat = flat.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(flat.x, rb.linearVelocity.y, flat.z);
        }

        if (input.sqrMagnitude < 0.01f)
        {
            Vector3 slow = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(-slow * groundDrag, ForceMode.Acceleration);
        }

        rb.AddForce(Vector3.down * downForce);
    }
}