using UnityEngine;

/// <summary>
/// A stable chase camera that follows a target at a fixed world-space offset and
/// looks at it. Call SetTarget to swap between the player and the car. The fixed
/// angle is what keeps camera-relative WASD predictable on foot.
///
/// Put on Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 8f, -10f);
    [SerializeField] private float followLerp = 8f;
    [SerializeField] private float lookHeight = 1.2f;

    public void SetTarget(Transform t) => target = t;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followLerp * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookHeight);
    }
}
