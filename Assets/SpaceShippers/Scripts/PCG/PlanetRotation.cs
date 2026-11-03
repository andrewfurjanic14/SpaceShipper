using UnityEngine;

namespace SpaceShippers.Scripts.PCG
{
    public class PlanetRotation : MonoBehaviour
    {
        [Header("Axial Spin")]
        [SerializeField] private float rotationSpeed = 2f; // Degrees per second
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        private void Update()
        {
            // Rotates the planet cleanly around its local or world directional axis
            transform.Rotate(rotationAxis.normalized * (rotationSpeed * Time.deltaTime), Space.Self);
        }
    }
}