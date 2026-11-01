using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    #region Scripts
    private PlayerMovement _playerMovement;
    private Shooter _shooter;
    private Miner _miner;
    #endregion

    #region UI
    [SerializeField] private Slider _dashMeter;
    #endregion

    #region Input Handling
    private PlayerInputActions _inputSystem;
    private Vector2 _SteerInput;
    #endregion

    #region Tools
    private enum Tool { None, Shooter, Miner }
    private Tool _activeTool = Tool.None;
    private bool CanUseTools => !_playerMovement.IsDashing;
    #endregion

    #region Runtime Status
    private bool _isForwardThrusting;
    private bool _isReverseThrusting;
    private bool _isBraking;
    private bool _isShooting;
    private bool _isMining;
    #endregion


    private void Awake()
    {
        _inputSystem = new PlayerInputActions();

        _playerMovement = GetComponent<PlayerMovement>();
        _shooter = GetComponent<Shooter>();
        _miner = GetComponent<Miner>();
    }

    private void OnEnable()
    {
        var p = _inputSystem.Player;
        p.Dash.performed += _playerMovement.Dash;

        p.Shoot.started += OnShoot;
        p.Shoot.canceled += OnShoot;

        p.Mine.started  += OnMine;
        p.Mine.canceled  += OnMine;

        p.ForwardThrust.started += OnThrust;
        p.ForwardThrust.canceled += OnThrust;

        p.ReverseThrust.started += OnThrust;
        p.ReverseThrust.canceled += OnThrust;

        p.Brake.started += OnThrust;
        p.Brake.canceled += OnThrust;

        p.Enable();

        // Dash restricts usage of tools for its duration
        _playerMovement.DashStateChanged += RefreshActiveTool;
    }
    private void OnDisable()
    {
        var p = _inputSystem.Player;
        p.Dash.performed -= _playerMovement.Dash;

        p.Shoot.started -= OnShoot;
        p.Shoot.canceled -= OnShoot;

        p.Mine.started -= OnMine;
        p.Mine.canceled -= OnMine;

        p.ForwardThrust.started -= OnThrust;
        p.ForwardThrust.canceled -= OnThrust;

        p.ReverseThrust.started -= OnThrust;
        p.ReverseThrust.canceled -= OnThrust;

        p.Brake.started -= OnThrust;
        p.Brake.canceled -= OnThrust;

        p.Disable();

        // Stop all running actions
        _isShooting = _isMining = false;
        _isForwardThrusting = _isReverseThrusting = _isBraking = false;
        ActivateTool(Tool.None);
    }

    void Update()
    {
        _SteerInput = _inputSystem.Player.Steer.ReadValue<Vector2>();

        _dashMeter.value = _playerMovement.DashPercent;
    }

    private void FixedUpdate()
    {
        // movement
        _playerMovement.Steer(_SteerInput);

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

    #region Event Handlers
    private void OnThrust(InputAction.CallbackContext ctx)
    {
        _isForwardThrusting = _inputSystem.Player.ForwardThrust.IsPressed();
        _isReverseThrusting = _inputSystem.Player.ReverseThrust.IsPressed();
        _isBraking = _inputSystem.Player.Brake.IsPressed();  
    }
    private void OnShoot(InputAction.CallbackContext ctx)
    {
        _isShooting = ctx.ReadValueAsButton();
        RefreshActiveTool();
    } 
    private void OnMine(InputAction.CallbackContext ctx)
    {
        _isMining = ctx.ReadValueAsButton();
        RefreshActiveTool();
    } 
    #endregion

    #region Tool Helpers
    /// <summary>
    /// Considers input bools to refresh <see cref="_activeTool"/> 
    /// </summary>
    private void RefreshActiveTool()
    {
        if (!CanUseTools)
        {
            ActivateTool(Tool.None);
            return;
        }

        bool activeStillHeld = _activeTool == Tool.Shooter ? _isShooting
                             : _activeTool == Tool.Miner   ? _isMining
                             : false;
        if (activeStillHeld) return;

        if (_isShooting)
            ActivateTool(Tool.Shooter);
        else if (_isMining)
            ActivateTool(Tool.Miner);
        else
            ActivateTool(Tool.None);
    }

    /// <summary>
    /// Handles the switch of <see cref="_activeTool"/> : stops old tool, starts new tool
    /// </summary>
    private void ActivateTool(Tool next)
    {
        if (next == _activeTool) return;

        // exit
        if (_activeTool == Tool.Shooter)
            _shooter.StopFiring();
        if (_activeTool == Tool.Miner)
             _miner.StopMining();

        _activeTool = next;

        // enter
        if (_activeTool == Tool.Shooter)
            _shooter.StartFiring();
        if (_activeTool == Tool.Miner)
             _miner.StartMining();
    }
    #endregion
}
