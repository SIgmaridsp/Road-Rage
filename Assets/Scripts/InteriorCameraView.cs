using UnityEngine;

/// <summary>
/// Press F while driving to toggle first-person cockpit view.
/// Disables the exterior follow-cam and snaps the main camera
/// inside the cab. Press F again to return to third-person.
///
/// Attach to the car GameObject alongside CarControllerFIXED.
/// </summary>
public class InteriorCameraView : MonoBehaviour
{
    [Header("Cockpit camera position (local to car)")]
    [Tooltip("Where the camera sits inside the cab — adjust per-model in the Inspector")]
    [SerializeField] private Vector3 seatOffset   = new Vector3(0.25f, 2.1f, 0.55f);
    [Tooltip("Extra pitch/yaw/roll applied to the interior camera")]
    [SerializeField] private Vector3 seatRotation = new Vector3(8f, 0f, 0f);

    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    private Camera     mainCam;
    private MonoBehaviour exteriorCamScript;
    private bool       isInterior;

    void Start()
    {
        mainCam = Camera.main;
        if (mainCam == null) return;

        // Support either FollowCamFIXED or CameraFollow on the main camera
        exteriorCamScript = (MonoBehaviour)mainCam.GetComponent<FollowCamFIXED>()
                         ?? (MonoBehaviour)mainCam.GetComponent<CameraFollow>();
    }

    void Update()
    {
        // Only toggle when the car controller is active (i.e. player is driving)
        var controller = GetComponent<CarControllerFIXED>();
        if (controller != null && !controller.enabled) return;

        if (!Input.GetKeyDown(toggleKey)) return;

        isInterior = !isInterior;

        if (exteriorCamScript != null)
            exteriorCamScript.enabled = !isInterior;
    }

    void LateUpdate()
    {
        if (!isInterior || mainCam == null) return;

        // World-space driver seat position
        mainCam.transform.position = transform.TransformPoint(seatOffset);
        // Look straight ahead along the car's forward axis with a slight downward tilt
        mainCam.transform.rotation = transform.rotation * Quaternion.Euler(seatRotation);
    }

    // Called externally when the player exits the car so we cleanly restore
    // the exterior camera regardless of which view was active.
    public void ForceExteriorView()
    {
        isInterior = false;
        if (exteriorCamScript != null)
            exteriorCamScript.enabled = true;
    }
}
