using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Offset")]
    public Vector3 offset = new Vector3(0f, 2f, -4f);

    [Header("Rotation Settings")]
    public float lookSpeed = 120f;
    public float pitchMin = -40f;
    public float pitchMax = 80f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public float minZoom = 2f;
    public float maxZoom = 6f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;
    public float collisionSmoothTime = 0.05f;

    private float yaw;
    private float pitch;

    private float currentZoom;
    private float targetZoom;

    private Vector3 smoothVelocity;

    private PlayerInputActions inputActions;
    private Vector2 lookInput;


    private void Awake()
    {
        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
    }

    private void Start()
    {
        currentZoom = offset.magnitude;
        targetZoom = currentZoom;

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        ReadLookInput();
        HandleZoom();
        UpdateCameraPosition();
    }

    private void ReadLookInput()
    {
        lookInput = inputActions.Player.Look.ReadValue<Vector2>();

        yaw += lookInput.x * lookSpeed * Time.deltaTime;
        pitch -= lookInput.y * lookSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetZoom -= scroll * zoomSpeed * Time.deltaTime;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * 10f);
    }


    private void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 direction = rotation * offset.normalized;
        float distance = currentZoom;

        Vector3 desiredPosition = target.position + direction * distance;

        if (Physics.SphereCast(target.position, collisionRadius, direction, out RaycastHit hit, distance, collisionLayers))
        {
            desiredPosition = target.position + direction * (hit.distance - collisionRadius);
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, collisionSmoothTime);
        transform.LookAt(target.position + Vector3.up * offset.y * 0.5f);
    }


    public Vector3 GetMoveDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f)
            return Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }

    public Quaternion GetPlanarRotation()
    {
        return Quaternion.Euler(0f, yaw, 0f);
    }
}
