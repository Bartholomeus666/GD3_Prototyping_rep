using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to a Canvas GameObject.
/// Assign the Slider and (optionally) the fill Image in the Inspector,
/// then point it at the player's PlayerMovement component.
/// </summary>
public class StaminaUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  References
    // ─────────────────────────────────────────────
    [Header("References")]
    [Tooltip("The UI Slider that represents stamina.")]
    public Slider staminaSlider;

    [Tooltip("The Fill Image of the Slider — used for colour transitions.")]
    public Image fillImage;

    [Tooltip("The PlayerMovement component to read stamina from.")]
    public PlayerMovement playerMovement;

    // ─────────────────────────────────────────────
    //  Colour Settings
    // ─────────────────────────────────────────────
    [Header("Fill Colour")]
    [Tooltip("Colour when stamina is full.")]
    public Color fullColour = new Color(0.18f, 0.80f, 0.44f);   // green
    [Tooltip("Colour when stamina is at the warning threshold.")]
    public Color warningColour = new Color(1.00f, 0.76f, 0.03f);   // yellow
    [Tooltip("Colour when stamina is critically low.")]
    public Color emptyColour = new Color(0.91f, 0.18f, 0.18f);   // red
    [Tooltip("Normalised stamina level at which the colour switches to warning.")]
    [Range(0f, 1f)]
    public float warningThreshold = 0.5f;
    [Tooltip("Normalised stamina level at which the colour switches to critical.")]
    [Range(0f, 1f)]
    public float criticalThreshold = 0.2f;

    // ─────────────────────────────────────────────
    //  Smooth Fill Settings
    // ─────────────────────────────────────────────
    [Header("Smoothing")]
    [Tooltip("How fast the bar visually catches up to the actual stamina value.")]
    public float smoothSpeed = 8f;

    // ─────────────────────────────────────────────
    //  Fade Settings
    // ─────────────────────────────────────────────
    [Header("Auto-Hide")]
    [Tooltip("Fade the bar out when stamina is full and not in use.")]
    public bool autoHide = true;
    [Tooltip("Seconds at full stamina before the bar starts fading out.")]
    public float hideDelay = 2f;
    [Tooltip("How fast the bar fades in / out.")]
    public float fadeSpeed = 3f;

    [Tooltip("CanvasGroup on the bar root for fading. Leave empty to disable fading.")]
    public CanvasGroup canvasGroup;

    // ─────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────
    private float _displayValue;        // smoothed slider value
    private float _hideTimer;
    private float _targetAlpha = 1f;

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // Auto-find PlayerMovement if not assigned
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerMovement == null)
            Debug.LogWarning("[StaminaUI] No PlayerMovement found in scene.", this);

        // Initialise slider
        if (staminaSlider != null)
        {
            staminaSlider.minValue = 0f;
            staminaSlider.maxValue = 1f;
            staminaSlider.value = 1f;
        }

        _displayValue = 1f;
    }

    private void Update()
    {
        if (playerMovement == null) return;

        float target = playerMovement.StaminaNormalized;

        // ── Smooth the visual fill ──────────────────
        _displayValue = Mathf.Lerp(_displayValue, target, smoothSpeed * Time.deltaTime);

        if (staminaSlider != null)
            staminaSlider.value = _displayValue;

        // ── Fill colour ─────────────────────────────
        if (fillImage != null)
            fillImage.color = GetStaminaColour(target);

        // ── Auto-hide ───────────────────────────────
        if (autoHide && canvasGroup != null)
        {
            bool atFull = Mathf.Approximately(target, 1f);

            if (atFull)
            {
                _hideTimer += Time.deltaTime;
                if (_hideTimer >= hideDelay)
                    _targetAlpha = 0f;
            }
            else
            {
                _hideTimer = 0f;
                _targetAlpha = 1f;
            }

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, _targetAlpha,
                                                   fadeSpeed * Time.deltaTime);
        }
    }

    // ─────────────────────────────────────────────
    //  Colour interpolation
    // ─────────────────────────────────────────────
    private Color GetStaminaColour(float normalised)
    {
        if (normalised <= criticalThreshold)
            return emptyColour;

        if (normalised <= warningThreshold)
        {
            float t = Mathf.InverseLerp(criticalThreshold, warningThreshold, normalised);
            return Color.Lerp(emptyColour, warningColour, t);
        }

        float t2 = Mathf.InverseLerp(warningThreshold, 1f, normalised);
        return Color.Lerp(warningColour, fullColour, t2);
    }
}