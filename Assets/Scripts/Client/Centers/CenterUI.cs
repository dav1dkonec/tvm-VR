using System.Collections.Generic;
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

    /// <summary>
    /// Initialization
    /// </summary>
    void Start()
    {
        meshRenderer.material = Instantiate(normalMaterialTemplate);
        normalMaterial = meshRenderer.material;
    }

    /// <summary>
    /// Executes when an interactor enters the center
    /// </summary>
    /// <param name="e">Hover event</param>
    public void OnHoverEnter(HoverEnterEventArgs e)
    {
        if (Sequence.playing) return;

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
        if (Sequence.playing) return;

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
        if (Sequence.playing) return;

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
        if (Sequence.playing) return;

        activeSelectionCount = Mathf.Max(0, activeSelectionCount - 1);
        meshRenderer.material = normalMaterial;
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

}
