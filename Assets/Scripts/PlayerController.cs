using UnityEngine;

/// <summary>
/// Walk/run a humanoid on foot with WASD, camera-relative. Uses a CharacterController
/// (not a Rigidbody) for crisp, predictable movement. Drives an Animator "Speed"
/// float: 0 = idle, 1 = walk, 2 = run.
///
/// Put on the player character root, alongside a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("References")]
    [Tooltip("Used so WASD is relative to the camera. Defaults to Camera.main.")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Animator animator;

    private CharacterController cc;
    private float verticalVel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift);

        // Flatten camera axes onto the ground so movement stays horizontal.
        Vector3 fwd   = cameraTransform ? cameraTransform.forward : Vector3.forward;
        Vector3 right = cameraTransform ? cameraTransform.right   : Vector3.right;
        fwd.y = 0f; right.y = 0f; fwd.Normalize(); right.Normalize();

        Vector3 move = fwd * v + right * h;
        float inputMag = Mathf.Clamp01(move.magnitude);
        if (inputMag > 0.001f) move.Normalize();

        float speed = sprint ? sprintSpeed : walkSpeed;
        Vector3 horizontal = move * speed * inputMag;

        // Face the way we're moving.
        if (inputMag > 0.05f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

        // Simple gravity so the CharacterController stays grounded.
        if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
        verticalVel += gravity * Time.deltaTime;

        cc.Move((horizontal + Vector3.up * verticalVel) * Time.deltaTime);

        if (animator != null) animator.SetFloat("Speed", inputMag * (sprint ? 2f : 1f));
    }
}
