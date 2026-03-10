using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks how long the player is airborne.
/// Airtime is paused while the player is holding onto a wall.
/// If they land after being in the air longer than the fall death threshold, they die.
/// Requires a CharacterController and PlayerMovement on the same GameObject.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FallingDeath : MonoBehaviour
{
    public float fallDeathThreshold = 3f;

    // Read-only for other scripts / UI
    public float AirTime => _airTime;
    public bool IsAirborne => _isAirborne;

    public List<GameObject> UI = new List<GameObject>();

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private CharacterController _cc;
    private PlayerMovement _movement;
    private float _airTime;
    private bool _isAirborne;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        Time.timeScale = 1;

        _cc = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerMovement>();

        if (_movement == null)
            Debug.LogWarning("[FallingDeath] No PlayerMovement found on this GameObject. " +
                             "Wall-hold pausing will not work.", this);
    }

    private void Update()
    {
        if (_cc.isGrounded)
        {
            if (_isAirborne)
                OnLanded();

            _isAirborne = false;
            _airTime = 0f;
        }
        else
        {
            _isAirborne = true;

            // Pause the timer while the player is actively holding a wall
            bool onWall = _movement != null && _movement.IsWallSliding;
            if (!onWall)
            {
                _airTime += Time.deltaTime;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────

    /// <summary>Called the frame the player touches the ground.</summary>
    private void OnLanded()
    {
        Debug.Log($"[FallingDeath] Landed after {_airTime:F2}s of free-fall.");

        if (_airTime >= fallDeathThreshold)
        {
            Debug.Log("[FallingDeath] Fatal landing!");
            Die();
        }
    }

    public void Die()
    {
        Time.timeScale = 0;

        foreach (GameObject ui in UI)
        {
            ui.SetActive(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}