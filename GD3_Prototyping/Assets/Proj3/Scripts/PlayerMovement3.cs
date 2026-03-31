using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerMovement3 : MonoBehaviour
{
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
    private InputAction _moveAction;
    //private InputAction _jumpAction;
    //private InputAction _sprintAction;
    private Vector3 _velocity;
    private Vector3 _moveVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        PlayerInput playerInput = GetComponent<PlayerInput>();
        _moveAction = playerInput.actions["Move"];
        //_jumpAction = playerInput.actions["Jump"];
        //_sprintAction = playerInput.actions["Sprint"];

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        //_jumpAction.started += OnJump;
    }

    private void OnDisable()
    {
        //_jumpAction.started -= OnJump;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!_controller.isGrounded) return;
        _velocity.y = jumpForce;
    }

    private void Update()
    {
        ApplyGravity();
        HandleMovement();
        cameraTransform.position = Vector3.Lerp(
            cameraTransform.position,
            new Vector3(transform.position.x, cameraTransform.position.y, transform.position.z),
            0.1f
        );
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
        Vector2 input = _moveAction.ReadValue<Vector2>();
        Vector3 camForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        Vector3 moveDirection = (camForward * input.y + camRight * input.x).normalized;

        //float targetSpeed = input.sqrMagnitude > 0f;
            //? (_sprintAction.IsPressed() ? sprintSpeed : walkSpeed)
            //: 0f;

        float smoothing = input.sqrMagnitude > 0f ? acceleration : deceleration;
        _moveVelocity = Vector3.MoveTowards(_moveVelocity, moveDirection * walkSpeed, smoothing * Time.deltaTime);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}