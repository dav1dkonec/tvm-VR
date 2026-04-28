using TMPro;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflateFloatStepperUI : MonoBehaviour
{
    public enum ParameterKind
    {
        Radius,
        Strength
    }

    public InflateDeflateUI controller;
    public EditingMethodRuntimeSettings target;
    public ParameterKind parameterKind;
    public float step = 0.01f;
    public TMP_Text text;
    public Button minus;
    public Button plus;
    public Slider slider;
    public float radiusMinValue = 0.02f;
    public float radiusMaxValue = 0.30f;
    public float strengthMinValue = 0.01f;
    public float strengthMaxValue = 0.40f;

    private bool suppressSliderCallback;

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<InflateDeflateUI>();

        if (target == null)
            target = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        if (minus != null)
            minus.onClick.AddListener(Decrease);

        if (plus != null)
            plus.onClick.AddListener(Increase);

        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
            slider.onValueChanged.AddListener(HandleSliderValueChanged);
        }
    }

    private void Start()
    {
        SyncVisuals();
    }

    private void OnEnable()
    {
        SyncVisuals();
    }

    public void Decrease()
    {
        ChangeValue(-step);
    }

    public void Increase()
    {
        ChangeValue(step);
    }

    private void ChangeValue(float delta)
    {
        if (target == null)
            return;

        SetValue(GetCurrentValue() + delta);
    }

    private void HandleSliderValueChanged(float value)
    {
        if (suppressSliderCallback)
            return;

        SetValue(value);
    }

    private void SetValue(float value)
    {
        if (target == null)
            return;

        GetRange(out float minValue, out float maxValue);
        value = Mathf.Clamp(value, minValue, maxValue);

        if (parameterKind == ParameterKind.Radius)
            target.SetInflateRadius(value);
        else
            target.SetInflateStrength(value);

        SyncVisuals();
        controller?.RefreshSelectionPreview();
    }

    private float GetCurrentValue()
    {
        if (target == null)
            return 0f;

        return parameterKind == ParameterKind.Radius
            ? target.InflateRadius
            : target.InflateStrength;
    }

    private void SyncVisuals()
    {
        if (target == null)
            return;

        float value = GetCurrentValue();

        if (text != null)
        {
            text.text = $"{value:0.00}";
            text.raycastTarget = false;
        }

        if (slider != null)
        {
            GetRange(out float minValue, out float maxValue);
            suppressSliderCallback = true;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, minValue, maxValue));
            suppressSliderCallback = false;
        }
    }

    private void GetRange(out float minValue, out float maxValue)
    {
        if (parameterKind == ParameterKind.Radius)
        {
            minValue = radiusMinValue;
            maxValue = radiusMaxValue;
            return;
        }

        minValue = strengthMinValue;
        maxValue = strengthMaxValue;
    }

}
