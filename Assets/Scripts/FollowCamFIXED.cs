using UnityEngine;

/// <summary>
/// Follows a target's POSITION but never its rotation, so the view doesn't flip
/// when the car turns. Hold RIGHT mouse and drag to orbit; scroll to zoom.
///
/// Put on Main Camera (NOT parented under the car). Assign Target = Cube.
/// </summary>
public class FollowCamFIXED : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float distance = 14f;
    [SerializeField] private float height   = 6f;

    [Header("Drag to look (right mouse)")]
    [SerializeField] private float yawSpeed   = 4f;
    [SerializeField] private float pitchSpeed = 2.5f;
    [SerializeField] private float minPitch   = 5f;
    [SerializeField] private float maxPitch   = 75f;

    [Header("Zoom (scroll)")]
    [SerializeField] private float zoomSpeed   = 5f;
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 30f;

    [Header("Smoothing")]
    [SerializeField] private float followSmooth = 10f;

    private float yaw;
    private float pitch = 25f;

    void LateUpdate()
    {
        if (target == null) return;

        // Right-drag to orbit. This yaw is the camera's OWN, independent of the car.
        if (Input.GetMouseButton(1))
        {
            yaw   += Input.GetAxis("Mouse X") * yawSpeed;
            pitch -= Input.GetAxis("Mouse Y") * pitchSpeed;
            pitch  = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Scroll to zoom.
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed * 10f;
        distance  = Mathf.Clamp(distance, minDistance, maxDistance);

        // Build a position around the target using OUR yaw/pitch (car rotation ignored).
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focus  = target.position + Vector3.up * (height * 0.3f);
        Vector3 desired = focus - rot * Vector3.forward * distance;

        transform.position = Vector3.Lerp(transform.position, desired, followSmooth * Time.deltaTime);
        transform.LookAt(focus);
    }
}