using UnityEngine;
using UnityEngine.InputSystem;

public class Forces : MonoBehaviour
{
    [Header("Input")]
    public InputAction pullAction;
    public InputAction pushAction;

    [Header("Selection")]
    public float selectionRange = 20f;
    public LayerMask nonThrowableLayer;
    public LayerMask throwableLayer;

    [Header("Throw")]
    public float throwForce = 40f;
    public Vector3 CheckBox;

    private Camera _cam;

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
        pullAction.Enable();
        pushAction.Enable();

        pullAction.started += OnPush_Moveable;
        pushAction.started += OnPush_NonMoveable;
    }

    private void OnDisable()
    {
        pullAction.started -= OnPush_Moveable;
        pushAction.started -= OnPush_NonMoveable;

        pullAction.Disable();
        pushAction.Disable();
    }


    private void OnPush_Moveable(InputAction.CallbackContext ctx)
    {
        if (Physics.Raycast(CrosshairRay, out RaycastHit hit, selectionRange, nonThrowableLayer))
        {
            Vector3 forceNormal = hit.normal;
            Collider[] colliders = Physics.OverlapBox(hit.point + forceNormal, CheckBox);
            foreach (Collider collider in colliders)
            {
                if (collider.attachedRigidbody != null)
                {
                    collider.attachedRigidbody.AddForce(forceNormal * throwForce, ForceMode.Impulse);
                }
            }
        }
    }

    //private void OnHoldCanceled(InputAction.CallbackContext ctx) => DropObject();

    private void OnPush_NonMoveable(InputAction.CallbackContext ctx)
    {
        if (Physics.Raycast(CrosshairRay, out RaycastHit hit, selectionRange, throwableLayer))
        {
            hit.collider.gameObject.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * throwForce, ForceMode.Impulse);
        }
    }

    private void FixedUpdate()
    {
        Ray ray = CrosshairRay;
        Vector3 targetPosition = ray.origin + ray.direction;
    }
}
