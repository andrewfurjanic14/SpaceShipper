using UnityEngine;

[RequireComponent(typeof(BarUI))]
public class EnergyBar : MonoBehaviour
{
    [SerializeField] private Energy _energy;
    private BarUI _barUI;

    private void Awake() => _barUI = GetComponent<BarUI>();

    private void OnEnable() => _energy.Changed += _barUI.Set;

    private void OnDisable() => _energy.Changed -= _barUI.Set;

    private void Start() => _barUI.Set(_energy.Current, _energy.Max); 
}