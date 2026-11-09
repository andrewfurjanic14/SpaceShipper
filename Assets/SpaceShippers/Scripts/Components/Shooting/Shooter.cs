using UnityEngine;
using UnityEngine.InputSystem;

public class Shooter : MonoBehaviour, IAimSource
{
    [SerializeField] private Weapon _weapon;
    [SerializeField] private float _spreadDegrees = 0f;

    [Header("Ship Configs")]
    [SerializeField] private Transform[] _muzzles; 
    [SerializeField] private Transform _ship;
    [SerializeField] private bool _useShipForward = true;

    [Header("Optional: Trajectory Visualization")]
    [SerializeField] private TrajectoryPreview _trajectoryPreview;
    [SerializeField] private TrajectoryStyle _trajectoryStyle;

    // Runtime
    private int _projectileLayer;
    private float _nextFireTime;
    private bool _isFiring;

    // IAimSource Accessors
    public Transform Ship => _ship;
    public Transform[] Muzzles => _muzzles;
    public Quaternion GetAimRotation(Transform muzzle) => 
        _useShipForward ? _ship.rotation : muzzle.rotation;

    public float FireRateMultiplier = 1f;

    private void Awake()
    {
        if (_ship == null)
            _ship = transform.parent != null ? transform.parent : transform;

        if (_muzzles == null || _muzzles.Length == 0)
            _muzzles = new[] { transform };
        
        if (_trajectoryPreview == null)
            _trajectoryPreview = GetComponent<TrajectoryPreview>();

        if (_trajectoryPreview != null)
            _trajectoryPreview.Register(this, _trajectoryStyle);

        _projectileLayer = GetProjectileLayer();
    }

    private void OnDestroy()
    {
        if (_trajectoryPreview != null)
            _trajectoryPreview.Unregister(this);
    }

    public void StartFiring()
    {
        _isFiring = true;
        if (_trajectoryPreview != null)
            _trajectoryPreview.SetVisible(this, true);
    }

    public void StopFiring()
    {
        _isFiring = false;
        if (_trajectoryPreview != null)
            _trajectoryPreview.SetVisible(this, false);
    }

    private void Update()
    {
        if (_isFiring) TryFire();
    }

    // Random tilt for one bullet: a random point in a circle, used as yaw (x) and pitch (y)
    private Quaternion GetAimOffset()
    {
        Vector2 r = Random.insideUnitCircle * (_spreadDegrees + _weapon.SpreadDegrees);
        return Quaternion.Euler(r.y, r.x, 0f);
    }

    private void TryFire()
    {
        if (_weapon == null || _weapon.ProjectilePrefab == null) return;
        if (Time.time < _nextFireTime) return;

        _nextFireTime = Time.time + 1f / (_weapon.FireRate * FireRateMultiplier);

        foreach (var muzzle in _muzzles)
        {
            if (muzzle == null) continue;

            Quaternion rotation = GetAimRotation(muzzle) * GetAimOffset();

            Projectile p = Instantiate(_weapon.ProjectilePrefab, muzzle.position, rotation);
            p.Initialize(_projectileLayer, _weapon);
        }
    }

    private int GetProjectileLayer()
    {
        bool isPlayer = _ship.gameObject.layer == LayerMask.NameToLayer("Player");
        string layerName = isPlayer ? "PlayerProjectile" : "EnemyProjectile";
        
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) // NameToLayer returns -1 on failure
            Debug.LogError($"Layer '{layerName}' doesn't exist.", this);

        return layer;
    }
}