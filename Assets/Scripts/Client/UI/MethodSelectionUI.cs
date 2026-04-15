using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class MethodSelectionUI : MonoBehaviour
{
    private const string ToggleButtonName = "MethodToggleButton";
    private const string DropdownRootName = "MethodDropdownRoot";
    private const string BasicOptionName = "MethodBasicTranslateButton";
    private const string InflateOptionName = "MethodInflateDeflateButton";

    private EditingMethodRuntimeSettings target;
    private InflateDeflateUI inflateDeflateUi;
    private Button toggleButton;
    private Button basicTranslateButton;
    private Button inflateDeflateButton;
    private TMP_Text toggleButtonText;
    private GameObject dropdownRoot;
    private bool dropdownVisible;

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
        BuildUi();
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
        dropdownVisible = false;
        ApplyMethodVisibility();
    }

    private void ApplyMethodVisibility()
    {
        var isInflate = target != null && target.CurrentMethod == MethodKind.InflateDeflate;

        SetNamedObjectActive("Sigma", !isInflate);
        SetNamedObjectActive("Kabsch Neighbors", !isInflate);
        SetNamedObjectActive("Surface Neighbors", !isInflate);
        SetNamedObjectActive("Commit", !isInflate);

        if (inflateDeflateUi != null)
            inflateDeflateUi.SetVisible(isInflate);

        if (dropdownRoot != null)
            dropdownRoot.SetActive(dropdownVisible);

        UpdateToggleLabel();
        UpdateButtonState();
    }

    private void UpdateToggleLabel()
    {
        if (toggleButtonText == null || target == null)
            return;

        toggleButtonText.text = target.CurrentMethod == MethodKind.InflateDeflate
            ? "Method: Inflate/Deflate"
            : "Method: Basic Translate";
    }

    private void UpdateButtonState()
    {
        if (target == null)
            return;

        var isInflate = target.CurrentMethod == MethodKind.InflateDeflate;
        SetButtonVisualState(toggleButton, true);
        SetButtonVisualState(basicTranslateButton, !isInflate);
        SetButtonVisualState(inflateDeflateButton, isInflate);
    }

    private static void SetNamedObjectActive(string objectName, bool active)
    {
        var namedObject = GameObject.Find(objectName);
        if (namedObject != null)
            namedObject.SetActive(active);
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

        DestroyLegacyPanel();

        toggleButton = FindButton(root, ToggleButtonName);
        dropdownRoot = FindObject(root, DropdownRootName);
        basicTranslateButton = FindButton(root, BasicOptionName);
        inflateDeflateButton = FindButton(root, InflateOptionName);

        if (toggleButton == null)
            toggleButton = CreateButton(root, ToggleButtonName, "Method", new Vector2(0f, 0.18f), new Vector2(0.3f, 0.1f), ToggleMethodDropdown);

        toggleButtonText = FindText(toggleButton.GetComponent<RectTransform>(), "MethodLabel");
        if (toggleButtonText == null)
            toggleButtonText = FindText(toggleButton.GetComponent<RectTransform>(), "MethodToggleButtonLabel");

        if (dropdownRoot == null)
            dropdownRoot = CreateDropdownRoot(root, DropdownRootName, new Vector2(0f, 0.105f), new Vector2(0.3f, 0.12f)).gameObject;

        var dropdownRect = dropdownRoot.GetComponent<RectTransform>();
        if (basicTranslateButton == null)
            basicTranslateButton = CreateButton(dropdownRect, BasicOptionName, "Basic Translate", new Vector2(0f, 0.025f), new Vector2(0.3f, 0.05f), SelectBasicTranslate);
        if (inflateDeflateButton == null)
            inflateDeflateButton = CreateButton(dropdownRect, InflateOptionName, "Inflate/Deflate", new Vector2(0f, -0.03f), new Vector2(0.3f, 0.05f), SelectInflateDeflate);

        dropdownRoot.SetActive(false);
        dropdownVisible = false;
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
    }

    private static void DestroyLegacyPanel()
    {
        var legacy = GameObject.Find("Method Selection Panel");
        if (legacy != null)
            Destroy(legacy);
    }

    private static RectTransform CreateDropdownRoot(RectTransform parent, string name, Vector2 anchoredPosition, Vector2 size)
    {
        var rootObject = new GameObject(name, typeof(RectTransform));
        rootObject.transform.SetParent(parent, false);
        var rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
        return rect;
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
        rect.sizeDelta = new Vector2(200f, 50f);
        rect.localScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

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
        rect.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.35f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        CreateLabel(rect, text, Vector2.zero, 20f);
        return button;
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

    private static GameObject FindObject(RectTransform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.gameObject : null;
    }
}
