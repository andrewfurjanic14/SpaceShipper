using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    #region Scripts
    private PlayerMovement _playerMovement;
    [SerializeField] private Weapon _weapon;
    #endregion

    #region UI
    [SerializeField] private Slider _dashMeter;
    #endregion

    #region States
    private bool _isForwardThrusting;
    private bool _isReverseThrusting;
    private bool _isBraking;
    private bool _isShooting;
    #endregion

    #region Input Handling
    private PlayerInputActions _inputSystem;
    private Vector2 _movementInput;
    #endregion

    private void Awake()
    {
        _inputSystem = new PlayerInputActions();

        _playerMovement = GetComponent<PlayerMovement>();

        if (_weapon == null)
            _weapon = GetComponent<Weapon>();
    }

    private void OnEnable()
    {
        _inputSystem.Player.Dash.performed += _playerMovement.Dash;

        _inputSystem.Player.Enable();
    }
    private void OnDisable()
    {
        _inputSystem.Player.Dash.performed -= _playerMovement.Dash;
        _inputSystem.Player.Disable();

        if (_weapon != null)
            _weapon.SetTrajectoryVisible(false);
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
        _isShooting = _inputSystem.Player.Shoot.IsPressed(); // hold to fire

        if (_weapon != null)
            _weapon.SetTrajectoryVisible(_isShooting);

        if (_isShooting && _weapon != null)
            _weapon.TryFire();

        // update UI
        _dashMeter.value = _playerMovement.DashPercent;
    }

    private void FixedUpdate()
    {
        // movement
        _playerMovement.Steer(_movementInput);

        if (_isBraking)
        {
            _playerMovement.Brake();
        } 
        else if (_isForwardThrusting)
        {
            _playerMovement.ForwardThrust();
        }
        else if (_isReverseThrusting)
        {
            _playerMovement.ReverseThrust();
        }
    }
}
