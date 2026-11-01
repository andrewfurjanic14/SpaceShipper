using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ship")]
    [SerializeField] private Renderer _shipRenderer;
    private Rigidbody _rb;

    [Header("Steering & Movement Forces")]
    [SerializeField] private float _moveSpeed;
    [SerializeField] private float _steerSpeed;
    [SerializeField] private float _brakeForce;
    [SerializeField] private float _linearDampening;

    [Header("Dash")]
    [SerializeField] private float _dashForce;
    [SerializeField] private float _dashCost;
    [SerializeField] private float _dashDuration;
    [SerializeField] private float _dashRechargeRate;
    [SerializeField] private float _maxDashEnergy;

    [Space]
    [SerializeField] private Color _dashColor = Color.cyan;
    [SerializeField] private Color _dashFailColor = Color.red;

    // Defaults
    private Color _defaultColor;

    // Runtime Status
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
        transform.Rotate(new Vector3(-input.y, input.x, 0) * _steerSpeed);
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
