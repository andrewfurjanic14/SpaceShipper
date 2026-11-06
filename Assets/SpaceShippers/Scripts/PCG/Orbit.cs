using System;
using UnityEngine;

namespace SpaceShippers.Scripts.PCG
{
    public class Orbit : MonoBehaviour
    {
        [Header("Orbital Mechanics")]
        [SerializeField] private float moveSpeed = 8f;
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
        
        [SerializeField]
        private GameObject _currentlyOrbiting;

        public GameObject CurrentlyOrbiting
        {
            get => _currentlyOrbiting;
            set
            {
                _currentlyOrbiting = value; 
                if (value)
                {
                    InitOrbit();
                    InitializeAngle();
                }
            }
        }

        private Rigidbody rb;

        private float currentAngle = 0f;
        private float targetDistance = 10f;
        private Quaternion targetRotation;
        private float moonSelfSpinAngle = 0f;
        [SerializeField] private float mass = 1.0f; 

        private void Awake()
        {
            InitializeRigidbody();
        }

        private void InitializeRigidbody()
        {
            if (!rb)
            {
                rb = GetComponent<Rigidbody>();
                if (!rb) rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
            }
        }

        private void OnValidate()
        {
            CurrentlyOrbiting = _currentlyOrbiting;
        }

        private void Start()
        {
            InitializeRigidbody();
            if (!CurrentlyOrbiting) FindNearestOrbitableObject();
            else 
            {
                InitOrbit();
                InitializeAngle();
            }
        }

        private void FixedUpdate()
        {
            InitializeRigidbody();

            if (refreshCount == 0)
            {
                refreshCount = refreshInterval;
                if (!CurrentlyOrbiting)
                {
                    FindNearestOrbitableObject();
                    if (!CurrentlyOrbiting) return;
                }
            }
            else
            {
                refreshCount--;
            }

            if (!CurrentlyOrbiting) return;

            // Advance the orbit loop path angle
            float angularVelocity = (targetDistance > 0.001f) ? (moveSpeed / targetDistance) : 0f;
            currentAngle += angularVelocity * Time.fixedDeltaTime;

            // Compute path position coordinates using a consistent orthonormal basis
            Vector3 planetCenter = CurrentlyOrbiting.transform.position;
            Vector3 normAxis = orbitAxis.normalized;
            
            Vector3 planeRight = Vector3.Cross(normAxis, Vector3.up);
            if (planeRight.sqrMagnitude < 0.001f) planeRight = Vector3.Cross(normAxis, Vector3.forward);
            planeRight.Normalize();
            
            // Fixed: Correct cross-product order matching SignedAngle's right-hand rule (Axis x Right)
            Vector3 planeForward = Vector3.Cross(normAxis, planeRight).normalized;

            Vector3 orbitalOffset = (planeRight * Mathf.Cos(currentAngle) + planeForward * Mathf.Sin(currentAngle)) * targetDistance;
            Vector3 desiredPosition = planetCenter + orbitalOffset;

            if (rb)
            {
                rb.MovePosition(desiredPosition);

                // Keep velocity vectors matched up
                Vector3 tangentDirection = (-planeRight * Mathf.Sin(currentAngle) + planeForward * Mathf.Cos(currentAngle)).normalized;
                rb.linearVelocity = tangentDirection * moveSpeed;

                // Calculate core directional face targets
                Vector3 surfaceNormal = (desiredPosition - planetCenter).normalized;

                if (tidallyLocked)
                {
                    targetRotation = Quaternion.LookRotation(tangentDirection, surfaceNormal);
                }
                else if (customMoonSpin)
                {
                    moonSelfSpinAngle += moonSpinSpeed * Time.fixedDeltaTime;
                    
                    Quaternion baseTrackLook = Quaternion.LookRotation(tangentDirection, surfaceNormal);
                    Quaternion internalSpin = Quaternion.AngleAxis(moonSelfSpinAngle, moonSpinAxis.normalized);
                    
                    targetRotation = baseTrackLook * internalSpin;
                }
                else
                {
                    targetRotation = Quaternion.LookRotation(tangentDirection, Vector3.up);
                }
            }
        }

        private void Update()
        {
            if (!CurrentlyOrbiting || !rb) return;

            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime));
        }

        private void InitOrbit()
        {
            if (CurrentlyOrbiting)
            {
                InitializeRigidbody();

                // Capture current distance seamlessly as our target orbit radius from the new parent
                targetDistance = Vector3.Distance(transform.position, CurrentlyOrbiting.transform.position);

                if (rb)
                {
                    float currentSpeed = rb.linearVelocity.magnitude;
                    if (currentSpeed > 0.1f)
                    {
                        moveSpeed = Mathf.Lerp(moveSpeed, currentSpeed, 0.5f);
                    }
                }

                var sph = CurrentlyOrbiting.GetComponent<ProceduralSphere>();
                if (sph)
                {
                    planetRadius = sph.radius;
                }
                else
                {
                    if (Physics.Raycast(transform.position, (CurrentlyOrbiting.transform.position - transform.position).normalized, out RaycastHit hit, 1000f))
                    {
                        planetRadius = Mathf.Max(0.1f, (transform.position - CurrentlyOrbiting.transform.position).magnitude - hit.distance);
                    }
                }
                
                var health = CurrentlyOrbiting.GetComponent<Health>();
                if (health)
                {
                    health.Died += HandleTargetDeath;
                }
            }
        }

        private void HandleTargetDeath()
        {
            if (CurrentlyOrbiting)
            {
                var health = CurrentlyOrbiting.GetComponent<Health>();
                if (health)
                {
                    health.Died -= HandleTargetDeath;
                }
            }
            CurrentlyOrbiting = null;
        }

        private void FindNearestOrbitableObject()
        {
            var results = new Collider[100];
            var size = Physics.OverlapSphereNonAlloc(transform.position, 10000f, results, orbitalMask);
            if (size == 0) return;

            GameObject nearest = null;
            var minDistance = float.MaxValue;

            foreach (var c in results)
            {
                if (!c) continue;
                if (c.gameObject == gameObject) continue; 
                var orbitComponent = c.GetComponent<Orbit>();
                if (orbitComponent && orbitComponent.mass <= 2f * mass) continue; 
                
                var distance = Vector3.Distance(transform.position, c.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = c.gameObject;
                }
            }

            CurrentlyOrbiting = nearest;
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