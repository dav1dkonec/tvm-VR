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
    public float strengthMinValue = 0.01f;
    public float strengthMaxValue = 0.40f;

    private bool suppressSliderCallback;
    private bool IsLegacyRadiusControl => parameterKind == ParameterKind.Radius;

    private void Awake()
    {
        if (DisableLegacyRadiusControl())
            return;

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
        if (DisableLegacyRadiusControl())
            return;

        SyncVisuals();
    }

    private void OnEnable()
    {
        if (DisableLegacyRadiusControl())
            return;

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
        if (IsLegacyRadiusControl)
            return;

        if (target == null)
            return;

        SetValue(GetCurrentValue() + delta);
    }

    private void HandleSliderValueChanged(float value)
    {
        if (IsLegacyRadiusControl)
            return;

        if (suppressSliderCallback)
            return;

        SetValue(value);
    }

    private void SetValue(float value)
    {
        if (IsLegacyRadiusControl)
            return;

        if (target == null)
            return;

        GetRange(out float minValue, out float maxValue);
        value = Mathf.Clamp(value, minValue, maxValue);

        target.SetInflateStrength(value);

        SyncVisuals();
        controller?.RefreshSelectionPreview();
    }

    private float GetCurrentValue()
    {
        if (target == null)
            return 0f;

        return target.InflateStrength;
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
        minValue = strengthMinValue;
        maxValue = strengthMaxValue;
    }

    private bool DisableLegacyRadiusControl()
    {
        if (!IsLegacyRadiusControl)
            return false;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);

        return true;
    }

}
