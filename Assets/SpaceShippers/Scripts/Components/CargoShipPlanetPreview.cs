using UnityEngine;
using UnityEngine.InputSystem;

public class CargoShipPlanetPreview : MonoBehaviour
{
    private static readonly Color PreviewColor = new Color(1f, 0.5f, 0f, 1f);
    private static readonly Color TargetColor = Color.green;

    [SerializeField] private CargoShipMovement _cargoShip;
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private float _length = 100f;
    [SerializeField] private float _width = 0.1f;
    [SerializeField] private float _parkingClearance = 2.5f;

    private LineRenderer _line;
    private Material _runtimeLineMaterial;
    private SphereGen _targetPlanet;
    private RaycastHit _targetHit;
    private bool _isPreviewing;

    private void Start()
    {
        Debug.Log("Hold G to preview the cargo-ship route; release G while aiming at a planet to send it there.", this);
    }

    private void Awake()
    {
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

        GameObject lineObject = new GameObject("Cargo Ship Planet Preview Line");
        lineObject.transform.SetParent(transform, false);

        _runtimeLineMaterial = new Material(_lineMaterial);
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
        if (_runtimeLineMaterial != null)
            Destroy(_runtimeLineMaterial);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.gKey.wasPressedThisFrame)
        {
            _isPreviewing = true;
            _line.enabled = true;
        }

        if (!_isPreviewing)
            return;

        UpdatePreviewLine();

        if (keyboard.gKey.wasReleasedThisFrame)
        {
            if (_targetPlanet != null)
                _cargoShip.MoveToPlanet(_targetHit.point + _targetHit.normal * _parkingClearance);

            _isPreviewing = false;
            _line.enabled = false;
            _targetPlanet = null;
        }
    }

    private void UpdatePreviewLine()
    {
        Vector3 origin = transform.position + transform.forward;
        Vector3 direction = transform.forward;
        Vector3 end = origin + direction * _length;

        _targetPlanet = null;
        if (Physics.Raycast(origin, direction, out RaycastHit hit, _length, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            _targetPlanet = hit.collider.GetComponentInParent<SphereGen>();
            if (_targetPlanet != null)
                _targetHit = hit;
        }

        Color lineColor = _targetPlanet != null ? TargetColor : PreviewColor;
        _line.startColor = lineColor;
        _line.endColor = lineColor;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, end);
    }
}
