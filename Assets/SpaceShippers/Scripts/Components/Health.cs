using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _current;

    public event Action<float, float> Changed; // current, max
    public event Action Died;

    public bool IsDead { get; private set; }
    
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
}