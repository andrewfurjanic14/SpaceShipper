using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Shooter))]
[RequireComponent(typeof(Miner))]
public class PlayerController : MonoBehaviour
{
    #region Scripts
    private PlayerMovement _playerMovement;
    [SerializeField] private Shooter _shooter;
    [SerializeField] private Miner _miner;
    [SerializeField] private Canvas _inventoryCanvas;
    #endregion

    #region UI
    [SerializeField] private Slider _dashMeter;
    #endregion

    #region States
    private bool _isForwardThrusting;
    private bool _isReverseThrusting;
    private bool _isBraking;
    private bool _isShooting;
    private bool _isMining;
    private uint _ticksSinceLastStart; 
    #endregion

    #region Input Handling
    private PlayerInputActions _inputSystem;
    private Vector2 _steerInput;
    private float _rollInput;
    #endregion

    private void Awake()
    {
        _inputSystem = new PlayerInputActions();
        
        _playerMovement = GetComponent<PlayerMovement>();

        if (_shooter == null)
            _shooter = GetComponent<Shooter>();
        if (_miner == null)
            _miner = GetComponent<Miner>();
    }

    private void OnEnable()
    {
        if (_inputSystem == null) return;
        _inputSystem.Player.Dash.performed += OnDash;
        _playerMovement.DashStateChanged += OnDashStateChanged;

        _inputSystem.Player.Shoot.started  += OnShoot;
        _inputSystem.Player.Shoot.canceled += OnShoot;

        _inputSystem.Player.Mine.started   += OnMine;

        if (_inventoryCanvas && _inventoryCanvas.TryGetComponent<InventoryUI>(out var inventoryUI))
        {
            _inputSystem.Player.ToggleUI.performed += inventoryUI.OnTogglePressed;
        }

        _inputSystem.Player.Enable();
    }
    private void OnDisable()
    {
        _inputSystem.Player.Dash.performed -= OnDash;

        _inputSystem.Player.Shoot.started  -= OnShoot;
        _inputSystem.Player.Shoot.canceled -= OnShoot;

        _inputSystem.Player.Mine.started   -= OnMine;

        if (_inventoryCanvas && _inventoryCanvas.TryGetComponent<InventoryUI>(out var inventoryUI))
        {
            _inputSystem.Player.ToggleUI.performed -= inventoryUI.OnTogglePressed;
        }

        _inputSystem.Player.Disable();

        if (_shooter.IsFiring)
            _shooter.StopFiring();
        if (_miner.IsMining)
            _miner.StopMining();
    }

    void Update()
    {
        // Read inputs
        _steerInput = _inputSystem.Player.Steer.ReadValue<Vector2>();
        _rollInput = _inputSystem.Player.Roll.ReadValue<float>();

        _isForwardThrusting = _inputSystem.Player.ForwardThrust.IsPressed();
        _isReverseThrusting = _inputSystem.Player.ReverseThrust.IsPressed();
        _isBraking = _inputSystem.Player.Brake.IsPressed();

        _isShooting = _inputSystem.Player.Shoot.IsPressed(); // Hold to fire

        // Update UI
        _dashMeter.value = _playerMovement.DashPercent;
        _ticksSinceLastStart++;
    }

    private void FixedUpdate()
    {
        // Movement
        _playerMovement.Steer(_steerInput);
        _playerMovement.Roll(_rollInput);

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
    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (_playerMovement.IsDashing) return;
        if (ctx.action.IsPressed())
        {
            if (_miner.IsMining)
                _miner.StopMining();
            _shooter.StartFiring();
        } 
        else
        {
            _shooter.StopFiring();
        }
    } 
    private void OnMine(InputAction.CallbackContext ctx)
    {
        if (_playerMovement.IsDashing || _ticksSinceLastStart < 5) return;

        if (!_miner.IsMining)
        {
            if (_shooter.IsFiring)
                _shooter.StopFiring();
            _miner.StartMining();
            _isMining = true;
        }
        else
        {
            _miner.StopMining();
            _isMining = false;
        }
    } 
    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (_miner.IsMining)
            _miner.StopMining();
        else if (_shooter.IsFiring)
            _shooter.StopFiring();
        
        _playerMovement.Dash();
    }
    private void OnDashStateChanged()
    {
        if (_playerMovement.IsDashing) return;

        if (_isShooting)
            _shooter.StartFiring();
        else if (_isMining)
            _miner.StartMining();
    }
}