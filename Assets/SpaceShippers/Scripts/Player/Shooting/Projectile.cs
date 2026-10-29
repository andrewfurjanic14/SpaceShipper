using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private Rigidbody _rb;
    private float _damage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(int layer, float damage, float speed, float lifetime)
    {
        gameObject.layer = layer;
        _damage = damage;
        _rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifetime);
    }
    public void Initialize(int layer, Weapon weapon)
    {
        gameObject.layer = layer;
        _damage = weapon.Damage;
        _rb.linearVelocity = transform.forward * weapon.ProjectileSpeed;
        Destroy(gameObject, weapon.ProjectileLifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        var target = other.GetComponentInParent<DamageableEntity>();

        if (target != null)
        {
            target.TakeDamage(_damage);
        }
        Destroy(gameObject);
    }
}