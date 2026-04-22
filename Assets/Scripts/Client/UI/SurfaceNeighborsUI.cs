using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TvmVr2.Client.Sequence;


/// <summary>
/// Surface deformation neighbors number picker
/// </summary>
public class SurfaceNeighborsUI : MonoBehaviour
{
    /// <summary>
    /// Object with the targeted attribute
    /// </summary>
    public EditingMethodRuntimeSettings target;

    /// <summary>
    /// Minus button
    /// </summary>
    public Button minus;
    /// <summary>
    /// Plus button
    /// </summary>
    public Button plus;
    /// <summary>
    /// Current value text
    /// </summary>
    public TMP_Text text;

    /// <summary>
    /// Minimum, maximum value
    /// </summary>
    public int min, max;

    /// <summary>
    /// Sets initial text
    /// </summary>
    void Start()
    {
        if (target == null)
            target = FindFirstObjectByType<EditingMethodRuntimeSettings>();

        if (target != null)
            text.text = "" + target.SurfaceNeighborCount;
    }

    /// <summary>
    /// Adds one
    /// </summary>
    public void OnPlusClicked()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (target == null)
            return;

        target.SetSurfaceNeighborCount(target.SurfaceNeighborCount + 1);
        text.text = "" + target.SurfaceNeighborCount;
        if (target.SurfaceNeighborCount == max) plus.enabled = false;
        minus.enabled = true;
    }

    /// <summary>
    /// Subtracts one
    /// </summary>
    public void OnMinusClicked()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        if (target == null)
            return;

        target.SetSurfaceNeighborCount(target.SurfaceNeighborCount - 1);
        text.text = "" + target.SurfaceNeighborCount;
        if (target.SurfaceNeighborCount == min) minus.enabled = false;
        plus.enabled = true;
    }
}
