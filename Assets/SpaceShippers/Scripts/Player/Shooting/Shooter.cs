using UnityEngine;

public class Shooter : MonoBehaviour
{
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform[] _muzzles;

    [SerializeField] private bool _useShipForward = true;

    [SerializeField] private float _fireRate = 3f; 
    [SerializeField] private float _projectileSpeed = 60f;
    [SerializeField] private float _damage = 10f;

    [SerializeField] private float _trajectoryLength = 100f;
    [SerializeField] private float _trajectoryWidth = 0.03f;
    [SerializeField] private Color _trajectoryColor = new Color(0.2f, 0.9f, 1f, 0.4f);

    private DamageableEntity _owner;
    private Transform _ship;
    private float _nextFireTime;
    private LineRenderer[] _trajectoryLines;
    private Material _trajectoryMaterial;
    private bool _showTrajectory;

    private void Awake()
    {
        _owner = GetComponentInParent<DamageableEntity>();

        var movement = GetComponentInParent<PlayerMovement>();
        _ship = movement != null ? movement.transform : transform.root;

        if (_muzzles == null || _muzzles.Length == 0)
            _muzzles = new[] { transform };

        CreateTrajectoryLines();
    }

    private void Update()
    {
        if (!_showTrajectory || _trajectoryLines == null) return;

        UpdateTrajectoryLines();
    }

    private void UpdateTrajectoryLines()
    {
        for (int i = 0; i < _trajectoryLines.Length; i++)
        {
            Transform muzzle = _muzzles[i];
            LineRenderer line = _trajectoryLines[i];
            if (muzzle == null || line == null) continue;

            Quaternion rotation = _useShipForward ? _ship.rotation : muzzle.rotation;
            Vector3 start = _ship.InverseTransformPoint(muzzle.position);
            Vector3 direction = _ship.InverseTransformDirection(rotation * Vector3.forward);
            line.SetPosition(0, start);
            line.SetPosition(1, start + direction * _trajectoryLength);
        }
    }

    private void OnDestroy()
    {
        if (_trajectoryMaterial != null)
            Destroy(_trajectoryMaterial);
    }

    public void SetTrajectoryVisible(bool visible)
    {
        _showTrajectory = visible;

        if (_trajectoryLines == null) return;

        if (visible)
            UpdateTrajectoryLines();

        foreach (LineRenderer line in _trajectoryLines)
        {
            if (line != null)
                line.enabled = visible;
        }
    }

    private void CreateTrajectoryLines()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogError("Could not find the Universal Render Pipeline/Unlit shader for the weapon trajectory preview.", this);
            return;
        }

        _trajectoryMaterial = new Material(shader);
        _trajectoryMaterial.SetColor("_BaseColor", Color.white);
        _trajectoryMaterial.SetFloat("_Surface", 1f);
        _trajectoryMaterial.SetFloat("_Blend", 0f);
        _trajectoryMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _trajectoryMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _trajectoryMaterial.SetFloat("_ZWrite", 0f);
        _trajectoryMaterial.SetOverrideTag("RenderType", "Transparent");
        _trajectoryMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        _trajectoryMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _trajectoryLines = new LineRenderer[_muzzles.Length];

        for (int i = 0; i < _muzzles.Length; i++)
        {
            if (_muzzles[i] == null) continue;

            var lineObject = new GameObject($"Trajectory Preview {i + 1}");
            lineObject.transform.SetParent(_ship, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = _trajectoryWidth;
            line.endWidth = _trajectoryWidth;
            line.startColor = _trajectoryColor;
            line.endColor = _trajectoryColor;
            line.sharedMaterial = _trajectoryMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            _trajectoryLines[i] = line;
        }
    }

    public bool TryFire()
    {
        // NO fire
        if (_projectilePrefab == null || Time.time < _nextFireTime)
        {
            return false;
        }
            
        _nextFireTime = Time.time + 1f / _fireRate; 

        uint team = _owner != null ? _owner.TeamID : 0;

        foreach (var muzzle in _muzzles)
        {
            if (muzzle == null) continue;

            Quaternion rotation = _useShipForward ? _ship.rotation : muzzle.rotation;
            Projectile p = Instantiate(_projectilePrefab, muzzle.position, rotation);
            p.Initialize(_damage, _projectileSpeed, team, transform.root);
        }
        return true;
    }
}