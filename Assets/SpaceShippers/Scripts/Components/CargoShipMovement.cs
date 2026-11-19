using System;
using UnityEngine;

/// <summary>
/// Moves a cargo ship with a kinematic Rigidbody, charges fuel by travel distance,
/// and optionally keeps the parked ship at a position relative to a moving planet.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CargoShipMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _arrivalDistance = 0.25f;
    [SerializeField, Min(0.01f), Tooltip("World-space distance the cargo ship can travel per fuel unit.")]
    private float unitsPerFuel = 10f;

    // Cached dependencies used while consuming fuel.
    private Rigidbody _rigidbody;
    private Inventory _playerInventory;
    private Item _fuelItem;

    // A followed destination is stored in the planet's local space so it can be
    // converted back to world space after the planet moves.
    private Transform _parkingPoint;
    private Vector3 _parkingLocalPosition;
    private Vector3 _destinationPosition;
    private bool _followsParkingPoint;

    // Remaining distance available from the most recently consumed fuel unit.
    private float _fuelDistanceRemaining;

    // State used only to produce periodic diagnostics while a route is active.
    private float _nextProgressLogTime;
    private Vector3 _lastProgressLogPosition;
    private int _fixedUpdatesSinceProgressLog;
    private float _commandedDistanceSinceProgressLog;

    /// <summary>Raised once the ship reaches its destination and becomes parked.</summary>
    public event Action Parked;

    /// <summary>True while the ship is following a route to its destination.</summary>
    public bool IsTraveling { get; private set; }

    /// <summary>True after arrival. A parked ship can continue following a moving planet.</summary>
    public bool IsParked { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        // Movement is controlled explicitly in FixedUpdate rather than by gravity
        // or forces, so make this Rigidbody kinematic.
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;

        _fuelItem = Resources.Load<Item>("Fuel");
    }

    private void Update()
    {
        // Report once per unscaled second so diagnostics are not tied to game speed.
        if (!IsTraveling || Time.unscaledTime < _nextProgressLogTime)
            return;

        _nextProgressLogTime = Time.unscaledTime + 1f;
        Vector3 position = _rigidbody.position;

        // "Moved" is the Rigidbody's observed displacement since the prior report;
        // "commanded" is the sum of steps requested during that interval.
        float distanceMoved = Vector3.Distance(position, _lastProgressLogPosition);
        _lastProgressLogPosition = position;

        string targetName;
        string remainingDistance;
        if (_followsParkingPoint && _parkingPoint == null)
        {
            targetName = "destroyed target";
            remainingDistance = "unavailable";
        }
        else
        {
            Vector3 destination = _followsParkingPoint
                ? _parkingPoint.TransformPoint(_parkingLocalPosition)
                : _destinationPosition;
            targetName = _followsParkingPoint ? _parkingPoint.name : "fixed destination";
            remainingDistance = $"{Vector3.Distance(position, destination):F1}";
        }

        int fuelCount = _playerInventory != null && _fuelItem != null &&
                        _playerInventory.Items.TryGetValue(_fuelItem, out int count)
            ? count
            : -1;

        Debug.Log(
            $"Cargo ship progress to '{targetName}': moved {distanceMoved:F1} units, " +
            $"commanded {_commandedDistanceSinceProgressLog:F1} units, " +
            $"{remainingDistance} units remaining, {_fixedUpdatesSinceProgressLog} fixed updates, " +
            $"traveling {IsTraveling}, parked {IsParked}, time scale {Time.timeScale:F2}, " +
            $"fuel distance {_fuelDistanceRemaining:F1}, " +
            $"fuel count {fuelCount}.",
            this);
        _fixedUpdatesSinceProgressLog = 0;
        _commandedDistanceSinceProgressLog = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Collisions are diagnostic only; they do not cancel or alter the route.
        if (IsTraveling)
            Debug.LogWarning($"Cargo ship collided with '{collision.collider.name}' while traveling.", this);
    }

    private void OnDisable()
    {
        // Warn if the component is disabled before its active route can finish.
        if (Application.isPlaying && IsTraveling)
            Debug.LogWarning("Cargo ship movement was disabled while its route was active.", this);
    }

    /// <summary>
    /// Travels to the supplied parking transform itself and follows it thereafter.
    /// </summary>
    /// <param name="parkingPoint">A transform representing the desired parking point.</param>
    public void MoveToPlanet(Transform parkingPoint)
    {
        if (parkingPoint == null)
        {
            Debug.LogError("A parking point is required to move the cargo ship.", this);
            return;
        }

        _parkingPoint = parkingPoint;
        _parkingLocalPosition = Vector3.zero;
        _followsParkingPoint = true;
        BeginTravel();
    }

    /// <summary>
    /// Travels to a world-space parking position, then follows that point relative
    /// to the planet as the planet moves.
    /// </summary>
    /// <param name="planet">The moving planet whose local space defines the parking point.</param>
    /// <param name="worldParkingPosition">The desired parking position in world space.</param>
    public void MoveToPlanet(Transform planet, Vector3 worldParkingPosition)
    {
        if (planet == null)
        {
            Debug.LogError("A planet is required to move the cargo ship.", this);
            return;
        }

        _parkingPoint = planet;
        _parkingLocalPosition = planet.InverseTransformPoint(worldParkingPosition);
        _followsParkingPoint = true;
        BeginTravel();
    }

    /// <summary>
    /// Travels to a fixed world-space position that is not attached to a planet.
    /// </summary>
    /// <param name="parkingPosition">The destination in world space.</param>
    public void MoveToPlanet(Vector3 parkingPosition)
    {
        _parkingPoint = null;
        _parkingLocalPosition = Vector3.zero;
        _destinationPosition = parkingPosition;
        _followsParkingPoint = false;
        BeginTravel();
    }

    private void FixedUpdate()
    {
        _fixedUpdatesSinceProgressLog++;

        // A followed target must still exist. Stop explicitly rather than
        // attempting to calculate a destination from a destroyed transform.
        if (_followsParkingPoint && _parkingPoint == null)
        {
            Debug.LogError("The cargo ship's planet or parking point was destroyed.", this);
            StopTravel();
            return;
        }

        // Travel takes priority over parked following. When travel is inactive,
        // update the ship's world position from the saved planet-local offset.
        if (!IsTraveling)
        {
            if (IsParked && _followsParkingPoint)
                _rigidbody.MovePosition(_parkingPoint.TransformPoint(_parkingLocalPosition));
            return;
        }

        // Recompute a followed destination every physics tick so a moving planet
        // does not leave the ship chasing an obsolete world-space position.
        Vector3 destination = _followsParkingPoint
            ? _parkingPoint.TransformPoint(_parkingLocalPosition)
            : _destinationPosition;
        Vector3 position = _rigidbody.position;
        float distanceToDestination = Vector3.Distance(position, destination);
        if (distanceToDestination <= 0f)
        {
            CompleteTravel(destination);
            return;
        }

        // Each fuel unit buys a fixed distance budget. Validate it before trying
        // to travel, then request another unit only after the current budget ends.
        if (unitsPerFuel <= 0f)
        {
            Debug.LogError("Units per fuel must be greater than zero.", this);
            StopTravel();
            return;
        }

        if (_fuelDistanceRemaining <= 0f && !TryConsumeFuel())
        {
            StopTravel();
            return;
        }

        // Use the exact remaining distance for the final approach; otherwise
        // convert speed to a per-physics-tick step.
        float requestedDistance = distanceToDestination <= _arrivalDistance
            ? distanceToDestination
            : _moveSpeed * Time.fixedDeltaTime;
        float stepDistance = Mathf.Min(requestedDistance, _fuelDistanceRemaining);
        Vector3 nextPosition = Vector3.MoveTowards(position, destination, stepDistance);
        float distanceMoved = Vector3.Distance(position, nextPosition);
        _commandedDistanceSinceProgressLog += distanceMoved;
        _rigidbody.MovePosition(nextPosition);

        // Debit the requested step rather than the rounded Vector3 displacement.
        // At large coordinates, float rounding can leave a tiny positive remainder
        // that is too small to change the Rigidbody position or trigger a refill.
        _fuelDistanceRemaining = Mathf.Max(0f, _fuelDistanceRemaining - stepDistance);

        if (nextPosition == destination)
            CompleteTravel(destination);
    }

    private bool TryConsumeFuel()
    {
        // Resolve and cache the player's inventory the first time fuel is needed.
        if (_playerInventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                _playerInventory = player.GetComponent<Inventory>();
        }

        // Resolve the ScriptableObject fuel item if Awake could not load it.
        if (_fuelItem == null)
            _fuelItem = Resources.Load<Item>("Fuel");

        if (_playerInventory == null)
        {
            Debug.LogError("The player or its Inventory component could not be found.", this);
            return false;
        }

        if (_fuelItem == null)
        {
            Debug.LogError("The Fuel item could not be loaded from a Resources folder.", this);
            return false;
        }

        // A successful removal replenishes the distance budget for this route.
        if (!_playerInventory.Remove(_fuelItem, 1))
        {
            Debug.LogWarning("The cargo ship has stopped because the player is out of fuel.", this);
            return false;
        }

        _fuelDistanceRemaining = unitsPerFuel;
        return true;
    }

    private void CompleteTravel(Vector3 destination)
    {
        _rigidbody.MovePosition(destination);
        IsTraveling = false;
        IsParked = true;

        // Keep the followed planet and local offset so FixedUpdate can maintain
        // the parking position as that planet continues to move.
        string targetName = _followsParkingPoint && _parkingPoint != null
            ? _parkingPoint.name
            : "fixed destination";
        Debug.Log($"Cargo ship parked at '{targetName}' at {destination}.", this);
        Parked?.Invoke();
    }

    private void BeginTravel()
    {
        IsTraveling = true;
        IsParked = false;

        // Initialize progress diagnostics and report the route's starting range.
        Vector3 destination = _followsParkingPoint
            ? _parkingPoint.TransformPoint(_parkingLocalPosition)
            : _destinationPosition;
        string targetName = _followsParkingPoint ? _parkingPoint.name : "fixed destination";
        float distance = Vector3.Distance(_rigidbody.position, destination);
        _lastProgressLogPosition = _rigidbody.position;
        _nextProgressLogTime = Time.unscaledTime + 1f;
        _fixedUpdatesSinceProgressLog = 0;
        _commandedDistanceSinceProgressLog = 0f;
        Debug.Log($"Cargo ship route started to '{targetName}', {distance:F1} units away at {_moveSpeed:F1} units/s.", this);
    }

    private void StopTravel()
    {
        // Clear all target state so the ship neither continues nor follows a
        // previously selected planet after an error or failed fuel check.
        _parkingPoint = null;
        _parkingLocalPosition = Vector3.zero;
        _followsParkingPoint = false;
        IsTraveling = false;
        IsParked = false;
    }
}
