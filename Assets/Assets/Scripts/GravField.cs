using System;
using UnityEngine;
using System.Collections.Generic;

public class GravField : MonoBehaviour
{
    
    private Transform ParentTransform; 
    public float detectionRadius = 5f;
    public LayerMask targetLayer;
    public float MassConst = 39860f;

    private float TotalTime = 0.0f;
    public float TimeBetweenUpdates = 1.0f;
    
    List<Rigidbody> GravRigidbodies = new List<Rigidbody>();

    private void Awake()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 100000f; // Set a high mass to make it effectively immovable
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        ParentTransform = GetComponent<Transform>();
        gameObject.tag = "Terrestrial";
        FindObjectsInRange();
    }
    
    void FindObjectsInRange()
    {
        // 1. Gather all colliders in range
        Collider[] collidersInRange = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
        foreach (Collider col in collidersInRange)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null && !GravRigidbodies.Contains(rb))
            {
                GravRigidbodies.Add(rb);
            }
        }
    }

    // Update is called once per frame
    private void Update()
    {
        TotalTime += Time.deltaTime;
        if (TotalTime >= TimeBetweenUpdates)
        {
            FindObjectsInRange();
            TotalTime = 0.0f;
        }
        
        // 2. Apply gravity to each object in range
        foreach (Rigidbody col in GravRigidbodies)
        {
            ApplyGravity(col);
        }
    }

    private void ApplyGravity(Rigidbody r)
    {
        var distance = Vector3.Distance(transform.position, r.transform.position);
        if (distance > 0f) // Avoid division by zero
        {
            // Calculate the gravitational force magnitude
            float forceMagnitude = MassConst / (distance * distance);
            
            // Calculate the direction of the force
            Vector3 direction = (transform.position - r.transform.position).normalized;
            
            // Apply the force to the rigidbody
            r.AddForce(direction * forceMagnitude, ForceMode.Acceleration);
        }
    }
    
    public void RemoveObjectFromGravField(Rigidbody r)
    {
        if (GravRigidbodies.Contains(r))
        {
            GravRigidbodies.Remove(r);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
