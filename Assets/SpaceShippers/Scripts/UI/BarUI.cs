using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BarUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TMP_Text _label;

    private void Awake()
    {
        if (_slider == null)
            _slider = GetComponent<Slider>();
    }

    public void Set(float current, float max)
    {
        _slider.maxValue = max;
        _slider.value = current;

        if (_label != null)
            _label.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
