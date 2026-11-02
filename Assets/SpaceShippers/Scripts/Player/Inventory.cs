using System;
using System.Collections.Generic;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private readonly Dictionary<Item, int> _items = new();
    public Dictionary<Item, int> Items => _items;
    public event Action Changed;

    public void Add(Item item, int amount)
    {
        if (item == null || amount <= 0) return;

        _items.TryGetValue(item, out int current);
        _items[item] = current + amount;
        Changed?.Invoke();

        Debug.Log($"+{amount} {item}");
    }

    public bool Remove(Item item, int amount)
    {
        if (item == null) return false;

        _items.TryGetValue(item, out int current);

        if (amount > current) return false;

        _items[item] = current - amount;
        Changed?.Invoke();
        
        Debug.Log($"-{amount} {item}");
        return true;
    }
}
