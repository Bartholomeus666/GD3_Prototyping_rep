using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maps a PhysicsMaterial to a climb speed multiplier.
/// Leave Material as None to use as the default/fallback row.
/// </summary>
[Serializable]
public class WallMaterialEntry
{
    [Tooltip("The PhysicsMaterial to match. Set to None to use as the fallback for any unrecognised surface.")]
    public PhysicsMaterial material;
    [Tooltip("Multiplier on top of wallSlideSpeedMult. e.g. 0.4 = slippery/slow, 1.5 = great grip/fast.")]
    [Range(0.05f, 3f)]
    public float speedMultiplier = 1f;
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Input Actions  (assign in Inspector)
    // ─────────────────────────────────────────────
    [Header("Input Actions")]
    public InputAction moveAction;
    public InputAction wallSlideAction;   // Space bar

    // ─────────────────────────────────────────────
    //  Movement Settings
    // ─────────────────────────────────────────────
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;

    // ─────────────────────────────────────────────
    //  Wall Slide Settings
    // ─────────────────────────────────────────────
    [Header("Wall Slide")]
    [Tooltip("How close the player must be to a wall to trigger sliding.")]
    public float wallDetectionRange = 0.6f;
    [Tooltip("Layer(s) considered as walls.")]
    public LayerMask wallLayerMask = ~0;
    [Tooltip("How many rays to cast around the player for wall detection.")]
    public int wallRayCount = 8;
    [Tooltip("Speed multiplier while sliding along a wall.")]
    public float wallSlideSpeedMult = 0.85f;
    [Tooltip("How strongly the player is pushed into the wall (cosmetic).")]
    public float wallHugStrength = 2f;

    public bool IsWallSliding => _isNearWall && wallSlideAction.IsPressed() && !_isClimbing;

    // ─────────────────────────────────────────────
    //  Wall Material Settings
    // ─────────────────────────────────────────────
    [Header("Wall Materials")]
    [Tooltip("Per-material speed multipliers. Add a row per PhysicsMaterial. " +
             "A row with Material left as None acts as the fallback for unrecognised surfaces.")]
    public List<WallMaterialEntry> wallMaterials = new List<WallMaterialEntry>();

    // ─────────────────────────────────────────────
    //  Ledge Climb Settings
    // ─────────────────────────────────────────────
    [Header("Ledge Climb")]
    [Tooltip("How far above the player's head to check for a clear ledge top.")]
    public float ledgeCheckHeight = 0.2f;
    [Tooltip("How far forward (into the wall) to cast the downward ledge ray.")]
    public float ledgeCheckDepth = 0.5f;
    [Tooltip("How long the vault-over animation takes in seconds.")]
    public float climbDuration = 0.45f;
    [Tooltip("Extra upward arc height during the climb.")]
    public float climbArcHeight = 0.4f;

    // ─────────────────────────────────────────────
    //  Stamina Settings
    // ─────────────────────────────────────────────
    [Header("Stamina")]
    [Tooltip("Maximum stamina.")]
    public float maxStamina = 100f;
    [Tooltip("Stamina drained per second while wall sliding.")]
    public float slideDrainRate = 15f;
    [Tooltip("Extra stamina drained when vaulting over a ledge (flat cost).")]
    public float climbVaultCost = 20f;
    [Tooltip("Stamina recovered per second when not on a wall.")]
    public float regenRate = 25f;
    [Tooltip("Seconds of inactivity before regen begins.")]
    public float regenDelay = 1.2f;

    // Read-only for UI / other scripts
    public float Stamina => _stamina;
    public float StaminaNormalized => _stamina / maxStamina;

    // ─────────────────────────────────────────────
    //  Camera reference
    // ─────────────────────────────────────────────
    [Header("Camera")]
    [Tooltip("Assign your camera transform so movement is camera-relative.")]
    public Transform cameraTransform;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;
    private Vector3 _wallNormal;
    private bool _isNearWall;
    private bool _isClimbing;
    private float _stamina;
    private float _regenDelayTimer;
    private PhysicsMaterial _wallHitMaterial;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (moveAction.bindings.Count == 0)
            moveAction = new InputAction("Move", binding: "<Gamepad>/leftStick");

