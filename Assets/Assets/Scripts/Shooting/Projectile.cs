using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 4f;
    private Rigidbody _rb;
    private float _damage;
    private uint _teamID;
    private Transform _ownerRoot;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(float damage, float speed, uint teamID, Transform ownerRoot)
    {
        _damage = damage;
        _teamID = teamID;
        _ownerRoot = ownerRoot;
        _rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_ownerRoot != null && other.transform.root == _ownerRoot) return;

        if (other.isTrigger) return;

        var target = other.GetComponentInParent<DamageableEntity>();

        if (target != null)
        {
            if (target.TeamID == _teamID) return;

            target.TakeDamage(_damage);
        }
        Destroy(gameObject);
    }
}