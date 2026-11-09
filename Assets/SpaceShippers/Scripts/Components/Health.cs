using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _current;
    [SerializeField] private ItemTable _debrisTable;

    public event Action<float, float> Changed; // current, max

    public event Action Died;

    public bool IsDead { get; private set; }

    public void ScaleMaxHealth(float mult) {_maxHealth *= mult; _current = _maxHealth;}

    private void Awake()
    {
        _current = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        _current = Mathf.Max(0f, _current - amount);

        // Changed
        if (Changed != null)
            Changed.Invoke(_current, _maxHealth);

        // Died
        if (_current <= 0f)
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
            _current = _maxHealth;
        }
    }
}