using UnityEngine;

/// <summary>
/// Follows a target and lets you orbit the view by dragging the mouse (hold the
/// RIGHT mouse button by default), with scroll-wheel zoom. Replaces CameraFollow
/// when you want to look around instead of a fixed angle.
///
/// Put on Main Camera, assign Target = the car (Cube).
/// </summary>
public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 12f;
    [SerializeField] private float height = 4f;

    [Header("Look")]
    [SerializeField] private float yawSpeed = 4f;
    [SerializeField] private float pitchSpeed = 2.5f;
    [SerializeField] private float minPitch = -10f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Drag button: 0=Left 1=Right 2=Middle")]
    [SerializeField] private int dragButton = 1;

    private float yaw;
    private float pitch = 20f;

    void Start()
    {
        if (target != null) yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Drag to look.
        if (Input.GetMouseButton(dragButton))
        {
            yaw   += Input.GetAxis("Mouse X") * yawSpeed;
            pitch -= Input.GetAxis("Mouse Y") * pitchSpeed;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Scroll to zoom.
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 10f;
        distance  = Mathf.Clamp(distance, minDistance, maxDistance);

        // Orbit position around the target.
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus = target.position + Vector3.up * height * 0.5f;
        transform.position = focus - rot * Vector3.forward * distance + Vector3.up * height;
        transform.LookAt(focus);
    }
}
