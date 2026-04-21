using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class MethodSelectionUI : MonoBehaviour
{
    private const string MethodLabelName = "MethodHeaderLabel";
    private const string ChangeButtonName = "MethodChangeButton";
    private const string DropdownRootName = "MethodDropdownRoot";
    private const string BasicOptionName = "MethodBasicTranslateButton";
    private const string InflateOptionName = "MethodInflateDeflateButton";
    private const string LoopOptionName = "MethodLoopSequenceButton";
    private static readonly Color ButtonBackgroundColor = new(0.08f, 0.11f, 0.14f, 0.72f);
    private static readonly Color ButtonHighlightedColor = new(0.35f, 0.40f, 0.46f, 0.95f);
    private static readonly Color ButtonPressedColor = new(0.55f, 0.60f, 0.66f, 0.95f);
    private static readonly Color DropdownBackgroundColor = new(0.49019608f, 1f, 0.8784314f, 0.16f);
    private static readonly Color TransparentColor = new(0f, 0f, 0f, 0f);

    private EditingMethodRuntimeSettings target;
    private InflateDeflateUI inflateDeflateUi;
    private CenterPool centerPool;
    private TMP_Text methodLabelText;
    private Button basicTranslateButton;
    private Button inflateDeflateButton;
    private Button loopSequenceButton;
    private GameObject dropdownRoot;
    private bool dropdownVisible;
    private GameObject sigmaObject;
    private GameObject kabschNeighborsObject;
    private GameObject surfaceNeighborsObject;
    private GameObject commitObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var canvasObject = GameObject.Find("Method Canvas");
        if (canvasObject == null)
            return;

        if (canvasObject.GetComponent<MethodSelectionUI>() == null)
            canvasObject.AddComponent<MethodSelectionUI>();

        if (canvasObject.GetComponent<InflateDeflateUI>() == null)
            canvasObject.AddComponent<InflateDeflateUI>();
    }

    private void Start()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        inflateDeflateUi = GetComponent<InflateDeflateUI>();
        centerPool = FindFirstObjectByType<CenterPool>();

        var root = ResolveUiRoot();
        if (root == null)
            return;

        CacheBasicTranslateObjects(root);
        BuildUi(root);
        ApplyMethodVisibility();
    }

    public void ToggleMethodDropdown()
    {
        dropdownVisible = !dropdownVisible;
        if (dropdownRoot != null)
            dropdownRoot.SetActive(dropdownVisible);
    }

    public void SelectBasicTranslate()
    {
        if (target == null)
            return;

        target.CurrentMethod = MethodKind.BasicTranslate;
        dropdownVisible = false;
        ApplyMethodVisibility();
    }

    public void SelectInflateDeflate()
    {
        if (target == null)
            return;

        target.CurrentMethod = MethodKind.InflateDeflate;
        target.SetInflateMode(0);
        dropdownVisible = false;
        ApplyMethodVisibility();
    }

    private void ApplyMethodVisibility()
    {
        var isInflate = target != null && target.CurrentMethod == MethodKind.InflateDeflate;

        SetObjectActive(sigmaObject, !isInflate);
        SetObjectActive(kabschNeighborsObject, !isInflate);
        SetObjectActive(surfaceNeighborsObject, !isInflate);
        SetObjectActive(commitObject, !isInflate);
        centerPool?.SetInteractionEnabled(!isInflate);

        if (inflateDeflateUi != null)
            inflateDeflateUi.SetVisible(isInflate);

        if (dropdownRoot != null)
            dropdownRoot.SetActive(dropdownVisible);

        UpdateMethodLabel();
        UpdateOptionButtons();
    }

    private void UpdateMethodLabel()
    {
        if (methodLabelText == null || target == null)
            return;

        methodLabelText.text = target.CurrentMethod switch
        {
            MethodKind.InflateDeflate => "Inflate / Deflate",
            MethodKind.LoopSequence => "Looping",
            _ => "Basic Translate"
        };
    }

    private void UpdateOptionButtons()
    {
        if (target == null)
            return;

        var isInflate = target.CurrentMethod == MethodKind.InflateDeflate;
        SetButtonVisualState(basicTranslateButton, !isInflate);
        SetButtonVisualState(inflateDeflateButton, isInflate);
        SetDisabledButtonVisualState(loopSequenceButton);
    }

    private void CacheBasicTranslateObjects(RectTransform root)
    {
        sigmaObject = FindObjectRecursive(root, "Sigma");
        kabschNeighborsObject = FindObjectRecursive(root, "Kabsch Neighbors");
        surfaceNeighborsObject = FindObjectRecursive(root, "Surface Neighbors");
        commitObject = FindObjectRecursive(root, "Commit");
    }

    private void BuildUi(RectTransform root)
    {
        DestroyLegacyObjects(root);

        methodLabelText = CreateLabel(
            root,
            MethodLabelName,
            "Basic Translate",
            new Vector2(0.01f, 0.18f),
            new Vector2(180f, 40f),
            10.5f,
            TextAlignmentOptions.Center);

        CreateButton(
            root,
            ChangeButtonName,
            "Methods",
            new Vector2(0.245f, 0.182f),
            new Vector2(0.19f, 0.045f),
            ToggleMethodDropdown);

        dropdownRoot = CreateDropdownRoot(
            root,
            DropdownRootName,
            new Vector2(0.255f, 0.082f),
            new Vector2(0.18f, 0.165f)).gameObject;

        var dropdownRect = dropdownRoot.GetComponent<RectTransform>();
        basicTranslateButton = CreateButton(
            dropdownRect,
            BasicOptionName,
            "Basic Translate",
            new Vector2(0f, 0.05f),
            new Vector2(0.18f, 0.042f),
            SelectBasicTranslate,
            true,
            true);
        inflateDeflateButton = CreateButton(
            dropdownRect,
            InflateOptionName,
            "Inflate/Deflate",
            new Vector2(0f, 0f),
            new Vector2(0.18f, 0.042f),
            SelectInflateDeflate,
            true,
            true);
        loopSequenceButton = CreateButton(
            dropdownRect,
            LoopOptionName,
            "Looping",
            new Vector2(0f, -0.05f),
            new Vector2(0.18f, 0.042f),
            null,
            false,
            true);

        dropdownRoot.SetActive(false);
        dropdownVisible = false;
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
    }

    private static void DestroyLegacyObjects(RectTransform root)
    {
        DestroyChild(root, "Method Selection Panel");
        DestroyChild(root, MethodLabelName);
        DestroyChild(root, ChangeButtonName);
        DestroyChild(root, DropdownRootName);
        DestroyChild(root, "MethodToggleButton");
    }

    private static void DestroyChild(Transform root, string childName)
    {
        var child = FindObjectRecursive(root, childName);
        if (child != null)
            Destroy(child);
    }

    private static RectTransform CreateDropdownRoot(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var rootObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(parent, false);
        var rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        var image = rootObject.GetComponent<Image>();
        image.color = DropdownBackgroundColor;
        return rect;
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
        tmp.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            tmp.font = TMP_Settings.defaultFontAsset;

        return tmp;
    }

    private static Button CreateButton(RectTransform parent, string name, string text, Vector2 anchoredPosition, Vector2 size, UnityEngine.Events.UnityAction onClick, bool interactable = true, bool textOnly = false)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = new Vector3(0.7f, 0.7f, 0.7f);

        var image = buttonObject.GetComponent<Image>();
        image.color = textOnly ? TransparentColor : ButtonBackgroundColor;

        var button = buttonObject.GetComponent<Button>();
        button.interactable = interactable;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        var colors = button.colors;
        colors.normalColor = textOnly ? TransparentColor : ButtonBackgroundColor;
        colors.highlightedColor = textOnly ? TransparentColor : ButtonHighlightedColor;
        colors.pressedColor = textOnly ? TransparentColor : ButtonPressedColor;
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(rect, name + "Label", text, Vector2.zero, new Vector2(220f, 50f), 12.5f, TextAlignmentOptions.Center);
        return button;
    }

    private static void SetObjectActive(GameObject targetObject, bool active)
    {
        if (targetObject != null)
            targetObject.SetActive(active);
    }

    private static void SetButtonVisualState(Selectable button, bool selected)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = TransparentColor;
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
            label.color = selected ? new Color(0.49019608f, 1f, 0.8784314f, 1f) : Color.white;
        }
    }

    private static void SetDisabledButtonVisualState(Selectable button)
    {
        if (button == null || button.targetGraphic == null)
            return;

        button.targetGraphic.color = TransparentColor;
        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.fontStyle = FontStyles.Normal;
            label.color = new Color(1f, 1f, 1f, 0.65f);
        }
    }

    private static TMP_Text FindText(RectTransform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<TMP_Text>() : null;
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
