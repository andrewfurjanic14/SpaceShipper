using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private CanvasGroup _containerStatus;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private InventorySlotUI _slotPrefab;

    private readonly List<InventorySlotUI> _slots = new();
    private bool _needsRefresh = true;

    public bool IsOpen { get; private set; }

    private void OnEnable()
    {
        _inventory.Changed += OnInventoryChanged;
        SetOpen(false);
    }

    private void OnDisable()
    {
        _inventory.Changed -= OnInventoryChanged;
    }

    public void OnTogglePressed(InputAction.CallbackContext ctx)  
    {
        SetOpen(!IsOpen);
    }

    private void OnInventoryChanged()
    {
        _needsRefresh = true;
        if (IsOpen) 
            Refresh();
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        _containerStatus.alpha = open ? 1f : 0f;
        _containerStatus.interactable = open;
        _containerStatus.blocksRaycasts = open;

        if (open && _needsRefresh) 
            Refresh();
    }

    private void Refresh()
    {
        _needsRefresh = false;
        int i = 0;

        foreach (var (item, amount) in _inventory.Items)
        {
            if (i >= _slots.Count)
                _slots.Add(Instantiate(_slotPrefab, _slotContainer));

            _slots[i].gameObject.SetActive(true);
            _slots[i].Set(item, amount);
            i++;
        }

        // hide leftover slots (items that were removed)
        for (; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(false);
    }
}