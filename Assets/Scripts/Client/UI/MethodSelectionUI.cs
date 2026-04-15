using TMPro;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.UI;

public class MethodSelectionUI : MonoBehaviour
{
    private const string PanelName = "Method Selection Panel";

    private EditingMethodRuntimeSettings target;
    private InflateDeflateUI inflateDeflateUi;
    private Button basicTranslateButton;
    private Button inflateDeflateButton;

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

    public void SelectBasicTranslate()
    {
        if (target == null)
            return;

        target.CurrentMethod = MethodKind.BasicTranslate;
        ApplyMethodVisibility();
    }

    public void SelectInflateDeflate()
    {
        if (target == null)
            return;

        target.CurrentMethod = MethodKind.InflateDeflate;
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

        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (target == null)
            return;

        var isInflate = target.CurrentMethod == MethodKind.InflateDeflate;
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

        var panelObject = GameObject.Find(PanelName);
        RectTransform panel;
        if (panelObject == null)
        {
            panel = CreatePanel(root, PanelName, new Vector2(0.15f, 0.115f), new Vector2(0.24f, 0.08f));
            CreateLabel(panel, "Method", new Vector2(0f, 0.022f), 20f);
            basicTranslateButton = CreateButton(panel, "BasicTranslateButton", "Basic Translate", new Vector2(-0.055f, -0.01f), new Vector2(0.11f, 0.05f), SelectBasicTranslate);
            inflateDeflateButton = CreateButton(panel, "InflateDeflateButton", "Inflate/Deflate", new Vector2(0.055f, -0.01f), new Vector2(0.11f, 0.05f), SelectInflateDeflate);
        }
        else
        {
            panel = panelObject.GetComponent<RectTransform>();
            basicTranslateButton = FindButton(panel, "BasicTranslateButton");
            inflateDeflateButton = FindButton(panel, "InflateDeflateButton");
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
        panel.localScale = Vector3.one;

        var image = panelObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.45f);

        return panel;
    }

    private static TMP_Text CreateLabel(RectTransform parent, string text, Vector2 anchoredPosition, float fontSize)
    {
        var labelObject = new GameObject(text + "Label", typeof(RectTransform), typeof(TextMeshProUGUI));
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

        CreateLabel(rect, text, Vector2.zero, 4.6f);
        return button;
    }

    private static Button FindButton(RectTransform parent, string name)
    {
        var child = parent.Find(name);
        return child != null ? child.GetComponent<Button>() : null;
    }
}
