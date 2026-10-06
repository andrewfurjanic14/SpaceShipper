using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Script Access
    [SerializeField] private MovementScript _movementScript;
    // Movement System
    private PlayerInputActions _inputSystem;
    private InputAction _moveAction;
    private Vector3 _movementInput;
 
    // States
    public PlayerState CurrentState { get; private set; }

    public static PlayerController Instance; // singleton

    private void Awake()
    {
        _inputSystem = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _moveAction = _inputSystem.Player.Move;
        _moveAction.Enable();
    }
    private void OnDisable()
    {
        _moveAction.Disable();
    }
    void Start()
    {
        
    }

    void Update()
    {
        _movementInput = _moveAction.ReadValue<Vector3>();
    }

    private void FixedUpdate()
    {
        CurrentState.FixedUpdateState();
        _movementScript.Move(_movementInput);
    }
}