        if (wallSlideAction.bindings.Count == 0)
            wallSlideAction = new InputAction("WallSlide", binding: "<Keyboard>/space");

        _stamina = maxStamina;
    }

    private void OnEnable()
    {
        moveAction.Enable();
        wallSlideAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        wallSlideAction.Disable();
    }

    // ─────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────
    private void Update()
    {
        if (_isClimbing) return;

        DetectWall();

        bool wantsSlide = wallSlideAction.IsPressed();
        bool sliding = wantsSlide && _isNearWall && _stamina > 0f;
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = GetCameraRelativeMovement(input);

        if (sliding)
        {
            if (input.y > 0.1f && TryGetLedgeTarget(out Vector3 ledgeTop))
            {
                StartCoroutine(ClimbOverLedge(ledgeTop));
                return;
            }

            HandleWallSlide(input);
            DrainStamina(slideDrainRate * Time.deltaTime);
        }
        else
        {
            HandleNormalMovement(move);
            ApplyGravity();
            RegenStamina();
        }
    }

    // ─────────────────────────────────────────────
    //  Normal movement
    // ─────────────────────────────────────────────
    private void HandleNormalMovement(Vector3 move)
    {
        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot,
                                                     rotationSpeed * Time.deltaTime);
        }

        _cc.Move((move * moveSpeed + Vector3.up * _velocity.y) * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Wall slide movement
    //    Horizontal (A/D)  -> slide left / right along the wall
    //    Vertical   (W/S)  -> move up / down along the wall
    // ─────────────────────────────────────────────
    private void HandleWallSlide(Vector2 rawInput)
    {
        Vector3 wallRight = Vector3.Cross(_wallNormal, Vector3.up).normalized;

        if (Vector3.Dot(wallRight, transform.right) < 0f)
            wallRight = -wallRight;

        float matMult = GetMaterialSpeedMultiplier();
        Vector3 slideVelocity = wallRight * (rawInput.x * moveSpeed * wallSlideSpeedMult * matMult)
                              + Vector3.up * (rawInput.y * moveSpeed * wallSlideSpeedMult * matMult);

        if (Mathf.Abs(rawInput.x) > 0.1f)
        {
            Vector3 faceDir = wallRight * Mathf.Sign(rawInput.x);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                   Quaternion.LookRotation(faceDir),
                                                   rotationSpeed * Time.deltaTime);
        }

        _velocity.y = 0f;
        _cc.Move((slideVelocity - _wallNormal * wallHugStrength) * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Material speed lookup
    // ─────────────────────────────────────────────
    private float GetMaterialSpeedMultiplier()
    {
        if (wallMaterials == null || wallMaterials.Count == 0) return 1f;

        // First: exact match for the current wall's PhysicsMaterial
        foreach (WallMaterialEntry entry in wallMaterials)
        {
            if (entry.material != null && entry.material == _wallHitMaterial)
                return entry.speedMultiplier;
        }

        // Second: fallback — first row with Material left as None
        foreach (WallMaterialEntry entry in wallMaterials)
        {
            if (entry.material == null)
                return entry.speedMultiplier;
        }

        return 1f;
    }

    // ─────────────────────────────────────────────
    //  Ledge detection
    // ─────────────────────────────────────────────
    private bool TryGetLedgeTarget(out Vector3 ledgeTop)
    {
        ledgeTop = Vector3.zero;

        Vector3 toWall = -_wallNormal;
        float halfHeight = _cc.height * 0.5f;

        Vector3 lowOrigin = transform.position + Vector3.up * halfHeight;
        bool lowHit = Physics.Raycast(lowOrigin, toWall, wallDetectionRange + 0.1f,
                                            wallLayerMask, QueryTriggerInteraction.Ignore);
        if (!lowHit) return false;

        Vector3 highOrigin = transform.position + Vector3.up * (_cc.height + ledgeCheckHeight);
        bool highHit = Physics.Raycast(highOrigin, toWall, wallDetectionRange + ledgeCheckDepth,
                                             wallLayerMask, QueryTriggerInteraction.Ignore);
        if (highHit) return false;

        Vector3 dropOrigin = highOrigin + toWall * (wallDetectionRange + ledgeCheckDepth);
        if (!Physics.Raycast(dropOrigin, Vector3.down, out RaycastHit surfaceHit,
                             _cc.height + ledgeCheckHeight + 1f,
                             wallLayerMask, QueryTriggerInteraction.Ignore))
            return false;

        ledgeTop = surfaceHit.point;
        return true;
    }

    // ─────────────────────────────────────────────
    //  Ledge climb coroutine
    // ─────────────────────────────────────────────
    private IEnumerator ClimbOverLedge(Vector3 ledgeTop)
    {
        _isClimbing = true;
        _velocity = Vector3.zero;

        DrainStamina(climbVaultCost);

        Vector3 destination = ledgeTop + (-_wallNormal * (_cc.radius + 0.1f));

        Vector3 faceDir = -_wallNormal;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(faceDir);

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < climbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / climbDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);
            float arc = Mathf.Sin(t * Mathf.PI) * climbArcHeight;

            Vector3 target = Vector3.Lerp(startPos, destination, smooth);
            target.y += arc;

            _cc.Move(target - transform.position);
            yield return null;
        }

        _cc.Move(destination - transform.position);
        _isClimbing = false;
    }

    // ─────────────────────────────────────────────
    //  Stamina
    // ─────────────────────────────────────────────
    private void DrainStamina(float amount)
    {
        _stamina = Mathf.Max(0f, _stamina - amount);
        _regenDelayTimer = regenDelay;
    }

    private void RegenStamina()
    {
        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        _stamina = Mathf.Min(maxStamina, _stamina + regenRate * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Gravity
    // ─────────────────────────────────────────────
    private void ApplyGravity()
    {
        if (_cc.isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
    }

    // ─────────────────────────────────────────────
    //  Wall detection - fan of rays around the player
    // ─────────────────────────────────────────────
    private void DetectWall()
    {
        _isNearWall = false;
        _wallNormal = Vector3.zero;
        _wallHitMaterial = null;

        Vector3 origin = transform.position + Vector3.up * (_cc.height * 0.5f);

        for (int i = 0; i < wallRayCount; i++)
        {
            float angle = i * (360f / wallRayCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit,
                                wallDetectionRange, wallLayerMask,
                                QueryTriggerInteraction.Ignore))
            {
                _isNearWall = true;
                _wallNormal = hit.normal;
                _wallHitMaterial = hit.collider.sharedMaterial;
                break;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Camera-relative direction from raw 2-D input
    // ─────────────────────────────────────────────
    private Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (input.sqrMagnitude < 0.01f) return Vector3.zero;

        Vector3 camForward = cameraTransform != null
            ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized
            : Vector3.forward;

        Vector3 camRight = cameraTransform != null
            ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized
            : Vector3.right;

        return (camForward * input.y + camRight * input.x).normalized;
    }

    // ─────────────────────────────────────────────
    //  Gizmos - visualise detection rays in editor
    // ─────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (_cc == null) _cc = GetComponent<CharacterController>();
        float halfH = _cc != null ? _cc.height * 0.5f : 0.9f;
        float fullH = _cc != null ? _cc.height : 1.8f;
        Vector3 origin = transform.position + Vector3.up * halfH;

        Gizmos.color = _isNearWall ? Color.red : Color.cyan;
        for (int i = 0; i < wallRayCount; i++)
        {
            float angle = i * (360f / wallRayCount);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Gizmos.DrawRay(origin, dir * wallDetectionRange);
        }

        if (_isNearWall)
        {
            Vector3 toWall = -_wallNormal;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(origin, _wallNormal * 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position + Vector3.up * halfH,
                           toWall * (wallDetectionRange + 0.1f));

            Gizmos.color = Color.magenta;
            Vector3 highOrigin = transform.position + Vector3.up * (fullH + ledgeCheckHeight);
            Gizmos.DrawRay(highOrigin, toWall * (wallDetectionRange + ledgeCheckDepth));

            Gizmos.color = Color.white;
            Vector3 dropOrigin = highOrigin + toWall * (wallDetectionRange + ledgeCheckDepth);
            Gizmos.DrawRay(dropOrigin, Vector3.down * (fullH + ledgeCheckHeight + 1f));
        }
    }
}