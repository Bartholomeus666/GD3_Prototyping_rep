using UnityEngine;
using UnityEngine.InputSystem;

public class YRotator : MonoBehaviour
{
    [Header("Input Actions")]
    public InputAction rotateClockwiseAction;
    public InputAction rotateCounterClockwiseAction;

    [Header("Settings")]
    public float rotationSpeed = 90f;

    private void OnEnable()
    {
        rotateClockwiseAction.Enable();
        rotateCounterClockwiseAction.Enable();
    }

    private void OnDisable()
    {
        rotateClockwiseAction.Disable();
        rotateCounterClockwiseAction.Disable();
    }

    private void Update()
    {
        float direction = 0f;

        if (rotateClockwiseAction.IsPressed())
            direction += 1f;

        if (rotateCounterClockwiseAction.IsPressed())
            direction -= 1f;

        if (direction != 0f)
            transform.Rotate(0f, direction * rotationSpeed * Time.deltaTime, 0f, Space.World);
    }
}