using System;
using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Gravity))]
[RequireComponent(typeof(Health))]
public class GravityDeath : MonoBehaviour
{
    private Gravity _gravity;
    private Health _health;
    private void Awake()
    {
        _gravity = GetComponent<Gravity>();
        _health = GetComponent<Health>();
    }

    private void OnEnable() => _health.Died += OnDied;
    private void OnDisable() => _health.Died -= OnDied;

    private void OnDied()
    {
        Debug.Log($"{gameObject} died");
         _gravity.IsAffected = true;
    }
}
