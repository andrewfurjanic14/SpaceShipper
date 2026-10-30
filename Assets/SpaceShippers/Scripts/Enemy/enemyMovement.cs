using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    public Transform Target => target;

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 5f; // Used as the Slerp speed multiplier now
    [SerializeField] private float stopDistance = 3f;
    [SerializeField] private float maxAccel = 5f; // Used for smooth speed lerping

    [Header("Orbit Settings")] [SerializeField]
    private float orbitSpeed = 2f;

    [SerializeField] private float altitude = 5f;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private LayerMask orbitalMask;

    const uint refreshInterval = 100;
    private uint refreshCount = 0;

    private float planetRadius = 5f;
    public GameObject CurrentlyOrbiting { get; private set; } = null;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    private void Start()
    {
        if (Random.value < 0.5f)
        {
            GameObject cargo = GameObject.Find("cargoship");
            if (cargo != null) target = cargo.transform;
        }
        else
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
    }

    public void FindNearestOrbitableObject()
    {
        var results = new Collider[100];
        var size = Physics.OverlapSphereNonAlloc(rb.position, 1000f, results, orbitalMask);
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
            var sph = CurrentlyOrbiting.GetComponent<ProceduralSphere>();
            if (sph)
            {
                planetRadius = sph.radius;
            }
            else
            {
                Physics.Raycast(transform.position, (CurrentlyOrbiting.transform.position - transform.position).normalized, 
                    out RaycastHit hit, 1000f);
                planetRadius = (transform.position - CurrentlyOrbiting.transform.position).magnitude - hit.distance;
            }
        }
    }

    private void FixedUpdate()
    {
        // Physics updates can go here if needed
    }

    public void Idle(float deltaTime)
    {
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, deltaTime * maxAccel);
    }

    public void Patrol(float deltaTime)
    {
        // 1. Ensure we have a target to orbit
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

        // 2. Check for obstacles ahead
        RaycastHit hit;
        Debug.DrawRay(transform.position, transform.forward * 100f, Color.cyan);

        if (Physics.Raycast(transform.position, transform.forward, out hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            desiredDirection = Vector3.Reflect(transform.forward, hit.normal).normalized;
        }
        else
        {
            // 3. True Perpendicular Orbital Mechanics
            Vector3 planetCenter = CurrentlyOrbiting.transform.position;
            Vector3 radialVector = transform.position - planetCenter;
            float currentDistance = radialVector.magnitude;
            Vector3 surfaceNormal = radialVector.normalized;

            float targetDistance = planetRadius + altitude;

            // Safe Cross Product: Check if orbitAxis and surfaceNormal are parallel (at the poles)
            Vector3 tangentDirection = Vector3.Cross(orbitAxis, surfaceNormal);
            if (tangentDirection.sqrMagnitude < 0.001f)
            {
                // Fallback axis to prevent zeroing out near poles
                tangentDirection = Vector3.Cross(Vector3.right, surfaceNormal);
            }

            tangentDirection.Normalize();

            // Clamped altitude correction so it never overpowers the orbit direction
            float distanceError = currentDistance - targetDistance;
            float maxCorrectionPull = 0.3f; // Limits how hard it pulls inward/outward
            Vector3 correctionVector =
                -surfaceNormal * Mathf.Clamp(distanceError, -maxCorrectionPull, maxCorrectionPull);

            // Combine tangent movement with safe altitude correction
            desiredDirection = (tangentDirection + correctionVector).normalized;
        }

        // 4. Smooth Rotation using Quaternion.Slerp over time
        if (CurrentlyOrbiting)
        {
            Vector3 surfaceNormal = (transform.position - CurrentlyOrbiting.transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, surfaceNormal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * deltaTime);
        }

        // 5. Smoothly Lerp up to the target movement speed
        Vector3 targetVelocity = transform.forward * moveSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, maxAccel * deltaTime);
    }
}