using UnityEngine;
using UnityEngine.InputSystem;

public class TelekinesisController : MonoBehaviour
{
    [Header("Input")]
    public InputAction holdAction;
    public InputAction throwAction;
    public InputAction pullAction;
    public InputAction pushAction;

    [Header("Selection")]
    public float selectionRange = 20f;
    public LayerMask throwableLayer;

    [Header("Floating")]
    public float followSpeed = 10f;
    public float minDistance = 1.5f;
    public float maxDistance = 15f;
    public float distanceStep = 1.5f;

    [Header("Throw")]
    public float throwForce = 40f;

    private Camera _cam;
    private Rigidbody _heldRb;
    private GameObject _heldObject;
    private float _currentDistance;

    // Always returns a ray from the exact center of the screen
    private Ray CrosshairRay => _cam.ScreenPointToRay(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

    private void Awake()
    {
        _cam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        holdAction.Enable();
        throwAction.Enable();
        pullAction.Enable();
        pushAction.Enable();

        holdAction.started += OnHoldStarted;
        holdAction.canceled += OnHoldCanceled;
        throwAction.started += OnThrow;
        pullAction.started += OnPull;
        pushAction.started += OnPush;
    }

    private void OnDisable()
    {
        holdAction.started -= OnHoldStarted;
        holdAction.canceled -= OnHoldCanceled;
        throwAction.started -= OnThrow;
        pullAction.started -= OnPull;
        pushAction.started -= OnPush;

        holdAction.Disable();
        throwAction.Disable();
        pullAction.Disable();
        pushAction.Disable();
    }


    private void OnHoldStarted(InputAction.CallbackContext ctx)
    {
        if (_heldObject != null) return;

        if (Physics.Raycast(CrosshairRay, out RaycastHit hit, selectionRange, throwableLayer))
        {
            _heldRb = hit.collider.GetComponent<Rigidbody>();
            if (_heldRb == null) return;

            _heldObject = hit.collider.gameObject;
            _currentDistance = Mathf.Clamp(hit.distance, minDistance, maxDistance);

            _heldRb.useGravity = false;
            _heldRb.linearDamping = 8f;
            _heldRb.angularDamping = 8f;
        }
    }

    private void OnHoldCanceled(InputAction.CallbackContext ctx) => DropObject();

    private void OnThrow(InputAction.CallbackContext ctx)
    {
        if (_heldObject == null) return;

        Vector3 throwDirection = CrosshairRay.direction;

        ReleaseObject();
        _heldRb.linearDamping = 0f;
        _heldRb.angularDamping = 0.05f;
        _heldRb.AddForce(throwDirection * throwForce, ForceMode.Impulse);

        _heldRb = null;
    }

    private void OnPull(InputAction.CallbackContext ctx)
    {
        if (_heldObject == null) return;
        _currentDistance = Mathf.Max(minDistance, _currentDistance - distanceStep);
    }

    private void OnPush(InputAction.CallbackContext ctx)
    {
        if (_heldObject == null) return;
        _currentDistance = Mathf.Min(maxDistance, _currentDistance + distanceStep);
    }


    private void FixedUpdate()
    {
        if (_heldObject == null || _heldRb == null) return;

        Ray ray = CrosshairRay;
        Vector3 targetPosition = ray.origin + ray.direction * _currentDistance;

        Vector3 delta = targetPosition - _heldObject.transform.position;
        _heldRb.linearVelocity = delta * followSpeed;
    }


    private void DropObject()
    {
        if (_heldRb == null) return;
        ReleaseObject();
        _heldRb.linearDamping = 0f;
        _heldRb.angularDamping = 0.05f;
        _heldRb = null;
    }

    private void ReleaseObject()
    {
        if (_heldRb != null)
            _heldRb.useGravity = true;

        _heldObject = null;
    }
}