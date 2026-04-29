using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Centers;
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
    private static readonly Color SelectedMethodTextColor = new(0.49019608f, 1f, 0.8784314f, 1f);
    private static readonly Color DisabledMethodTextColor = new(0.42f, 0.42f, 0.42f, 1f);
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

    private void Start()
    {
        target = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        inflateDeflateUi = GetComponent<InflateDeflateUI>();
        centerPool = FindFirstObjectByType<CenterPool>();

        var root = ResolveUiRoot();
        if (root == null)
            return;

        CacheBasicTranslateObjects(root);
        CacheMethodSelectionObjects(root);
        WireMethodSelectionEvents();
        ApplyMethodVisibility();
    }

    public void ToggleMethodDropdown()
    {
        if (inflateDeflateUi != null && !inflateDeflateUi.CanChangeMethod())
            return;

        dropdownVisible = !dropdownVisible;
        if (dropdownRoot != null)
            dropdownRoot.SetActive(dropdownVisible);
    }

    public void SelectBasicTranslate()
    {
        if (target == null)
            return;

        if (inflateDeflateUi != null && !inflateDeflateUi.CanChangeMethod())
            return;

        target.CurrentMethod = MethodKind.BasicTranslate;
        dropdownVisible = false;
        ApplyMethodVisibility();
    }

    public void SelectInflateDeflate()
    {
        if (target == null)
            return;

        if (inflateDeflateUi != null && !inflateDeflateUi.CanChangeMethod())
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
        centerPool?.SetInteractionEnabled(target == null || target.CurrentMethod != MethodKind.LoopSequence);

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

        var method = target.CurrentMethod;
        SetButtonVisualState(basicTranslateButton, method == MethodKind.BasicTranslate);
        SetButtonVisualState(inflateDeflateButton, method == MethodKind.InflateDeflate);
        SetButtonVisualState(loopSequenceButton, method == MethodKind.LoopSequence);
    }

    private void CacheBasicTranslateObjects(RectTransform root)
    {
        sigmaObject = FindObjectRecursive(root, "Sigma");
        kabschNeighborsObject = FindObjectRecursive(root, "Kabsch Neighbors");
        surfaceNeighborsObject = FindObjectRecursive(root, "Surface Neighbors");
        commitObject = FindObjectRecursive(root, "Commit");
    }

    private void CacheMethodSelectionObjects(RectTransform root)
    {
        methodLabelText = FindComponentRecursive<TMP_Text>(root, MethodLabelName);
        dropdownRoot = FindObjectRecursive(root, DropdownRootName);

        var changeButton = FindComponentRecursive<Button>(root, ChangeButtonName);
        basicTranslateButton = FindComponentRecursive<Button>(root, BasicOptionName);
        inflateDeflateButton = FindComponentRecursive<Button>(root, InflateOptionName);
        loopSequenceButton = FindComponentRecursive<Button>(root, LoopOptionName);

        if (changeButton != null)
            changeButton.onClick.AddListener(ToggleMethodDropdown);
    }

    private void WireMethodSelectionEvents()
    {
        if (basicTranslateButton != null)
            basicTranslateButton.onClick.AddListener(SelectBasicTranslate);

        if (inflateDeflateButton != null)
            inflateDeflateButton.onClick.AddListener(SelectInflateDeflate);

        if (loopSequenceButton != null)
            loopSequenceButton.interactable = false;

        if (dropdownRoot == null)
            return;

        dropdownRoot.SetActive(false);
        dropdownVisible = false;
    }

    private RectTransform ResolveUiRoot()
    {
        if (transform.childCount > 0 && transform.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return transform as RectTransform;
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
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontStyle = FontStyles.Normal;
            label.color = !button.interactable
                ? DisabledMethodTextColor
                : selected ? SelectedMethodTextColor : Color.white;
        }
    }

    private static T FindComponentRecursive<T>(Transform parent, string name) where T : Component
    {
        var targetObject = FindObjectRecursive(parent, name);
        return targetObject != null ? targetObject.GetComponent<T>() : null;
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
