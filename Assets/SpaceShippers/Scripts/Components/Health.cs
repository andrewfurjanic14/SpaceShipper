using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Health : MonoBehaviour
{
    [SerializeField] private float _max = 100f;
    [SerializeField] private ItemTable _debrisTable;

    public event Action<float, float> Changed; // Current, Max
    public event Action Died;

    public bool IsDead { get; private set; }
    public float Current { get; private set; }
    public float Max => _max;
    
    private void Awake()
    {
        Current = _max;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);

        // Changed
        if (Changed != null)
            Changed.Invoke(Current, _max);

        // Died
        if (Current <= 0f)
            Die();
    }

    private void Die()
    {
        IsDead = true;
            
        if (Died != null) 
            Died.Invoke();
        else 
            Destroy(gameObject); // nobody handled it, so default to removal
    }
    
    public void MakeDebris()
    {
        gameObject.layer = LayerMask.NameToLayer("Debris");
        if (_debrisTable)
        {
            var r = gameObject.AddComponent<ResourceNode>();
            r._itemTable = _debrisTable;
            IsDead = false; 
            Current = _max;
        }
    }
}