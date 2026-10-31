using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // Components
    private Rigidbody _rb;
    [SerializeField] private Renderer _shipRenderer;

    // Settings
    [SerializeField] private float 
        _moveSpeed, _turnSpeed, _linearDampening, _dashForce, _brakeForce,
        _dashCost, _dashRechargeRate, _dashDuration, _maxDashEnergy;
    
    [SerializeField] private Color _dashColor = Color.cyan;
    [SerializeField] private Color _dashFailColor = Color.red;

    // Defaults
    private Color _defaultColor;

    // Status
    private float _dashEnergy;
    public float DashPercent => _dashEnergy / _maxDashEnergy;
    public bool IsDashing { get; private set; }

    // Events
    public event Action DashStateChanged;


    public void Awake()
    {
        // Get components
        _rb = GetComponent<Rigidbody>();

        // Set physics rules
        _rb.linearDamping = _linearDampening;

        // Set defaults
        _defaultColor = _shipRenderer.material.color;

        // Set status
        _dashEnergy = _maxDashEnergy;
    }

    public void Update()
    {
        _dashEnergy += _dashRechargeRate * Time.deltaTime;
        _dashEnergy = Mathf.Min(_dashEnergy, _maxDashEnergy);
    }

    public void Steer(Vector2 input)
    {
        transform.Rotate(new Vector3(-input.y, input.x, 0) * _turnSpeed);
    }

    public void ForwardThrust()
    {
        _rb.AddForce(transform.forward * _moveSpeed, ForceMode.Force);
    }

    public void ReverseThrust()
    {
        _rb.AddForce(-transform.forward * _moveSpeed, ForceMode.Force);
    }

    public void Dash(InputAction.CallbackContext ctx)
    {
        if (_dashEnergy < _dashCost)
        {
            _shipRenderer.material.color = _dashFailColor;
            return;
        }

        _rb.AddForce(transform.forward * _dashForce, ForceMode.Impulse);
        _shipRenderer.material.color = _dashColor;
        _dashEnergy -= _dashCost;
        IsDashing = true;
        DashStateChanged?.Invoke();

        Invoke(nameof(EndDash), _dashDuration);
    }

    public void EndDash()
    {
        _shipRenderer.material.color = _defaultColor;
        IsDashing = false;
        DashStateChanged?.Invoke();
    }
    
    public void Brake()
    {
        _rb.AddForce(-_rb.linearVelocity * _brakeForce, ForceMode.Force);
    }
}
