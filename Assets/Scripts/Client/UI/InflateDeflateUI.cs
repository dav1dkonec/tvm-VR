using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflateUI : MonoBehaviour
{
    private const string PanelName = "Inflate Deflate Panel";

    private EditingMethodRuntimeSettings target;
    private ActivateTeleportationRay teleportRay;
    private TMP_Text radiusValueText;
    private TMP_Text strengthValueText;
    private TMP_Text statusText;
    private GameObject panelObject;
    private Button inflateButton;
    private Button deflateButton;

    private void Start()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        teleportRay = FindFirstObjectByType<ActivateTeleportationRay>();
        BuildUi();
        SyncValues();
        SetVisible(target != null && target.CurrentMethod == MethodKind.InflateDeflate);
    }

    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible && teleportRay != null)
            teleportRay.CancelInflateDeflatePick();
    }

    public void SetInflateMode()
    {
        if (target == null)
            return;

        target.SetInflateMode(0);
        UpdateModeButtons();
    }

    public void SetDeflateMode()
    {
        if (target == null)
            return;

        target.SetInflateMode(1);
        UpdateModeButtons();
    }

    public void OnRadiusChanged(float value)
    {
        if (target == null)
            return;

        target.SetInflateRadius(value);
        UpdateRadiusText();
    }

    public void OnStrengthChanged(float value)
    {
        if (target == null)
            return;

        target.SetInflateStrength(value);
        UpdateStrengthText();
    }

    public void BeginPick()
    {
        if (target == null || teleportRay == null)
            return;

        target.CurrentMethod = MethodKind.InflateDeflate;
        teleportRay.BeginInflateDeflatePick();
        SetStatus("Aim at the mesh and release trigger.");
    }

    public void CancelPick()
    {
        if (teleportRay != null)
            teleportRay.CancelInflateDeflatePick();

        SetStatus("Pick cancelled.");
    }

    private void SyncValues()
    {
        UpdateRadiusText();
        UpdateStrengthText();
        UpdateModeButtons();
        SetStatus("Set mode and parameters, then press Apply.");
    }

    private void UpdateRadiusText()
    {
        if (radiusValueText != null && target != null)
            radiusValueText.text = $"Radius: {target.InflateRadius:0.000}";
    }

    private void UpdateStrengthText()
    {
        if (strengthValueText != null && target != null)
            strengthValueText.text = $"Strength: {target.InflateStrength:0.000}";
    }

    private void UpdateModeButtons()
    {
        if (target == null)
            return;

        var inflateSelected = target.InflateMode == InflateDeflateMode.Inflate;
        SetButtonVisualState(inflateButton, inflateSelected);
        SetButtonVisualState(deflateButton, !inflateSelected);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private static void SetButtonVisualState(Selectable button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = selected
            ? new Color(0.23f, 1f, 0.80f, 0.85f)
            : new Color(1f, 1f, 1f, 0.35f);
    }

    private void BuildUi()
    {
        var root = ResolveUiRoot();
        if (root == null)
            return;

        panelObject = GameObject.Find(PanelName);
        RectTransform panel;
        if (panelObject == null)
        {
            panel = CreatePanel(root, PanelName, new Vector2(0.34f, -0.10f), new Vector2(230f, 215f));
            panelObject = panel.gameObject;

            CreateLabel(panel, "Inflate / Deflate", new Vector2(0f, 86f), 6.5f);
            inflateButton = CreateButton(panel, "InflateModeButton", "Inflate", new Vector2(-52f, 54f), new Vector2(95f, 28f), SetInflateMode);
            deflateButton = CreateButton(panel, "DeflateModeButton", "Deflate", new Vector2(52f, 54f), new Vector2(95f, 28f), SetDeflateMode);

            radiusValueText = CreateLabel(panel, "Radius", new Vector2(0f, 22f), 5f);
            var radiusSlider = CreateSlider(panel, "RadiusSlider", new Vector2(0f, 0f), 0.01f, 0.25f, target != null ? target.InflateRadius : 0.08f, OnRadiusChanged);

            strengthValueText = CreateLabel(panel, "Strength", new Vector2(0f, -34f), 5f);
            var strengthSlider = CreateSlider(panel, "StrengthSlider", new Vector2(0f, -56f), 0.001f, 0.10f, target != null ? target.InflateStrength : 0.02f, OnStrengthChanged);

            CreateButton(panel, "InflateApplyButton", "Apply", new Vector2(-52f, -94f), new Vector2(95f, 28f), BeginPick);
            CreateButton(panel, "InflateCancelButton", "Cancel", new Vector2(52f, -94f), new Vector2(95f, 28f), CancelPick);
            statusText = CreateLabel(panel, "Status", new Vector2(0f, -125f), 4.4f);

            OnRadiusChanged(radiusSlider.value);
            OnStrengthChanged(strengthSlider.value);
        }
        else
        {
            panel = panelObject.GetComponent<RectTransform>();
            inflateButton = FindButton(panel, "InflateModeButton");
            deflateButton = FindButton(panel, "DeflateModeButton");
            radiusValueText = FindText(panel, "RadiusLabel");
            strengthValueText = FindText(panel, "StrengthLabel");
            statusText = FindText(panel, "StatusLabel");
        }
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(parent, false);
        var panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;

        var image = panelObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.45f);

        return panel;
    }

    private static TMP_Text CreateLabel(RectTransform parent, string text, Vector2 anchoredPosition, float fontSize)
    {
        var labelObject = new GameObject(text.Replace(" ", string.Empty) + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(190f, 24f);

        var tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = text;
        tmp.color = Color.white;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return tmp;
    }

    private static Button CreateButton(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.35f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        CreateLabel(rect, text, Vector2.zero, 4.6f);
        return button;
    }

    private static Slider CreateSlider(RectTransform parent, string name, Vector2 anchoredPosition, float min, float max, float value, UnityEngine.Events.UnityAction<float> onChanged)
    {
        var sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        var rect = sliderObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(180f, 18f);

        var backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        var backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        backgroundObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(fillArea.transform, false);
        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillObject.GetComponent<Image>().color = new Color(0.23f, 1f, 0.80f, 0.85f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderObject.transform, false);
        var handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(8f, 0f);
        handleAreaRect.offsetMax = new Vector2(-8f, 0f);

        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(handleArea.transform, false);
        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 18f);
        handleObject.GetComponent<Image>().color = Color.white;

        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.targetGraphic = handleObject.GetComponent<Image>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private static Button FindButton(RectTransform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static TMP_Text FindText(RectTransform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }
}
