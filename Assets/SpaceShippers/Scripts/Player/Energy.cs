using System;
using UnityEngine;
public class Energy : MonoBehaviour
{
    [SerializeField] private float _max;
    [SerializeField] private float _rechargeRate;
    public float Current { get; private set; }
    public float Max => _max;

    public event Action<float, float> Changed; // Current, Max

    private void Awake()
    {
        Current = _max;
    }

    private void Update()
    {
        if (Current >= _max) return;
        SetCurrent(Current + _rechargeRate * Time.deltaTime);
    }

    public bool TrySpend(float amount)
    {
        if (Current < amount) return false;
        SetCurrent(Current - amount);
        return true;
    }
    public void Gain(float amount) => SetCurrent(Current - amount);
    public void SetCurrent(float current)
    {
        Current = Mathf.Clamp(current, 0f, _max);
        Changed?.Invoke(Current, _max);
    }

    public void SetMax(float max)
    {
        _max = Mathf.Max(max, 0.1f);
        Changed?.Invoke(Current, _max);
    }
}
