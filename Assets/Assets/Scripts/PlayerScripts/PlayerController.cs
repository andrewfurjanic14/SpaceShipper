using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(MovementScript))]
public class PlayerController : MonoBehaviour
{
    // Scripts
    private MovementScript _movementScript;

    // UI
    [SerializeField] private Slider _dashMeter;

    // Movement System
    private PlayerInputActions _inputSystem;
    private bool _isForwardThrusting;
    private bool _isReverseThrusting;
    private bool _isBraking;
    private Vector2 _movementInput;

 

    private void Awake()
    {
        _inputSystem = new PlayerInputActions();

        _movementScript = GetComponent<MovementScript>();
    }

    private void OnEnable()
    {
        _inputSystem.Player.Dash.performed += _movementScript.Dash;

        _inputSystem.Player.Enable();
    }
    private void OnDisable()
    {
        _inputSystem.Player.Disable();
    }
    void Start()
    {
        
    }

    void Update()
    {
        // read inputs
        _movementInput = _inputSystem.Player.Steer.ReadValue<Vector2>();
        _isForwardThrusting = _inputSystem.Player.ForwardThrust.IsPressed();
        _isReverseThrusting = _inputSystem.Player.ReverseThrust.IsPressed();
        _isBraking = _inputSystem.Player.Brake.IsPressed();

        // update UI
        _dashMeter.value = _movementScript.DashPercent;
    }

    private void FixedUpdate()
    {
        // movement
        _movementScript.Steer(_movementInput);

        if (_isBraking)
        {
            _movementScript.Brake();
        } else
        {
            if (_isForwardThrusting)
            _movementScript.ForwardThrust();
            if (_isReverseThrusting)
            _movementScript.ReverseThrust();
        }
    }
}
