using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEditor.Callbacks;

public class GravField : MonoBehaviour
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private float _gravityStrength = 39860f;
    [SerializeField] private float _scanInterval = 1.0f;

    private Collider[] _collidersInRange = new Collider[50]; // Detection
    private HashSet<Rigidbody> _affectedBodies = new ();     // Storage
    public List<Rigidbody> _removalBuffer = new ();         // Removal

    private float _nextScan;

    private void FixedUpdate()
    {
        TryScan();
        ApplyGravityToAll();
        RemoveStaleBodies();
    }

    private void TryScan()
    {
        if (Time.time < _nextScan) return;

        ScanObjectsInRange();
        _nextScan = Time.time + _scanInterval;
    }

    private void ApplyGravityToAll()
    {
        foreach (var rb in _affectedBodies)
        {
            // Skip and buffer for removal: objects that were destroyed or exited field
            if (!rb || (rb.position - transform.position).sqrMagnitude > _radius * _radius)
            {
                _removalBuffer.Add(rb);
                continue;
            }

            ApplyGravity(rb);
        }
    }

    private void RemoveStaleBodies()
    {
        foreach (var rb in _removalBuffer)
        {
            _affectedBodies.Remove(rb);
        }
        _removalBuffer.Clear();
    }
    
    private void ScanObjectsInRange()
    {
        // Record all colliders in range
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, 
            _radius, 
            _collidersInRange,
            _targetLayer,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            // identify new rb
            Rigidbody rb = _collidersInRange[i].GetComponent<Rigidbody>();
            if (rb == null) continue;

            // filter out: non-grav objects
            Gravity grav = rb.GetComponent<Gravity>();
            if (grav == null || !grav.IsAffected) continue;

            _affectedBodies.Add(rb); // HashSet avoids adding duplicates
        }
    }

    private void ApplyGravity(Rigidbody rb)
    {
        var distance = Vector3.Distance(transform.position, rb.transform.position);
        if (distance > 0f) // Avoid division by zero
        {
            // Calculate the gravitational force magnitude
            float forceMagnitude = _gravityStrength / (distance * distance);
            
            // Calculate the direction of the force
            Vector3 direction = (transform.position - rb.transform.position).normalized;
            
            // Apply the force to the rigidbody
            rb.AddForce(direction * forceMagnitude, ForceMode.Acceleration);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}
