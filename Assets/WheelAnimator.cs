using UnityEngine;

/// <summary>
/// Spins the wheels based on car speed and turns the front wheels when steering.
/// Attach to the CAR (the Monster Truck root), assign the 4 tire transforms.
/// </summary>
public class WheelAnimator : MonoBehaviour
{
    [Header("Wheel Transforms")]
    [SerializeField] private Transform frontLeft;
    [SerializeField] private Transform frontRight;
    [SerializeField] private Transform backLeft;
    [SerializeField] private Transform backRight;

    [Header("Settings")]
    [SerializeField] private float spinSpeed = 400f;      // how fast wheels roll visually
    [SerializeField] private float maxSteerAngle = 25f;   // how far front wheels turn
    [SerializeField] private float steerSmooth = 8f;

    private Rigidbody carRb;
    private float currentSteer;

    void Start()
    {
        // Grab the rigidbody from the parent chassis (the Cube)
        carRb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        // How fast are we going? (used for spin amount)
        float speed = carRb != null ? carRb.linearVelocity.magnitude : 0f;
        float roll = speed * spinSpeed * Time.deltaTime * 0.1f;

        // Steering input for front-wheel turn
        float steerInput = Input.GetAxis("Horizontal");
        float targetSteer = steerInput * maxSteerAngle;
        currentSteer = Mathf.Lerp(currentSteer, targetSteer, steerSmooth * Time.deltaTime);

        // Spin all wheels (roll forward)
        if (frontLeft)  frontLeft.Rotate(roll, 0f, 0f, Space.Self);
        if (frontRight) frontRight.Rotate(roll, 0f, 0f, Space.Self);
        if (backLeft)   backLeft.Rotate(roll, 0f, 0f, Space.Self);
        if (backRight)  backRight.Rotate(roll, 0f, 0f, Space.Self);

        // Turn the FRONT wheels for steering (visual)
        if (frontLeft)  frontLeft.localRotation  = frontLeft.localRotation;
        // Note: steering turn applied via localEulerAngles Y below
        ApplySteer(frontLeft);
        ApplySteer(frontRight);
    }

    void ApplySteer(Transform wheel)
    {
        if (!wheel) return;
        Vector3 e = wheel.localEulerAngles;
        e.y = currentSteer;
        wheel.localEulerAngles = e;
    }
}