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
    private static readonly Color SelectedButtonColor = new(0.20f, 0.24f, 0.29f, 0.92f);
    private static readonly Color UnselectedButtonColor = new(0.08f, 0.11f, 0.14f, 0.72f);

    private EditingMethodRuntimeSettings target;
    private InflateDeflateUI inflateDeflateUi;
    private Button toggleButton;
    private Button basicTranslateButton;
    private Button inflateDeflateButton;
    private TMP_Text toggleButtonText;
    private GameObject dropdownRoot;
    private bool dropdownVisible;
    private GameObject sigmaObject;
    private GameObject kabschNeighborsObject;
    private GameObject surfaceNeighborsObject;
    private GameObject commitObject;
    private CenterPool centerPool;

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
        BuildUi();
        CacheBasicTranslateObjects();
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

        SetObjectActive(sigmaObject, !isInflate);
        SetObjectActive(kabschNeighborsObject, !isInflate);
        SetObjectActive(surfaceNeighborsObject, !isInflate);
        SetObjectActive(commitObject, !isInflate);
        centerPool?.SetInteractionEnabled(!isInflate);

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

    private void CacheBasicTranslateObjects()
    {
        var root = ResolveUiRoot();
        if (root == null)
            return;

        sigmaObject = FindObjectRecursive(root, "Sigma");
        kabschNeighborsObject = FindObjectRecursive(root, "Kabsch Neighbors");
        surfaceNeighborsObject = FindObjectRecursive(root, "Surface Neighbors");
        commitObject = FindObjectRecursive(root, "Commit");
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

        button.targetGraphic.color = selected ? SelectedButtonColor : UnselectedButtonColor;
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
            toggleButton = CreateButton(root, ToggleButtonName, "Method", new Vector2(0f, 0.18f), new Vector2(0.34f, 0.065f), ToggleMethodDropdown);

        toggleButtonText = FindText(toggleButton.GetComponent<RectTransform>(), "MethodLabel");
        if (toggleButtonText == null)
            toggleButtonText = FindText(toggleButton.GetComponent<RectTransform>(), "MethodToggleButtonLabel");

        if (dropdownRoot == null)
            dropdownRoot = CreateDropdownRoot(root, DropdownRootName, new Vector2(0f, 0.095f), new Vector2(0.34f, 0.15f)).gameObject;

        var dropdownRect = dropdownRoot.GetComponent<RectTransform>();
        if (basicTranslateButton == null)
            basicTranslateButton = CreateButton(dropdownRect, BasicOptionName, "Basic Translate", new Vector2(0f, 0.035f), new Vector2(0.34f, 0.055f), SelectBasicTranslate);
        if (inflateDeflateButton == null)
            inflateDeflateButton = CreateButton(dropdownRect, InflateOptionName, "Inflate/Deflate", new Vector2(0f, -0.03f), new Vector2(0.34f, 0.055f), SelectInflateDeflate);

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
        rect.localScale = new Vector3(0.6f, 0.6f, 0.6f);

        var image = buttonObject.GetComponent<Image>();
        image.color = UnselectedButtonColor;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        var colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.35f, 0.40f, 0.46f, 0.95f);
        colors.pressedColor = new Color(0.55f, 0.60f, 0.66f, 0.95f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        CreateLabel(rect, text, Vector2.zero, 15f);
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
