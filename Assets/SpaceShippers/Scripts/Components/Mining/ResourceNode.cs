using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ResourceNode : MonoBehaviour
{
    [SerializeField] public ItemTable _itemTable;
    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        _health.Changed += OnChanged;
        _health.Died += OnDied;
    }
    private void OnDisable()
    {
        _health.Changed -= OnChanged;
        _health.Died -= OnDied;
    }

    public void YieldHarvest(float damage, Inventory collector)
    {
        if (_health.IsDead) return;

        _health.TakeDamage(damage);

        if (_itemTable.TryRoll(out var item, out int amount))
            collector.Add(item, amount);
    }

    private void OnChanged(float currentHealth, float maxHealth)
    {
        // TODO: visually breaks / shrinks the more its damaged
    }

    private void OnDied()
    {
        Destroy(gameObject);
    }
}
