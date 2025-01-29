using UnityEngine;
using UnityEngine.UI;

public class SliderLag : MonoBehaviour
{
	public Slider slider;
    private Slider _slider;
    private float _value;
    private float _targetValue;

    public void Start()
    {
        _slider = GetComponent<Slider>();
    }

    public void Update()
    {
        // Interpolate _slider.value to slider.value
        _targetValue = slider.value;
        _value = Mathf.Lerp(_value, _targetValue, Time.deltaTime * 10f);
        _slider.value = _value;

    }
}
