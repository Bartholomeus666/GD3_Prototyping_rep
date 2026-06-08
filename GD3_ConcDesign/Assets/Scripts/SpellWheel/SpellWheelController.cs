using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class SpellWheelController : MonoBehaviour
{
    [Header("References")]
    public SpellWheelConfig   config;
    public SpellWheelRenderer wheelRenderer;
    public Transform          castOrigin;

    [Header("Settings")]
    public float deadzonePx = 24f;

    private InputAction _rmb;
    private Vector2     _openPos;
    private bool        _isOpen;

    void Awake()     => _rmb = GetComponent<PlayerInput>().actions["SpellWheel"];
    void OnEnable()  { _rmb.performed += Open;  _rmb.canceled += Close; }
    void OnDisable() { _rmb.performed -= Open;  _rmb.canceled -= Close; }

    void Update()
    {
        if (_isOpen)
            wheelRenderer.UpdateHighlight(Mouse.current.position.ReadValue() - _openPos);
    }

    void Open(InputAction.CallbackContext _)
    {
        _openPos = Mouse.current.position.ReadValue();
        _isOpen  = true;
        wheelRenderer.Show(config);
    }

    void Close(InputAction.CallbackContext _)
    {
        if (!_isOpen) return;
        _isOpen = false;
        wheelRenderer.Hide();

        Vector2 drag = Mouse.current.position.ReadValue() - _openPos;
        if (drag.magnitude < deadzonePx) return;

        // Reuse same DragToAngle logic (duplicated here for self-containment)
        float deg     = Mathf.Atan2(drag.y, drag.x) * Mathf.Rad2Deg;
        float adjusted = ((90f - deg) % 360f + 360f) % 360f;

        var spell = config.GetSpellAtAngle(adjusted);
        if (spell == null) return;

        Vector3 origin = castOrigin ? castOrigin.position : transform.position;
        Vector3 dir    = AimDir(Mouse.current.position.ReadValue());
        spell.Cast(castOrigin);
    }

    Vector3 AimDir(Vector2 screenPos)
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0));
        return Physics.Raycast(ray, out var hit, 200f)
            ? (hit.point - (castOrigin ? castOrigin.position : transform.position)).normalized
            : ray.direction;
    }
}
