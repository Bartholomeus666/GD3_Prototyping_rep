using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement_2 : MonoBehaviour
{
    [Header("Input")]
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction sprintAction;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f;
    public float acceleration = 10f;
    public float deceleration = 12f;

    [Header("Jumping")]
    public float jumpForce = 5f;
    public float gravity = -20f;

    [Header("Rotation")]
    public float rotationSpeed = 12f;

    [Header("References")]
    public Transform cameraTransform;

    private CharacterController _controller;
    private Vector3 _velocity;
    private Vector3 _moveVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        sprintAction.Enable();

        jumpAction.started += OnJump;
    }

    private void OnDisable()
    {
        jumpAction.started -= OnJump;

        moveAction.Disable();
        jumpAction.Disable();
        sprintAction.Disable();
    }

    // ── Input callbacks ────────────────────────────────────────────────────────

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!_controller.isGrounded) return;
        _velocity.y = jumpForce;
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        ApplyGravity();
        HandleMovement();

        _controller.Move((_moveVelocity + _velocity) * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;
        else
            _velocity.y += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();

        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        float targetSpeed = input.sqrMagnitude > 0f
            ? (sprintAction.IsPressed() ? sprintSpeed : walkSpeed)
            : 0f;

        float smoothing = input.sqrMagnitude > 0f ? acceleration : deceleration;
        _moveVelocity = Vector3.MoveTowards(_moveVelocity, moveDirection * targetSpeed, smoothing * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
