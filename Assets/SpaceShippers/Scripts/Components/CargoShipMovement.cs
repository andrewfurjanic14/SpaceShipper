using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CargoShipMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _arrivalDistance = 0.25f;

    private Rigidbody _rigidbody;
    private Transform _parkingPoint;
    private Vector3 _destinationPosition;
    private bool _followsParkingPoint;

    public event Action Parked;

    public bool IsTraveling { get; private set; }
    public bool IsParked { get; private set; }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.isKinematic = true;
    }

    public void MoveToPlanet(Transform parkingPoint)
    {
        if (parkingPoint == null)
        {
            Debug.LogError("A parking point is required to move the cargo ship.", this);
            return;
        }

        _parkingPoint = parkingPoint;
        _followsParkingPoint = true;
        IsTraveling = true;
        IsParked = false;
    }

    public void MoveToPlanet(Vector3 parkingPosition)
    {
        _parkingPoint = null;
        _destinationPosition = parkingPosition;
        _followsParkingPoint = false;
        IsTraveling = true;
        IsParked = false;
    }

    private void FixedUpdate()
    {
        if (!IsTraveling)
            return;

        if (_followsParkingPoint && _parkingPoint == null)
        {
            Debug.LogError("The cargo ship's parking point was destroyed during travel.", this);
            IsTraveling = false;
            return;
        }

        Vector3 destination = _followsParkingPoint ? _parkingPoint.position : _destinationPosition;
        if ((_rigidbody.position - destination).sqrMagnitude <= _arrivalDistance * _arrivalDistance)
        {
            _rigidbody.MovePosition(destination);
            _parkingPoint = null;
            _followsParkingPoint = false;
            IsTraveling = false;
            IsParked = true;
            Parked?.Invoke();
            return;
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            _rigidbody.position,
            destination,
            _moveSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(nextPosition);
    }
}
