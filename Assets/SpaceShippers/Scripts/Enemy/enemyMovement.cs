using System;
using System.Collections.Generic;
using SpaceShippers.Scripts.PCG;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Steering & Movement Forces")]
    [SerializeField] private float _maxMoveSpeed = 8f;
    [SerializeField] private float _acceleration = 5f;
    [SerializeField] private float _brakeForce = 7f;
    [SerializeField] private float _linearDampening = 1f;
    [SerializeField] private float turnSpeed = 5f; // Slerp speed multiplier
    [SerializeField] private float stopDistance = 3f;

    [Header("Pathfinding & Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float obstacleCheckDistance = 15f;
    [SerializeField] private float waypointReachedThreshold = 1.5f;

    [SerializeField] private float baseRePathThreshold = 3.0f; // Distance target must move to trigger a full re-path

    [Header("Orbit Settings")]
    [SerializeField] private float altitude = 5f;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private LayerMask orbitalMask;


    private float _currentSpeed = 0f;
    const uint refreshInterval = 100;
    private uint refreshCount = 0;

    private float planetRadius = 5f;
    public GameObject CurrentlyOrbiting { get; private set; } = null;

    // Path Data 
    private List<Vector3> _pathWaypoints = new List<Vector3>();
    private int _currentWaypointIndex = 0;
    private Vector3 _lastTargetPosition;
    public void ScaleSpeed(float mult) => _maxMoveSpeed *= mult;


    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = _linearDampening;
    }

    private void FindNearestOrbitableObject()
    {
        var results = new Collider[100];
        var size = Physics.OverlapSphereNonAlloc(rb.position, 100000f, results, orbitalMask);
        if (size == 0) return;

        GameObject nearest = null;
        var minDistance = float.MaxValue;

        foreach (var c in results)
        {
            if (!c) continue;
            var distance = Vector3.Distance(rb.position, c.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = c.gameObject;
            }
        }

        CurrentlyOrbiting = nearest;

        if (CurrentlyOrbiting)
        {
            var sph = CurrentlyOrbiting.GetComponent<SphereGen>();
            if (sph)
            {
                planetRadius = sph.radius;
            }
            else
            {
                if (Physics.Raycast(transform.position, (CurrentlyOrbiting.transform.position - transform.position).normalized, out RaycastHit hit, 1000f))
                {
                    planetRadius = (transform.position - CurrentlyOrbiting.transform.position).magnitude - hit.distance;
                }
            }
        }
    }

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * _currentSpeed, ForceMode.Force);
    }

    public void Idle(float deltaTime)
    {
        ApplyBrakes(deltaTime);
        _pathWaypoints.Clear();
    }

    public void Patrol(float deltaTime)
    {
        _pathWaypoints.Clear(); // Clear path when switching to patrol

        if (refreshCount-- <= 0)
        {
            refreshCount = refreshInterval;
            if (!CurrentlyOrbiting)
            {
                FindNearestOrbitableObject();
                if (!CurrentlyOrbiting) return;
            }
        }

        Vector3 desiredDirection;
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * 100f, Color.cyan);

        if (Physics.Raycast(transform.position, transform.forward, out hit, 100f, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            desiredDirection = Vector3.Reflect(transform.forward, hit.normal).normalized;
        }
        else
        {
            if (!CurrentlyOrbiting)
            {
                FindNearestOrbitableObject();
                if (!CurrentlyOrbiting) return;
            }
            
            var planetCenter = CurrentlyOrbiting.transform.position;
            var radialVector = transform.position - planetCenter;
            var currentDistance = radialVector.magnitude;
            var surfaceNormal = radialVector.normalized;

            var targetDistance = planetRadius + altitude;

            var tangentDirection = Vector3.Cross(orbitAxis, surfaceNormal);
            if (tangentDirection.sqrMagnitude < 0.001f)
            {
                // Fallback axis to prevent zeroing out near poles
                tangentDirection = Vector3.Cross(Vector3.right, surfaceNormal);
            }

            tangentDirection.Normalize();

            // Clamped altitude correction so it never overpowers the orbit direction
            var distanceError = currentDistance - targetDistance;
            const float maxCorrectionPull = 0.3f; // Limits how hard it pulls inward/outward
            var correctionVector =
                -surfaceNormal * Mathf.Clamp(distanceError, -maxCorrectionPull, maxCorrectionPull);

            // Combine tangent movement with safe altitude correction
            desiredDirection = (tangentDirection + correctionVector).normalized;
        }

        if (CurrentlyOrbiting)
        {
            var surfaceNormal = (transform.position - CurrentlyOrbiting.transform.position).normalized;
            var targetRotation = Quaternion.LookRotation(desiredDirection, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        Accelerate(deltaTime);
    }
    
    public void MoveTowards(Vector3 targetPosition, float deltaTime)
    {
        var distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // Determine re-pathing threshold based on distance:
        // Closer to target = smaller threshold (more willing/frequent to recalculate), fewer steps needed.
        // Farther away = larger threshold (sticks to cached path longer, aims broadly).
        var dynamicRePathThreshold = Mathf.Lerp(0.5f, baseRePathThreshold, Mathf.Clamp01(distanceToTarget / 50f));
        var targetMovementDelta = Vector3.Distance(targetPosition, _lastTargetPosition);

        // Check if we need to generate a new path
        var needsNewPath = _pathWaypoints.Count == 0 || 
                           targetMovementDelta > dynamicRePathThreshold || 
                           _currentWaypointIndex >= _pathWaypoints.Count;

        if (needsNewPath)
        {
            GeneratePath(targetPosition, distanceToTarget);
            _lastTargetPosition = targetPosition;
        }

        // Traverse current waypoint if valid
        var steeringTarget = targetPosition;
        if (_pathWaypoints.Count > 0 && _currentWaypointIndex < _pathWaypoints.Count)
        {
            steeringTarget = _pathWaypoints[_currentWaypointIndex];

            // If close enough to current waypoint, advance to the next one
            if (Vector3.Distance(transform.position, steeringTarget) < waypointReachedThreshold)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex < _pathWaypoints.Count)
                {
                    steeringTarget = _pathWaypoints[_currentWaypointIndex];
                }
            }
        }

        // Calculate direction to current steering target with obstacle avoidance raycast
        var desiredDirection = (steeringTarget - transform.position).normalized;
        var checkDist = Mathf.Min(Vector3.Distance(transform.position, steeringTarget), obstacleCheckDistance);
        
        Debug.DrawRay(transform.position, desiredDirection * checkDist, Color.yellow);

        if (Physics.Raycast(transform.position, desiredDirection, out RaycastHit hit, checkDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            desiredDirection = Vector3.Reflect(desiredDirection, hit.normal).normalized;
        }

        if (desiredDirection != Vector3.zero)
        {
            var targetRotation = Quaternion.LookRotation(desiredDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        // Accelerate when far, brake when within stop distance
        if (distanceToTarget > stopDistance)
        {
            Accelerate(deltaTime);
        }
        else
        {
            ApplyBrakes(deltaTime);
        }
    }

    private void GeneratePath(Vector3 targetPosition, float distanceToTarget)
    {
        _pathWaypoints.Clear();
        _currentWaypointIndex = 0;

        // If far away, use fewer intermediate steps (just the target or a simple midpoint)
        // If close, we can map precise adjustments or direct line-of-sight
        var midPoint = Vector3.Lerp(transform.position, targetPosition, 0.5f);
        if (distanceToTarget > 30f)
        {
            // Far away: Create a simpler path with fewer steps (e.g., just a halfway check and target)

            // Offset midpoint slightly if blocked
            if (Physics.Raycast(transform.position, (targetPosition - transform.position).normalized, out RaycastHit hit, 15f, obstacleMask))
            {
                midPoint += hit.normal * 5f; 
            }
            
            _pathWaypoints.Add(midPoint);
        }

        // Closer: Direct tracking with tighter resolution
        _pathWaypoints.Add(targetPosition);
    }

    private void Accelerate(float deltaTime)
    {
        _currentSpeed += _acceleration * deltaTime;
        _currentSpeed = Mathf.Min(_currentSpeed, _maxMoveSpeed);
    }

    private void ApplyBrakes(float deltaTime)
    {
        if (_currentSpeed > 0)
        {
            _currentSpeed -= _brakeForce * deltaTime;
            if (_currentSpeed < 0) _currentSpeed = 0;
        }
        else if (_currentSpeed < 0)
        {
            _currentSpeed += _brakeForce * deltaTime;
            if (_currentSpeed > 0) _currentSpeed = 0;
        }
        else
        {
            _currentSpeed = 0;
        }

        if (Mathf.Abs(_currentSpeed) < 0.1f)
            _currentSpeed = 0;
    }
}