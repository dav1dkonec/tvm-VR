using TMPro;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Float stepper for inflate/deflate parameters.
/// </summary>
public class InflateDeflateFloatStepperUI : MonoBehaviour
{
    /// <summary>
    /// Inflate/deflate panel controller.
    /// </summary>
    public InflateDeflateUI controller;

    /// <summary>
    /// Runtime editing settings.
    /// </summary>
    public EditingMethodRuntimeSettings target;

    /// <summary>
    /// Step size.
    /// </summary>
    public float step = 0.01f;

    /// <summary>
    /// Value label.
    /// </summary>
    public TMP_Text text;

    /// <summary>
    /// Decrease button.
    /// </summary>
    public Button minus;

    /// <summary>
    /// Increase button.
    /// </summary>
    public Button plus;

    /// <summary>
    /// Value slider.
    /// </summary>
    public Slider slider;

    /// <summary>
    /// Minimum strength value.
    /// </summary>
    public float strengthMinValue = 0.01f;

    /// <summary>
    /// Maximum strength value.
    /// </summary>
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

    /// <summary>
    /// Decreases the value.
    /// </summary>
    public void Decrease()
    {
        ChangeValue(-step);
    }

    /// <summary>
    /// Increases the value.
    /// </summary>
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

}
