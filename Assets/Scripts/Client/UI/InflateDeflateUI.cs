using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflateUI : MonoBehaviour
{
    private const string PanelName = "Inflate Deflate Panel";
    private const float RadiusStep = 0.01f;
    private const float StrengthStep = 0.005f;
    private static readonly Color SelectedButtonColor = new(0.24f, 0.29f, 0.35f, 0.94f);
    private static readonly Color UnselectedButtonColor = new(0.10f, 0.13f, 0.16f, 0.78f);

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

        var root = ResolveUiRoot();
        if (root == null)
            return;

        BuildUi(root);
        SyncValues();
        SetVisible(target != null && target.CurrentMethod == MethodKind.InflateDeflate);
    }

    public void SetVisible(bool visible)
    {
        if (panelObject != null)
            panelObject.SetActive(visible);

        if (!visible && teleportRay != null)
            teleportRay.CancelInflateDeflatePick();

        if (visible)
            SetStatus("Set parameters, press Pick Point, then aim at the mesh and release the teleport trigger.");
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

    public void DecreaseRadius()
    {
        if (target == null)
            return;

        target.SetInflateRadius(target.InflateRadius - RadiusStep);
        UpdateRadiusText();
    }

    public void IncreaseRadius()
    {
        if (target == null)
            return;

        target.SetInflateRadius(target.InflateRadius + RadiusStep);
        UpdateRadiusText();
    }

    public void DecreaseStrength()
    {
        if (target == null)
            return;

        target.SetInflateStrength(target.InflateStrength - StrengthStep);
        UpdateStrengthText();
    }

    public void IncreaseStrength()
    {
        if (target == null)
            return;

        target.SetInflateStrength(target.InflateStrength + StrengthStep);
        UpdateStrengthText();
    }

    public void BeginPick()
    {
        if (target == null || teleportRay == null)
            return;

        target.CurrentMethod = MethodKind.InflateDeflate;
        teleportRay.BeginInflateDeflatePick();
        SetStatus("Pick mode is active. Hold the teleport trigger, aim at the mesh and release.");
    }

    public void CancelPick()
    {
        if (teleportRay != null)
            teleportRay.CancelInflateDeflatePick();

        SetStatus("Selection cancelled. Teleport works normally again.");
    }

    public void ShowPickFailed(string message)
    {
        SetStatus(message);
    }

    public void ShowPickCompleted()
    {
        SetStatus("Reference point selected. The edit was sent to the method pipeline.");
    }

    private void SyncValues()
    {
        UpdateRadiusText();
        UpdateStrengthText();
        UpdateModeButtons();
        SetStatus("Set parameters, press Pick Point, then aim at the mesh and release the teleport trigger.");
    }

    private void UpdateRadiusText()
    {
        if (radiusValueText != null && target != null)
            radiusValueText.text = $"{target.InflateRadius:0.000}";
    }

    private void UpdateStrengthText()
    {
        if (strengthValueText != null && target != null)
            strengthValueText.text = $"{target.InflateStrength:0.000}";
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

    private void BuildUi(RectTransform root)
    {
        DestroyExistingPanel(root);

        var panel = CreatePanel(root, PanelName, new Vector2(0.02f, -0.03f), new Vector2(0.28f, 0.20f));
        panelObject = panel.gameObject;

        CreateLabel(panel, "InflateDeflateTitle", "Inflate / Deflate", new Vector2(0f, 0.085f), new Vector2(220f, 44f), 13f, TextAlignmentOptions.Center);

        CreateLabel(panel, "ModeTitle", "Mode", new Vector2(-0.095f, 0.045f), new Vector2(120f, 36f), 11f, TextAlignmentOptions.Left);
        inflateButton = CreateButton(panel, "InflateModeButton", "Inflate", new Vector2(-0.035f, 0.045f), new Vector2(0.095f, 0.04f), SetInflateMode);
        deflateButton = CreateButton(panel, "DeflateModeButton", "Deflate", new Vector2(0.065f, 0.045f), new Vector2(0.095f, 0.04f), SetDeflateMode);

        CreateLabel(panel, "RadiusTitle", "Radius", new Vector2(-0.095f, 0.005f), new Vector2(120f, 36f), 11f, TextAlignmentOptions.Left);
        CreateStepper(panel, "Radius", 0.005f, out radiusValueText, DecreaseRadius, IncreaseRadius);

        CreateLabel(panel, "StrengthTitle", "Strength", new Vector2(-0.095f, -0.035f), new Vector2(120f, 36f), 11f, TextAlignmentOptions.Left);
        CreateStepper(panel, "Strength", -0.035f, out strengthValueText, DecreaseStrength, IncreaseStrength);

        CreateButton(panel, "InflatePickPointButton", "Pick Point", new Vector2(-0.045f, -0.085f), new Vector2(0.12f, 0.045f), BeginPick);
        CreateButton(panel, "InflateCancelButton", "Cancel", new Vector2(0.075f, -0.085f), new Vector2(0.09f, 0.045f), CancelPick);

        statusText = CreateLabel(panel, "InflateStatusLabel", string.Empty, new Vector2(0f, -0.135f), new Vector2(240f, 60f), 8.5f, TextAlignmentOptions.Center);
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
    }

    private static void DestroyExistingPanel(RectTransform root)
    {
        var existing = FindObjectRecursive(root, PanelName);
        if (existing != null)
            Destroy(existing);
    }

    private static RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var panelObject = new GameObject(name, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        var panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;
        panel.localScale = Vector3.one;
        return panel;
    }

    private static void CreateStepper(RectTransform parent, string prefix, float yPosition, out TMP_Text valueText, UnityEngine.Events.UnityAction onDecrease, UnityEngine.Events.UnityAction onIncrease)
    {
        CreateButton(parent, prefix + "DecreaseButton", "-", new Vector2(-0.03f, yPosition), new Vector2(0.04f, 0.035f), onDecrease);
        valueText = CreateLabel(parent, prefix + "ValueLabel", "0.000", new Vector2(0.03f, yPosition), new Vector2(90f, 36f), 11f, TextAlignmentOptions.Center);
        CreateButton(parent, prefix + "IncreaseButton", "+", new Vector2(0.09f, yPosition), new Vector2(0.04f, 0.035f), onIncrease);
    }

    private static TMP_Text CreateLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

        var tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.text = text;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
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
        rect.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        var image = buttonObject.GetComponent<Image>();
        image.color = UnselectedButtonColor;

        var button = buttonObject.GetComponent<Button>();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.34f, 0.39f, 0.45f, 0.95f);
        colors.pressedColor = new Color(0.52f, 0.58f, 0.64f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(rect, name + "Label", text, Vector2.zero, new Vector2(160f, 40f), 11f, TextAlignmentOptions.Center);
        return button;
    }

    private static void SetButtonVisualState(Selectable button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = selected ? SelectedButtonColor : UnselectedButtonColor;
    }

    private static GameObject FindObjectRecursive(Transform parent, string name)
    {
        if (parent == null)
            return null;

        if (parent.name == name)
            return parent.gameObject;

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindObjectRecursive(parent.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
