using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 3f;
    public float rotationSpeed = 10f;

    [Header("Jump / Gravity")]
    public float jumpHeight = 1.5f;
    public float gravity = -20f;

    [Header("Ground Check")]
    public float groundCheckRadius = 0.25f;
    public Vector3 groundCheckOffset = new Vector3(0, -0.1f, 0);
    public LayerMask groundLayer;

    [Header("Crouch")]
    public float crouchHeight = 1.0f;
    public Vector3 crouchCenter = new Vector3(0, 0.5f, 0);
    public float crouchTransitionSpeed = 8f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController controller;
    private PlayerInputActions input;

    private Vector2 moveInput;
    private float verticalVelocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    private float standHeight;
    private Vector3 standCenter;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        standHeight = controller.height;
        standCenter = controller.center;

        input = new PlayerInputActions();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += _ => moveInput = Vector2.zero;

        input.Player.Run.performed += _ => isSprinting = true;
        input.Player.Run.canceled += _ => isSprinting = false;

        input.Player.Crouch.performed += _ => HandleCrouchInput();
        input.Player.Jump.performed += _ => HandleJumpInput();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Update()
    {
        GroundCheck();
        HandleGravity();
        HandleMovement();
        HandleCollider();
    }

    private void HandleJumpInput()
    {
        bool grounded = isGrounded || controller.isGrounded;

        if (!grounded) return;

        if (isCrouching)
        {
            Debug.Log("Uncrouching via Jump!");
            isCrouching = false;
            return;
        }

        verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void HandleCrouchInput()
    {
        if (isCrouching)
        {
            isCrouching = false;
        }
        else
        {
            isCrouching = true;
            isSprinting = false;
        }
    }

    private void HandleMovement()
    {
        Vector3 moveDir = GetCameraRelativeDirection();

        float speed = isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed;
        Vector3 velocity = new Vector3( moveDir.x * speed, verticalVelocity, moveDir.z * speed);
        controller.Move(velocity * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }
        if (isCrouching && isSprinting)
            isCrouching = false;
    }

    private void HandleCollider()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        Vector3 targetCenter = isCrouching ? crouchCenter : standCenter;

        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        controller.center = Vector3.Lerp(controller.center, targetCenter, Time.deltaTime * crouchTransitionSpeed);
    }
    private void GroundCheck()
    {
        float bottomY = controller.center.y - (controller.height / 2f);
        Vector3 spherePosition = transform.TransformPoint(new Vector3(0, bottomY, 0));
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private void HandleGravity()
    {
        if (isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
            if (verticalVelocity < -20f) verticalVelocity = -20f;
        }
    }

    private Vector3 GetCameraRelativeDirection()
    {
        if (moveInput.sqrMagnitude < 0.01f) return Vector3.zero;
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0;
        right.y = 0;

        return (forward.normalized * moveInput.y + right.normalized * moveInput.x).normalized;
    }

    private void OnDrawGizmosSelected()
    {
        if (controller == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        float bottomY = controller.center.y - (controller.height / 2f);
        Vector3 pos = transform.TransformPoint(new Vector3(0, bottomY, 0));

        Gizmos.DrawWireSphere(pos, groundCheckRadius);
    }
}
