using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflateModeUI : MonoBehaviour
{
    private static readonly Color SelectedTextColor = new(0.49019608f, 1f, 0.8784314f, 1f);
    private static readonly Color UnselectedTextColor = new(1f, 1f, 1f, 0.95f);

    public InflateDeflateUI controller;
    public EditingMethodRuntimeSettings target;
    public Button inflateButton;
    public Button deflateButton;

    private TMP_Text inflateText;
    private TMP_Text deflateText;

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<InflateDeflateUI>();

        if (target == null)
            target = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        inflateText = inflateButton != null ? inflateButton.GetComponentInChildren<TMP_Text>(true) : null;
        deflateText = deflateButton != null ? deflateButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (inflateButton != null)
            inflateButton.onClick.AddListener(SetInflateMode);

        if (deflateButton != null)
            deflateButton.onClick.AddListener(SetDeflateMode);

        MakeTextOnly(inflateButton);
        MakeTextOnly(deflateButton);
    }

    private void Start()
    {
        if (target != null)
            target.SetInflateMode(0);

        UpdateVisualState();
    }

    public void SetInflateMode()
    {
        if (target == null)
            return;

        target.SetInflateMode(0);
        UpdateVisualState();
        controller?.RefreshSelectionPreview();
    }

    public void SetDeflateMode()
    {
        if (target == null)
            return;

        target.SetInflateMode(1);
        UpdateVisualState();
        controller?.RefreshSelectionPreview();
    }

    private void UpdateVisualState()
    {
        if (target == null)
            return;

        var inflateSelected = target.InflateMode == InflateDeflateMode.Inflate;
        ApplyTextState(inflateText, inflateSelected);
        ApplyTextState(deflateText, !inflateSelected);
    }

    private static void ApplyTextState(TMP_Text text, bool selected)
    {
        if (text == null)
            return;

        text.color = selected ? SelectedTextColor : UnselectedTextColor;
        text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        text.raycastTarget = false;
    }

    private static void MakeTextOnly(Button button)
    {
        if (button == null)
            return;

        if (button.targetGraphic != null)
            button.targetGraphic.color = new Color(0f, 0f, 0f, 0f);

        var colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0f);
        colors.highlightedColor = new Color(0f, 0f, 0f, 0f);
        colors.pressedColor = new Color(0f, 0f, 0f, 0f);
        colors.selectedColor = new Color(0f, 0f, 0f, 0f);
        button.colors = colors;
    }
}
