using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private Transform[] _muzzles;

    [SerializeField] private bool _useShipForward = true;

    [SerializeField] private float _fireRate = 3f; 
    [SerializeField] private float _projectileSpeed = 60f;
    [SerializeField] private float _damage = 10f;

    private DamageableEntity _owner;
    private Transform _ship;
    private float _nextFireTime;

    private void Awake()
    {
        _owner = GetComponentInParent<DamageableEntity>();

        var movement = GetComponentInParent<MovementScript>();
        _ship = movement != null ? movement.transform : transform.root;

        if (_muzzles == null || _muzzles.Length == 0)
            _muzzles = new[] { transform };
    }

    public bool TryFire()
    {
        if (_projectilePrefab == null)
        {
            return false;
        }

        if (Time.time < _nextFireTime) return false;
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