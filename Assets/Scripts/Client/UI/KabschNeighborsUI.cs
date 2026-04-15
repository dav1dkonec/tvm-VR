using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TvmVr2.Client.Sequence;

/// <summary>
/// Kabsch neighbors number picker
/// </summary>
public class KabschNeighborsUI : MonoBehaviour
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
            text.text = "" + target.SequenceNeighborCount;
    }

    /// <summary>
    /// Adds one
    /// </summary>
    public void OnPlusClicked()
    {
        if (target == null)
            return;

        target.SetSequenceNeighborCount(target.SequenceNeighborCount + 1);
        text.text = "" + target.SequenceNeighborCount;
        if (target.SequenceNeighborCount == max) plus.enabled = false;
        minus.enabled = true;
    }

    /// <summary>
    /// Subtracts one
    /// </summary>
    public void OnMinusClicked()
    {
        if (target == null)
            return;

        target.SetSequenceNeighborCount(target.SequenceNeighborCount - 1);
        text.text = "" + target.SequenceNeighborCount;
        if (target.SequenceNeighborCount == min) minus.enabled = false;
        plus.enabled = true;
    }
}
