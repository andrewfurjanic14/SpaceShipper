using UnityEngine;

[CreateAssetMenu(fileName = "ItemTable", menuName = "Scriptable Objects/ItemTable")]
public class ItemTable : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public Item Item; 
        public float Weight; 
        public int Amount;
    }

    [System.Serializable]
    public struct IntRange { public int Min; public int Max; }

    [SerializeField] private Entry[] _entries;
    public bool TryRoll(out Item item, out int amount)
    {
        float total = 0f;
        foreach (var entry in _entries) 
        {
            total += entry.Weight;
        }

        float roll = Random.value * total;
        foreach (var entry in _entries)
        {
            roll -= entry.Weight;
            if (roll <= 0f)
            {
                item = entry.Item;
                amount = entry.Amount;
                return true;
            }
        }
        item = null; amount = 0; return false;
    }
}
