using System.Collections.Generic;
using TvmVr2.Api.Enums;
using TvmVr2.Client.Sequence;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Represents a center object
/// </summary>
public class CenterUI : MonoBehaviour
{
    /// <summary>
    /// Center hover event listeners
    /// </summary>
    public static List<ICenterHoverListener> hoverListeners = new List<ICenterHoverListener>();
    
    /// <summary>
    /// Center selection event listeners
    /// </summary>
    public static List<ICenterSelectionListener> selectionListeners = new List<ICenterSelectionListener>();

    private static int activeSelectionCount;

    public static bool HasActiveSelection => activeSelectionCount > 0;

    public static void ClearActiveSelections()
    {
        activeSelectionCount = 0;
    }

    /// <summary>
    /// Index of this center in the sequence
    /// </summary>
    public int centerIndex;

    /// <summary>
    /// Center mesh renderer
    /// </summary>
    public MeshRenderer meshRenderer;

    /// <summary>
    /// Material attached whent he center is not selected - template
    /// </summary>
    public Material normalMaterialTemplate;
    /// <summary>
    /// Material attached whent he center is selected 
    /// </summary>
    public Material selectedMaterial;
    /// <summary>
    /// Material attached whent he center is not selected
    /// </summary>
    public Material normalMaterial;

    /// <summary>
    /// Center color when center is highlighted
    /// </summary>
    [ColorUsage(true, true)]
    public Color highlightedColor;

    /// <summary>
    /// Normal state center color
    /// </summary>
    [ColorUsage(true, true)]
    public Color normalColor;

    private EditingMethodRuntimeSettings methodSettings;
    private bool isPersistentSelected;

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        meshRenderer.material = Instantiate(normalMaterialTemplate);
        normalMaterial = meshRenderer.material;
        methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();
        ApplyNormalColor();
    }

    /// <summary>
    /// Executes when an interactor enters the center
    /// </summary>
    /// <param name="e">Hover event</param>
    public void OnHoverEnter(HoverEnterEventArgs e)
    {
        if (Sequence.playing || !IsCenterInteractionMethodActive()) return;

        ApplyHighlightColor();

        foreach (var l in hoverListeners)
        {
            l.Notify(this, true);
        }
    }

    /// <summary>
    /// Executes when an interactor exits the center
    /// </summary>
    /// <param name="e">Hover event</param>
    public void OnHoverExit(HoverExitEventArgs e)
    {
        if (Sequence.playing || !IsCenterInteractionMethodActive()) return;

        if (!isPersistentSelected && meshRenderer.material != selectedMaterial)
            ApplyNormalColor();

        foreach (var l in hoverListeners)
        {
            l.Notify(this, false);
        }
    }

    /// <summary>
    /// Executes when an interactor selects the center
    /// </summary>
    /// <param name="e">Selection event</param>
    public void OnSelectEnter(SelectEnterEventArgs e)
    {
        if (Sequence.playing || !IsCenterInteractionMethodActive()) return;

        if (IsInflateDeflateActive())
        {
            foreach (var l in selectionListeners)
            {
                l.Notify(this, true);
            }

            ReleaseSelection(e);
            return;
        }

        activeSelectionCount++;
        meshRenderer.material = selectedMaterial;
        foreach (var l in selectionListeners)
        {
            l.Notify(this, true);
        }
    }

    /// <summary>
    /// Executes when an interactor deselects the center
    /// </summary>
    /// <param name="e">Selection event</param>
    public void OnSelectExit(SelectExitEventArgs e)
    {
        if (Sequence.playing || !IsCenterInteractionMethodActive()) return;

        if (IsInflateDeflateActive())
            return;

        activeSelectionCount = Mathf.Max(0, activeSelectionCount - 1);
        meshRenderer.material = normalMaterial;
        ApplyNormalColor();
        foreach (var l in selectionListeners)
        {
            l.Notify(this, false);
        }
    }

    /// <summary>
    /// Registers a selection listener
    /// </summary>
    /// <param name="l">The listener</param>
    public static void RegisterListener(ICenterSelectionListener l)
    {
        selectionListeners.Add(l);
    }

    /// <summary>
    /// Registers a hover listener
    /// </summary>
    /// <param name="l">The listener</param>
    public static void RegisterListener(ICenterHoverListener l)
    {
        hoverListeners.Add(l);
    }

    public void SetPersistentSelected(bool selected)
    {
        isPersistentSelected = selected;
        meshRenderer.material = selected ? selectedMaterial : normalMaterial;

        if (!selected)
            ApplyNormalColor();
    }

    private bool IsCenterInteractionMethodActive()
    {
        if (methodSettings == null)
            methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        if (methodSettings == null)
            return true;

        return methodSettings.CurrentMethod == MethodKind.BasicTranslate
            || methodSettings.CurrentMethod == MethodKind.InflateDeflate;
    }

    private bool IsInflateDeflateActive()
    {
        if (methodSettings == null)
            methodSettings = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        return methodSettings != null && methodSettings.CurrentMethod == MethodKind.InflateDeflate;
    }

    private static void ReleaseSelection(SelectEnterEventArgs args)
    {
        if (args?.manager == null || args.interactorObject == null || args.interactableObject == null)
            return;

        args.manager.SelectExit(args.interactorObject, args.interactableObject);
    }

    private void ApplyHighlightColor()
    {
        if (normalMaterial == null)
            return;

        if (normalMaterial.HasProperty("_BaseColor"))
            normalMaterial.SetColor("_BaseColor", highlightedColor);
        else if (normalMaterial.HasProperty("_Color"))
            normalMaterial.SetColor("_Color", highlightedColor);
    }

    private void ApplyNormalColor()
    {
        if (normalMaterial == null)
            return;

        if (normalMaterial.HasProperty("_BaseColor"))
            normalMaterial.SetColor("_BaseColor", normalColor);
        else if (normalMaterial.HasProperty("_Color"))
            normalMaterial.SetColor("_Color", normalColor);
    }

}
