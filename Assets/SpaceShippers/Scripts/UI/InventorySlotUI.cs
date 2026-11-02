using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _count;

    public void Set(Item item, int amount)
    {
        _icon.sprite = item.Icon;
        _name.text = item.Name;
        _count.text = $"x{amount}";
    }
}