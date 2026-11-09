using UnityEngine;

[RequireComponent(typeof(BarUI))]
public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health _health;
    private BarUI _barUI;

    private void Awake() => _barUI = GetComponent<BarUI>();

    private void OnEnable() => _health.Changed += _barUI.Set;

    private void OnDisable() => _health.Changed -= _barUI.Set;

    private void Start() => _barUI.Set(_health.Current, _health.Max); 
}
