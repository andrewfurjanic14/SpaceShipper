using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Health : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 100f;

    public event Action<float, float> Changed; // current, max
    public event Action Died;

    public float Current { get; private set; }
    public float Max => _maxHealth;
    public bool IsDead { get; private set; }
    
    private void Awake()
    {
        Current = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke(Current, _maxHealth);

        if (Current <= 0f)
        {
            IsDead = true;
            
            if (Died != null) 
                Died.Invoke();
            else 
                Destroy(gameObject); // nobody handled it, so default to removal
        }
    }
}