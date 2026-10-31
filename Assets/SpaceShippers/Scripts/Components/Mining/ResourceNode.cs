using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class ResourceNode : MonoBehaviour
{
    [SerializeField] private ItemTable _itemTable;
    private Health _health;
    public static event Action<Item, int> Harvested; // item, amount

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

    private void OnChanged(float currentHealth, float maxHealth)
    {
        if (_itemTable.TryRoll(out var item, out int amount))
            Harvested?.Invoke(item, amount);
    }

    private void OnDied()
    {
        Destroy(gameObject);
    }
}
