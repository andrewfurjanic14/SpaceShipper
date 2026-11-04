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
    private Vector2 _movementInput;
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
        _inputSystem.Player.Dash.performed += _playerMovement.Dash;
        _inputSystem.Player.Shoot.started  += OnShootStarted;
        _inputSystem.Player.Shoot.canceled += OnShootCanceled;
        _inputSystem.Player.Mine.started  += OnMineStarted;
        // _inputSystem.Player.Mine.canceled += OnMineCanceled;
        if (_inventoryCanvas && _inventoryCanvas.TryGetComponent<InventoryUI>(out var inventoryUI))
        {
            _inputSystem.Player.ToggleUI.performed += inventoryUI.OnTogglePressed;
        }
        _inputSystem.Player.Enable();
    }
    private void OnDisable()
    {
        _inputSystem.Player.Dash.performed -= _playerMovement.Dash;
        _inputSystem.Player.Shoot.started  -= OnShootStarted;
        _inputSystem.Player.Shoot.canceled -= OnShootCanceled;
        _inputSystem.Player.Mine.started  -= OnMineStarted;
        // _inputSystem.Player.Mine.canceled -= OnMineCanceled;
        if (_inventoryCanvas && _inventoryCanvas.TryGetComponent<InventoryUI>(out var inventoryUI))
        {
            _inputSystem.Player.ToggleUI.performed -= inventoryUI.OnTogglePressed;
        }
        _inputSystem.Player.Disable();

        if (_shooter != null)
            _shooter.StopFiring();
    }

    void Update()
    {
        // read inputs
        _movementInput = _inputSystem.Player.Steer.ReadValue<Vector2>();
        _isForwardThrusting = _inputSystem.Player.ForwardThrust.IsPressed();
        _isReverseThrusting = _inputSystem.Player.ReverseThrust.IsPressed();
        _isBraking = _inputSystem.Player.Brake.IsPressed();
        _isShooting = _inputSystem.Player.Shoot.IsPressed(); // hold to fire
        _isMining = _inputSystem.Player.Mine.IsPressed();

        // update UI
        _dashMeter.value = _playerMovement.DashPercent;
        _ticksSinceLastStart++;
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
    private void OnShootStarted(InputAction.CallbackContext ctx)
    {
        _shooter.StartFiring();
    } 
    private void OnShootCanceled(InputAction.CallbackContext ctx) => _shooter.StopFiring();
    private void OnMineStarted(InputAction.CallbackContext ctx)
    {
        if (!_miner || _ticksSinceLastStart < 5) return;
        if (_miner._isMining) _miner.StopMining();
        else _miner.StartMining();
    } 
    private void OnMineCanceled(InputAction.CallbackContext ctx) => _miner.StopMining();
}