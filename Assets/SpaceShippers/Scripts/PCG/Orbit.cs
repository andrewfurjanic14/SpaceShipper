using UnityEngine;

namespace SpaceShippers.Scripts.PCG
{
    public class Orbit : MonoBehaviour
    {
        [Header("Orbital Mechanics")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float altitude = 5f;
        [SerializeField] private Vector3 orbitAxis = Vector3.up;
        [SerializeField] private LayerMask orbitalMask;

        [Header("Look Update Settings")]
        [SerializeField] private float turnSpeed = 5f;

        [Header("Moon Self-Rotation")]
        [Tooltip("If true, the same side of the moon will always face the planet (like Earth's Moon).")]
        [SerializeField] private bool tidallyLocked = true;
        
        [Tooltip("Only works if Tidally Locked is false. Lets the moon spin on its own axis.")]
        [SerializeField] private bool customMoonSpin = false;
        [SerializeField] private Vector3 moonSpinAxis = Vector3.up;
        [SerializeField] private float moonSpinSpeed = 30f;

        private const uint refreshInterval = 100;
        private uint refreshCount = 0;

        private float planetRadius = 5f;
        [SerializeField] private GameObject CurrentlyOrbiting = null;
        private Rigidbody rb;

        private float currentAngle = 0f;
        private float targetDistance = 10f;
        private Quaternion targetRotation;
        private float moonSelfSpinAngle = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            
            rb.useGravity = false;
        }

        private void Start()
        {
            if (!CurrentlyOrbiting) FindNearestOrbitableObject();
            else InitOrbit();
            InitializeAngle();
        }

        private void FixedUpdate()
        {
            if (refreshCount == 0)
            {
                refreshCount = refreshInterval;
                if (!CurrentlyOrbiting)
                {
                    FindNearestOrbitableObject();
                    if (!CurrentlyOrbiting) return;
                    InitializeAngle();
                }
            }
            else
            {
                refreshCount--;
            }

            if (!CurrentlyOrbiting) return;

            // 1. Advance the orbit loop path angle
            float angularVelocity = moveSpeed / targetDistance;
            currentAngle += angularVelocity * Time.fixedDeltaTime;

            // 2. Compute path position coordinates
            Vector3 planetCenter = CurrentlyOrbiting.transform.position;
            Vector3 normAxis = orbitAxis.normalized;
            
            Vector3 planeRight = Vector3.Cross(normAxis, Vector3.up);
            if (planeRight.sqrMagnitude < 0.001f) planeRight = Vector3.Cross(normAxis, Vector3.forward);
            planeRight.Normalize();
            
            Vector3 planeForward = Vector3.Cross(planeRight, normAxis).normalized;

            Vector3 orbitalOffset = (planeRight * Mathf.Cos(currentAngle) + planeForward * Mathf.Sin(currentAngle)) * targetDistance;
            Vector3 desiredPosition = planetCenter + orbitalOffset;

            rb.MovePosition(desiredPosition);

            // 3. Keep velocity vectors matched up
            Vector3 tangentDirection = (-planeRight * Mathf.Sin(currentAngle) + planeForward * Mathf.Cos(currentAngle)).normalized;
            rb.linearVelocity = tangentDirection * moveSpeed;

            // 4. Calculate core directional face targets
            Vector3 surfaceNormal = (desiredPosition - planetCenter).normalized;

            if (tidallyLocked)
            {
                // Force the forward face to look along the track, and up-axis to align with the planet core
                targetRotation = Quaternion.LookRotation(tangentDirection, surfaceNormal);
            }
            else if (customMoonSpin)
            {
                // Advance custom spin angle
                moonSelfSpinAngle += moonSpinSpeed * Time.fixedDeltaTime;
                
                // Keep the base tracking direction but multiply an added rotation offset over it
                Quaternion baseTrackLook = Quaternion.LookRotation(tangentDirection, surfaceNormal);
                Quaternion internalSpin = Quaternion.AngleAxis(moonSelfSpinAngle, moonSpinAxis.normalized);
                
                targetRotation = baseTrackLook * internalSpin;
            }
            else
            {
                // Simple standard look-forward flight
                targetRotation = Quaternion.LookRotation(tangentDirection, Vector3.up);
            }
        }

        private void Update()
        {
            if (!CurrentlyOrbiting) return;

            // Apply smoothed look vectors
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime));
        }

        private void InitOrbit()
        {
            if (CurrentlyOrbiting)
            {
                var sph = CurrentlyOrbiting.GetComponent<ProceduralSphere>();
                if (sph)
                {
                    planetRadius = sph.radius;
                }
                else
                {
                    if (Physics.Raycast(transform.position, (CurrentlyOrbiting.transform.position - transform.position).normalized, 
                            out RaycastHit hit, 1000f))
                    {
                        planetRadius = (transform.position - CurrentlyOrbiting.transform.position).magnitude - hit.distance;
                    }
                }
                
                targetDistance = planetRadius + altitude;
            }
        }

        public void FindNearestOrbitableObject()
        {
            var results = new Collider[100];
            var size = Physics.OverlapSphereNonAlloc(transform.position, 10000f, results, orbitalMask);
            if (size == 0) return;

            GameObject nearest = null;
            var minDistance = float.MaxValue;

            foreach (var c in results)
            {
                if (!c) continue;
                if (c.gameObject == gameObject) continue; // Skip self
                var distance = Vector3.Distance(transform.position, c.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = c.gameObject;
                }
            }

            CurrentlyOrbiting = nearest;

            InitOrbit(); 
        }

        private void InitializeAngle()
        {
            if (!CurrentlyOrbiting) return;

            Vector3 radialVector = transform.position - CurrentlyOrbiting.transform.position;
            Vector3 perpendicularPlaneVector = Vector3.ProjectOnPlane(radialVector, orbitAxis.normalized).normalized;
            
            Vector3 referenceDir = Vector3.Cross(orbitAxis.normalized, Vector3.up);
            if (referenceDir.sqrMagnitude < 0.001f) referenceDir = Vector3.Cross(orbitAxis.normalized, Vector3.forward);
            referenceDir.Normalize();

            currentAngle = Vector3.SignedAngle(referenceDir, perpendicularPlaneVector, orbitAxis.normalized) * Mathf.Deg2Rad;
        }
    }
}
