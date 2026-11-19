using SpaceShippers.Scripts.PCG;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Displays a line from this object's forward direction while G is held. When G is
/// released, a valid planet hit is converted to a parking position and sent to the
/// configured cargo ship.
/// </summary>
public class CargoShipPlanetPreview : MonoBehaviour
{
    // Orange marks an aim with no recognized planet; green means releasing G will launch the ship.
    private static readonly Color PreviewColor = new Color(1f, 0.5f, 0f, 1f);
    private static readonly Color TargetColor = Color.green;

    // The ship receives the selected planet and the parking point computed from the raycast hit.
    [SerializeField] private CargoShipMovement _cargoShip;
    // A material asset is required; Awake creates and later destroys a private runtime copy.
    [SerializeField] private Material _lineMaterial;
    // The preview ray is limited to this distance; the line width is in world units.
    [SerializeField] private float _length = 100f;
    [SerializeField] private float _width = 0.1f;
    // Offset from the surface hit along its normal, preventing the ship from targeting the surface itself.
    [SerializeField] private float _parkingClearance = 2.5f;

    private LineRenderer _line;
    private Material _runtimeLineMaterial;
    // The Transform is passed to CargoShipMovement so the parked ship can follow the planet.
    private Transform _targetPlanet;
    // Store the most recent valid planet hit to derive the surface-relative parking point on release.
    private RaycastHit _targetHit;
    private bool _isPreviewing;

    private void Start()
    {
        // Keep the hold-to-preview and release-to-launch controls discoverable in play mode.
        Debug.Log("Hold G to preview the cargo-ship route; release G while aiming at a planet to send it there.", this);
    }

    private void Awake()
    {
        // These references are needed before Update can display a line or issue a movement command.
        if (_cargoShip == null)
        {
            Debug.LogError("Assign the cargo ship movement component to the planet preview.", this);
            enabled = false;
            return;
        }

        if (_lineMaterial == null)
        {
            Debug.LogError("Assign a line material to the planet preview.", this);
            enabled = false;
            return;
        }

        // Keep the generated line under this object for hierarchy organization, but render its points in world space.
        GameObject lineObject = new GameObject("Cargo Ship Planet Preview Line");
        lineObject.transform.SetParent(transform, false);

        // Use a private material instance so preview color setup does not modify the shared asset.
        _runtimeLineMaterial = new Material(_lineMaterial);
        // Reset shader tint properties so the LineRenderer's start/end colors control the visible preview color.
        if (_runtimeLineMaterial.HasProperty("_BaseColor"))
            _runtimeLineMaterial.SetColor("_BaseColor", Color.white);
        if (_runtimeLineMaterial.HasProperty("_Color"))
            _runtimeLineMaterial.SetColor("_Color", Color.white);

        _line = lineObject.AddComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.positionCount = 2;
        _line.startWidth = _line.endWidth = _width;
        _line.sharedMaterial = _runtimeLineMaterial;
        _line.startColor = PreviewColor;
        _line.endColor = PreviewColor;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;
        _line.enabled = false;
    }

    private void OnDestroy()
    {
        // Materials created at runtime are not automatically destroyed with their source asset.
        if (_runtimeLineMaterial != null)
            Destroy(_runtimeLineMaterial);
    }

    private void Update()
    {
        // The preview is driven directly by the active keyboard; there is nothing to update without one.
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.gKey.isPressed)
        {
            // Enable the line only once per preview session, then refresh it every frame to track the aim.
            if (!_isPreviewing)
            {
                _isPreviewing = true;
                _line.enabled = true;
            }

            UpdatePreviewLine();
            return;
        }

        // Recast once on release so the selection and parking point use the final aim direction.
        if (!_isPreviewing)
            return;

        UpdatePreviewLine();

        if (_targetPlanet != null)
        {
            // Offset the surface hit along its normal to keep the ship clear of the planet's collider.
            _cargoShip.MoveToPlanet(
                _targetPlanet,
                _targetHit.point + _targetHit.normal * _parkingClearance);
        }

        // End the preview whether or not a planet was hit; an invalid aim does not launch the ship.
        _isPreviewing = false;
        _line.enabled = false;
        _targetPlanet = null;
    }

    /// <summary>
    /// Casts forward from this object's position, updates the line endpoint, and records
    /// a target only when the hit belongs to a supported planet component.
    /// </summary>
    private void UpdatePreviewLine()
    {
        // Move the ray start slightly forward so it does not begin inside this object's own collider.
        Vector3 origin = transform.position + transform.forward;
        Vector3 direction = transform.forward;
        // If nothing is hit, show the full preview range.
        Vector3 end = origin + direction * _length;

        // Clear the previous selection first so aiming away from a planet cannot reuse a stale target.
        _targetPlanet = null;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _length, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            // The visual line ends at any collider hit, but only recognized planet objects are valid launch targets.
            end = hit.point;
            // Colliders may be on child objects, so search their parent hierarchy for either supported planet type.
            SphereGen generatedPlanet = hit.collider.GetComponentInParent<SphereGen>();
            GravField scenePlanet = hit.collider.GetComponentInParent<GravField>();
            _targetPlanet = generatedPlanet != null
                ? generatedPlanet.transform
                : scenePlanet != null ? scenePlanet.transform : null;
            if (_targetPlanet != null)
            {
                // Preserve the hit point and normal only for a valid target; these define its parking offset.
                _targetHit = hit;
            }
        }

        // Color both ends consistently to communicate whether releasing G will issue a route.
        Color lineColor = _targetPlanet != null ? TargetColor : PreviewColor;
        _line.startColor = lineColor;
        _line.endColor = lineColor;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, end);
    }
}
