using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Dictionary<Item, int> _items;

    private void Awake()
    {
        if (_items == null)
            _items = new();
    }

    public void Add(Item item, int amount)
    {
        _items.TryGetValue(item, out int current);
        _items[item] = current + amount;
        Debug.Log($"+{amount}{item}");
    }
}
