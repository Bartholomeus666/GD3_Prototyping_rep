using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private CharacterController _controller;
    private Vector2 _moveVector;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void Move(InputAction.CallbackContext context)
    {
        _moveVector = context.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        _controller.Move(new Vector3(_moveVector.x, 0, _moveVector.y) * speed * Time.fixedDeltaTime);

        if (_moveVector != Vector2.zero)
            transform.rotation = Quaternion.LookRotation(new Vector3(_moveVector.x, 0, _moveVector.y));
    }
}
