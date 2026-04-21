using TMPro;
using TvmVr2.Client.Sequence;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class InflateDeflateMenuBuilder
{
    private const string PanelName = "Inflate Deflate Panel";

    [MenuItem("TVM VR/Build Inflate Deflate Menu")]
    public static void Build()
    {
        var canvas = GameObject.Find("Method Canvas");
        if (canvas == null)
        {
            Debug.LogError("InflateDeflateMenuBuilder: Method Canvas was not found in the open scene.");
            return;
        }

        var root = ResolveUiRoot(canvas.transform);
        if (root == null)
        {
            Debug.LogError("InflateDeflateMenuBuilder: Method Canvas has no RectTransform root.");
            return;
        }

        var sourceRow = FindObjectRecursive(root, "Surface Neighbors");
        var sourceAction = FindObjectRecursive(root, "Commit");
        if (sourceRow == null || sourceAction == null)
        {
            Debug.LogError("InflateDeflateMenuBuilder: Basic menu source objects were not found.");
            return;
        }

        DestroyExisting(root, PanelName);

        var panel = CreatePanel(root);
        var controller = canvas.GetComponent<InflateDeflateUI>();
        if (controller == null)
            controller = Undo.AddComponent<InflateDeflateUI>(canvas);

        controller.panelObject = panel.gameObject;

        var settings = Object.FindFirstObjectByType<EditingMethodRuntimeSettings>();

        var modeRow = CloneRow(sourceRow, panel, "Inflate Mode", 0.1152f);
        var mode = modeRow.gameObject.AddComponent<InflateDeflateModeUI>();
        mode.controller = controller;
        mode.target = settings;
        ConfigureStepperLikeRow(modeRow, "mode", null, "inflate", "deflate", true, out _, out mode.inflateButton, out mode.deflateButton);

        var radiusRow = CloneRow(sourceRow, panel, "Inflate Radius", 0.0176f);
        var radius = radiusRow.gameObject.AddComponent<InflateDeflateFloatStepperUI>();
        radius.controller = controller;
        radius.target = settings;
        radius.parameterKind = InflateDeflateFloatStepperUI.ParameterKind.Radius;
        radius.step = 0.01f;
        ConfigureStepperLikeRow(radiusRow, "radius", "0.10", "-", "+", false, out radius.text, out radius.minus, out radius.plus);

        var strengthRow = CloneRow(sourceRow, panel, "Inflate Strength", -0.08f);
        var strength = strengthRow.gameObject.AddComponent<InflateDeflateFloatStepperUI>();
        strength.controller = controller;
        strength.target = settings;
        strength.parameterKind = InflateDeflateFloatStepperUI.ParameterKind.Strength;
        strength.step = 0.01f;
        ConfigureStepperLikeRow(strengthRow, "strength", "0.20", "-", "+", false, out strength.text, out strength.minus, out strength.plus);

        var pickButton = CloneActionButton(sourceAction, panel, "Inflate Pick Point", "pick point", new Vector2(-0.07f, -0.181f));
        var cancelButton = CloneActionButton(sourceAction, panel, "Inflate Cancel", "cancel", new Vector2(0.075f, -0.181f));
        var pick = panel.gameObject.AddComponent<InflateDeflatePickUI>();
        pick.controller = controller;
        pick.pickPointButton = pickButton;
        pick.cancelButton = cancelButton;

        controller.statusText = CreateStatusLabel(panel);
        panel.gameObject.SetActive(false);

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        Debug.Log("InflateDeflateMenuBuilder: Inflate/Deflate menu was built from BasicTranslate UI objects.");
    }

    private static RectTransform ResolveUiRoot(Transform canvas)
    {
        if (canvas.childCount > 0 && canvas.GetChild(0) is RectTransform childRoot)
            return childRoot;

        return canvas as RectTransform;
    }

    private static RectTransform CreatePanel(RectTransform parent)
    {
        var panelObject = new GameObject(PanelName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelObject, "Create Inflate Deflate Panel");
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

    private static RectTransform CloneRow(GameObject source, RectTransform parent, string name, float anchoredY)
    {
        var clone = Object.Instantiate(source, parent, false);
        Undo.RegisterCreatedObjectUndo(clone, "Create Inflate Deflate Row");
        clone.name = name;

        var rect = clone.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, anchoredY);
        ClearButtons(clone);
        return rect;
    }

    private static Button CloneActionButton(GameObject source, RectTransform parent, string name, string label, Vector2 anchoredPosition)
    {
        var clone = Object.Instantiate(source, parent, false);
        Undo.RegisterCreatedObjectUndo(clone, "Create Inflate Deflate Action Button");
        clone.name = name;

        var rect = clone.GetComponent<RectTransform>();
        rect.anchoredPosition = anchoredPosition;

        var button = clone.GetComponent<Button>();
        ClearButton(button);
        SetButtonLabel(button, label);
        return button;
    }

    private static void ConfigureStepperLikeRow(
        RectTransform row,
        string title,
        string value,
        string minusLabel,
        string plusLabel,
        bool hideValue,
        out TMP_Text valueText,
        out Button minus,
        out Button plus)
    {
        var titleText = FindTextRecursive(row, "Title");
        if (titleText != null)
            titleText.text = title;

        valueText = FindTextRecursive(row, "CURR");
        if (valueText != null)
        {
            valueText.text = value ?? string.Empty;
            valueText.gameObject.SetActive(!hideValue);
        }

        minus = FindButtonRecursive(row, "Minus");
        plus = FindButtonRecursive(row, "Plus");

        ClearButton(minus);
        ClearButton(plus);
        SetButtonLabel(minus, minusLabel);
        SetButtonLabel(plus, plusLabel);
    }

    private static TMP_Text CreateStatusLabel(RectTransform parent)
    {
        var labelObject = new GameObject("Inflate Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(labelObject, "Create Inflate Deflate Status");
        labelObject.transform.SetParent(parent, false);

        var rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -0.255f);
        rect.sizeDelta = new Vector2(260f, 44f);
        rect.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

        var text = labelObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 3.5f;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.color = Color.white;
        text.text = string.Empty;
        return text;
    }

    private static void ClearButtons(GameObject root)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            ClearButton(buttons[i]);
    }

    private static void ClearButton(Button button)
    {
        if (button == null)
            return;

        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);

        button.onClick.RemoveAllListeners();
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        var text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            text.raycastTarget = false;
        }
    }

    private static void DestroyExisting(Transform root, string name)
    {
        var existing = FindObjectRecursive(root, name);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
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

    private static Button FindButtonRecursive(Transform parent, string name)
    {
        var target = FindObjectRecursive(parent, name);
        return target != null ? target.GetComponent<Button>() : null;
    }
}
