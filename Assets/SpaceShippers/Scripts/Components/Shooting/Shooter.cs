using UnityEngine;
using UnityEngine.InputSystem;

public class Shooter : MonoBehaviour
{
    [SerializeField] private Weapon _weapon;
    [SerializeField] private TrajectoryPreview _trajectoryPreview;
    [SerializeField] private Transform[] _muzzles;
    [SerializeField] private Transform _ship;
    [SerializeField] private bool _useShipForward = true;
    // Each bullet can be off by up to this many degrees in any direction
    [SerializeField] private float _spreadDegrees = 10f;

    private int _projectileLayer;
    private float _nextFireTime;
    private bool _isFiring;

    public Transform Ship => _ship;
    private Collider[] _ownColliders;

    public Transform[] Muzzles => _muzzles;

    private void Awake()
    {
        _projectileLayer = GetProjectileLayer();

        if (_trajectoryPreview == null)
            _trajectoryPreview = GetComponentInChildren<TrajectoryPreview>();

        if (_ship == null)
        _ship = transform.parent != null ? transform.parent : transform;

        _ownColliders = _ship.GetComponentsInChildren<Collider>(); 

        if (_muzzles == null || _muzzles.Length == 0)
            _muzzles = new[] { transform };
    }

    private void OnDisable() => StopFiring();

    private void Update()
    {
        if (_isFiring) TryFire();
    }

    public void StartFiring()
    {
        _isFiring = true;
        if (_trajectoryPreview != null)
            _trajectoryPreview.enabled = true;
    }

    public void StopFiring()
    {
        _isFiring = false;
        if (_trajectoryPreview != null)
            _trajectoryPreview.enabled = false;
    }

    //random tilt for one bullet: a random point in a circle, used as yaw (x) and pitch (y)
    private Quaternion GetAimOffset()
    {
        Vector2 r = Random.insideUnitCircle * _spreadDegrees;
        return Quaternion.Euler(r.y, r.x, 0f);
    }

    private void TryFire()
    {
        if (_weapon == null || _weapon.ProjectilePrefab == null) return;
        if (Time.time < _nextFireTime) return;
            
        _nextFireTime = Time.time + 1f / _weapon.FireRate; 

        foreach (var muzzle in _muzzles)
        {
            if (muzzle == null) continue;

            Quaternion rotation = GetAimRotation(muzzle) * GetAimOffset();

            Projectile p = Instantiate(_weapon.ProjectilePrefab, muzzle.position, rotation);
            p.Initialize(_projectileLayer, _weapon);

            // So that the player or enemy cant get hit by their own bullet
            var bulletCol = p.GetComponent<Collider>();
            foreach (var c in _ownColliders)
            {
                Physics.IgnoreCollision(bulletCol, c);
            }
        }
    }

    #region Helpers
    private int GetProjectileLayer()
    {
        bool isPlayer = gameObject.layer == LayerMask.NameToLayer("Player");
        string layerName = isPlayer ? "PlayerProjectile" : "EnemyProjectile";
        
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0) // NameToLayer returns -1 on failure
            Debug.LogError($"Layer '{layerName}' doesn't exist.", this);

        return layer;
    }

    public Quaternion GetAimRotation(Transform muzzle)
    {
        return _useShipForward ? _ship.rotation : muzzle.rotation;
    }
    #endregion
}