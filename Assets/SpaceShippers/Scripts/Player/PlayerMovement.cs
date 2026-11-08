using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ship")]
    [SerializeField] private Renderer _shipRenderer;
    private Rigidbody _rb;

    [FormerlySerializedAs("_moveSpeed")]
    [Header("Steering & Movement Forces")]
    [SerializeField] private float _maxMoveSpeed;
    [SerializeField] private float _acceleration = 1.0f;
    private float _currentSpeed = 0; 
    [SerializeField] private float _steerSpeed;
    [SerializeField] private float _brakeForce;
    [SerializeField] private float _linearDampening;
    [SerializeField] private float _rollSpeed = 1.0f; 
    [SerializeField] private float _rollDampening;

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
        
        _rb.angularDamping = _rollDampening;

        // Set defaults
        _defaultColor = _shipRenderer.material.color;

        // Set status
        _dashEnergy = _maxDashEnergy;
    }

    public void Update()
    {
        _dashEnergy += _dashRechargeRate * Time.deltaTime;
        _dashEnergy = Mathf.Min(_dashEnergy, _maxDashEnergy);
        _rb.AddForce(transform.forward * _currentSpeed, ForceMode.Force);
    }

    public void Steer(Vector2 input)
    {
        transform.Rotate(new Vector3(-input.y, input.x, 0) * _steerSpeed);
    }
    
    public void Roll(float input)
    {
        transform.Rotate(new Vector3(0, 0, -input) * _rollSpeed);
    }

    public void ForwardThrust()
    {
        _currentSpeed += (_acceleration * Time.deltaTime);
        if (_currentSpeed > _maxMoveSpeed) _currentSpeed = _maxMoveSpeed;
        else if (_currentSpeed < -_maxMoveSpeed) _currentSpeed = -_maxMoveSpeed;
    }

    public void ReverseThrust()
    {
        _currentSpeed -= (_acceleration * Time.deltaTime);

        if (_currentSpeed < -_maxMoveSpeed) _currentSpeed = -_maxMoveSpeed;
        else if (_currentSpeed > _maxMoveSpeed) _currentSpeed = _maxMoveSpeed;
    }

    public void Dash()
    {
        if (IsDashing) return;
        if (_dashEnergy < _dashCost)
        {
            _shipRenderer.material.color = _dashFailColor;
            Invoke(nameof(SetDefaultColor), 0.1f);
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
        SetDefaultColor();
        IsDashing = false;
        DashStateChanged?.Invoke();
    }
    
    public void Brake()
    {
        if (_currentSpeed > 0)
        {
            _currentSpeed -= (_brakeForce * Time.deltaTime);
            if (_currentSpeed < 0) _currentSpeed = 0;
        }
        else if (_currentSpeed < 0)
        {
            _currentSpeed += (_brakeForce * Time.deltaTime);
            if (_currentSpeed > 0) _currentSpeed = 0;
        } 
        else
            _currentSpeed = 0;
        
        if (Mathf.Abs(_currentSpeed) < 0.1f)
            _currentSpeed = 0;
    }

    private void SetDefaultColor()
    {
        _shipRenderer.material.color = _defaultColor;
    }
}
