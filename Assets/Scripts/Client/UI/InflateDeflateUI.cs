using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class InflateDeflateUI : MonoBehaviour
{
    private const string PanelName = "Inflate Deflate Panel";
    private const float RowWidth = 0.24000001f;
    private const float RowHeight = 0.315f;
    private const float TitleY = 0.005f;
    private const float ValueY = -0.03250019f;
    private const float StepperButtonY = -0.035f;
    private const float RadiusStep = 0.01f;
    private const float StrengthStep = 0.005f;
    private static readonly Vector3 TitleScale = new(0.005f, 0.005f, 0.005f);
    private static readonly Vector3 ValueScale = new(0.005f, 0.005f, 0.005f);
    private static readonly Vector3 ButtonLabelScale = new(0.0025f, 0.0025f, 0.0025f);
    private static readonly Vector3 StatusScale = new(0.0022f, 0.0022f, 0.0022f);
    private static readonly Color SelectedButtonColor = new(0.24f, 0.29f, 0.35f, 0.94f);
    private static readonly Color UnselectedButtonColor = new(0.13725491f, 0.13725491f, 0.13725491f, 0.7058824f);
    private static readonly Color StepperButtonColor = new(0.49019608f, 1f, 0.8784314f, 0.13725491f);

    private EditingMethodRuntimeSettings target;
    private ActivateTeleportationRay teleportRay;
    private TMP_Text radiusValueText;
    private TMP_Text strengthValueText;
    private TMP_Text statusText;
    private TMP_Text titleTemplate;
    private TMP_Text valueTemplate;
    private TMP_Text stepperButtonTemplate;
    private TMP_Text actionButtonTemplate;
    private GameObject panelObject;
    private Button inflateButton;
    private Button deflateButton;

    private void Start()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        teleportRay = FindFirstObjectByType<ActivateTeleportationRay>();

        if (target != null)
            target.SetInflateMode(0);

        var root = ResolveUiRoot();
        if (root == null)
            return;

        CacheTemplates(root);
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
            SetStatus("Set parameters, press Pick Point, then use the left hand ray to aim at the mesh and release the teleport trigger.");
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
        SetStatus("Pick mode is active. Use the left hand ray, hold the teleport trigger, aim at the mesh and release.");
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
        SetStatus("Set parameters, press Pick Point, then use the left hand ray to aim at the mesh and release the teleport trigger.");
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

        var panel = CreatePanel(root, PanelName);
        panelObject = panel.gameObject;

        var modeRow = CreateRow(panel, "ModeRow", 0.1152f);
        CreateLabel(modeRow, "ModeTitle", "mode", new Vector2(0f, TitleY), Vector2.zero, 5f, TextAlignmentOptions.Center, TitleScale, titleTemplate);
        inflateButton = CreateButton(modeRow, "InflateModeButton", "inflate", new Vector2(-0.06f, StepperButtonY), new Vector2(0.20f, 0.1f), SetInflateMode, UnselectedButtonColor, 20f, ButtonLabelScale, actionButtonTemplate);
        deflateButton = CreateButton(modeRow, "DeflateModeButton", "deflate", new Vector2(0.06f, StepperButtonY), new Vector2(0.20f, 0.1f), SetDeflateMode, UnselectedButtonColor, 20f, ButtonLabelScale, actionButtonTemplate);

        var radiusRow = CreateRow(panel, "RadiusRow", 0.0176f);
        CreateLabel(radiusRow, "RadiusTitle", "radius", new Vector2(0f, TitleY), Vector2.zero, 5f, TextAlignmentOptions.Center, TitleScale, titleTemplate);
        CreateStepper(radiusRow, "Radius", out radiusValueText, DecreaseRadius, IncreaseRadius);

        var strengthRow = CreateRow(panel, "StrengthRow", -0.08f);
        CreateLabel(strengthRow, "StrengthTitle", "strength", new Vector2(0f, TitleY), Vector2.zero, 5f, TextAlignmentOptions.Center, TitleScale, titleTemplate);
        CreateStepper(strengthRow, "Strength", out strengthValueText, DecreaseStrength, IncreaseStrength);

        var actionRow = CreateRow(panel, "ActionRow", -0.181f);
        CreateButton(actionRow, "InflatePickPointButton", "pick point", new Vector2(-0.075f, 0f), new Vector2(0.22f, 0.1f), BeginPick, UnselectedButtonColor, 20f, ButtonLabelScale, actionButtonTemplate);
        CreateButton(actionRow, "InflateCancelButton", "cancel", new Vector2(0.085f, 0f), new Vector2(0.13f, 0.1f), CancelPick, UnselectedButtonColor, 20f, ButtonLabelScale, actionButtonTemplate);

        statusText = CreateLabel(panel, "InflateStatusLabel", string.Empty, new Vector2(0f, -0.255f), new Vector2(260f, 44f), 3.5f, TextAlignmentOptions.Center, StatusScale);
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
    }

    private void CacheTemplates(RectTransform root)
    {
        titleTemplate = FindTextRecursive(FindObjectRecursive(root, "Sigma")?.transform, "Title");
        valueTemplate = FindTextRecursive(FindObjectRecursive(root, "Surface Neighbors")?.transform, "CURR");
        stepperButtonTemplate = FindTextRecursive(FindObjectRecursive(root, "Surface Neighbors")?.transform, "Text (TMP)");
        actionButtonTemplate = FindTextRecursive(FindObjectRecursive(root, "Commit")?.transform, "Text (TMP)");
    }

    private static void DestroyExistingPanel(RectTransform root)
    {
        var existing = FindObjectRecursive(root, PanelName);
        if (existing != null)
            Destroy(existing);
    }

    private static RectTransform CreatePanel(RectTransform parent, string name)
    {
        var panelObject = new GameObject(name, typeof(RectTransform));
        panelObject.transform.SetParent(parent, false);
        var panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = Vector2.zero;
        panel.localScale = Vector3.one;
        return panel;
    }

    private static RectTransform CreateRow(RectTransform parent, string name, float anchoredY)
    {
        var rowObject = new GameObject(name, typeof(RectTransform));
        rowObject.transform.SetParent(parent, false);
        var row = rowObject.GetComponent<RectTransform>();
        row.anchorMin = new Vector2(0.5f, 0.5f);
        row.anchorMax = new Vector2(0.5f, 0.5f);
        row.pivot = new Vector2(0.5f, 0.5f);
        row.anchoredPosition = new Vector2(0f, anchoredY);
        row.sizeDelta = new Vector2(RowWidth, RowHeight);
        row.localScale = Vector3.one;
        return row;
    }

    private void CreateStepper(RectTransform parent, string prefix, out TMP_Text valueText, UnityEngine.Events.UnityAction onDecrease, UnityEngine.Events.UnityAction onIncrease)
    {
        CreateButton(parent, prefix + "DecreaseButton", "-", new Vector2(-0.07f, StepperButtonY), new Vector2(0.1f, 0.1f), onDecrease, StepperButtonColor, 20f, ButtonLabelScale, stepperButtonTemplate);
        valueText = CreateLabel(parent, prefix + "ValueLabel", "0.000", new Vector2(0f, ValueY), Vector2.zero, 5f, TextAlignmentOptions.Center, ValueScale, valueTemplate);
        CreateButton(parent, prefix + "IncreaseButton", "+", new Vector2(0.07f, StepperButtonY), new Vector2(0.1f, 0.1f), onIncrease, StepperButtonColor, 20f, ButtonLabelScale, stepperButtonTemplate);
    }

    private TMP_Text CreateLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        return CreateLabel(parent, name, text, anchoredPosition, size, fontSize, alignment, new Vector3(0.0025f, 0.0025f, 0.0025f), null);
    }

    private TMP_Text CreateLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, Vector3 scale)
    {
        return CreateLabel(parent, name, text, anchoredPosition, size, fontSize, alignment, scale, null);
    }

    private TMP_Text CreateLabel(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, Vector3 scale, TMP_Text template)
    {
        var labelObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);
        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = scale;

        var tmp = labelObject.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.text = text;
        tmp.color = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;
        ApplyStyleFromTemplate(tmp, template);

        return tmp;
    }

    private Button CreateButton(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        Color color,
        float fontSize,
        Vector3 labelScale)
    {
        return CreateButton(parent, name, text, anchoredPosition, size, onClick, color, fontSize, labelScale, null);
    }

    private Button CreateButton(
        RectTransform parent,
        string name,
        string text,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick,
        Color color,
        float fontSize,
        Vector3 labelScale,
        TMP_Text template)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        var image = buttonObject.GetComponent<Image>();
        image.color = color;

        var button = buttonObject.GetComponent<Button>();
        if (onClick != null)
            button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        colors.pressedColor = new Color(0.6792453f, 0.6792453f, 0.6792453f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(buttonObject.GetComponent<RectTransform>(), name + "Label", text, new Vector2(0f, 0.004f), new Vector2(200f, 50f), fontSize, TextAlignmentOptions.Center, labelScale, template);
        return button;
    }

    private static void ApplyStyleFromTemplate(TMP_Text targetText, TMP_Text template)
    {
        if (targetText == null || template == null)
            return;

        targetText.font = template.font;
        targetText.fontSharedMaterial = template.fontSharedMaterial;
        targetText.fontStyle = template.fontStyle;
        targetText.enableKerning = template.enableKerning;
        targetText.isRightToLeftText = template.isRightToLeftText;
        targetText.color = template.color;
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

    private static TMP_Text FindTextRecursive(Transform parent, string name)
    {
        var target = FindObjectRecursive(parent, name);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }
}
