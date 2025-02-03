using UnityEngine;
using UnityEngine.UI;

public class SliderLag : MonoBehaviour
{
	public Slider slider;
    private Slider _slider;
    private float _value;
    private float _targetValue;

    [SerializeField] private float _lag = 10f;

    public void Start()
    {
        _slider = GetComponent<Slider>();
    }

    public void Update()
    {
        // Interpolate _slider.value to slider.value
        _targetValue = slider.value;
        _value = Mathf.Lerp(
            _value, 
            _targetValue,
            1f - Mathf.Exp(-_lag * Time.deltaTime)
        );
        _slider.value = _value;

    }
}
