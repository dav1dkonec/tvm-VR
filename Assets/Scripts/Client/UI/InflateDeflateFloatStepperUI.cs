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
    }

    private void Start()
    {
        UpdateText();
    }

    public void Decrease()
    {
        if (controller != null && !controller.CanChangeParameters())
            return;

        ChangeValue(-step);
    }

    public void Increase()
    {
        if (controller != null && !controller.CanChangeParameters())
            return;

        ChangeValue(step);
    }

    private void ChangeValue(float delta)
    {
        if (target == null)
            return;

        if (parameterKind == ParameterKind.Radius)
            target.SetInflateRadius(target.InflateRadius + delta);
        else
            target.SetInflateStrength(target.InflateStrength + delta);

        UpdateText();
    }

    private void UpdateText()
    {
        if (text == null || target == null)
            return;

        var value = parameterKind == ParameterKind.Radius
            ? target.InflateRadius
            : target.InflateStrength;

        text.text = $"{value:0.00}";
        text.raycastTarget = false;
    }
}
