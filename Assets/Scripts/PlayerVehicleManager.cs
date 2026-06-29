using UnityEngine;

/// <summary>
/// Hands control back and forth between the on-foot player and the car.
/// Press E near the car to get in; press E again to get out. While driving, the
/// player body is hidden and the car's controller is active; on foot, vice-versa.
///
/// Put this on its OWN empty GameObject (NOT on the player — it deactivates the
/// player object, which would also disable a script living on it).
/// </summary>
public class PlayerVehicleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ArcadeCarController car;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Settings")]
    [SerializeField] private float enterRange = 3.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool startInCar = false;

    private bool isDriving;

    void Start()
    {
        if (startInCar) EnterCar();
        else GoOnFoot();
    }

    void Update()
    {
        if (!Input.GetKeyDown(interactKey)) return;

        if (isDriving) ExitCar();
        else if (PlayerNearCar()) EnterCar();
    }

    private bool PlayerNearCar()
    {
        return player != null && car != null &&
               Vector3.Distance(player.transform.position, car.transform.position) <= enterRange;
    }

    private void GoOnFoot()
    {
        isDriving = false;
        if (car != null) car.enabled = false;
        if (player != null) player.gameObject.SetActive(true);
        if (cameraFollow != null && player != null) cameraFollow.SetTarget(player.transform);
    }

    private void EnterCar()
    {
        isDriving = true;
        if (player != null) player.gameObject.SetActive(false); // park the body
        if (car != null) car.enabled = true;
        if (cameraFollow != null && car != null) cameraFollow.SetTarget(car.transform);
    }

    private void ExitCar()
    {
        isDriving = false;
        if (car != null) car.enabled = false;

        if (player != null && car != null)
        {
            // Drop the player out to the left of the car.
            player.transform.position = car.transform.position
                                      - car.transform.right * 2.2f
                                      + Vector3.up * 0.5f;
            player.gameObject.SetActive(true);
        }

        if (cameraFollow != null && player != null) cameraFollow.SetTarget(player.transform);
    }
}
