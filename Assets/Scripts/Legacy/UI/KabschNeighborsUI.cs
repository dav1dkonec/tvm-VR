using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Kabsch neighbors number picker
/// </summary>
public class KabschNeighborsUI : MonoBehaviour
{
    /// <summary>
    /// Object with the targeted attribute
    /// </summary>
    public KabschSequenceDeformation target;

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
        text.text = "" + target.Neighbors;
    }

    /// <summary>
    /// Adds one
    /// </summary>
    public void OnPlusClicked()
    {
        target.Neighbors += 1;
        text.text = "" + target.Neighbors;
        if (target.Neighbors == max) plus.enabled = false;
        minus.enabled = true;
    }

    /// <summary>
    /// Subtracts one
    /// </summary>
    public void OnMinusClicked()
    {
        target.Neighbors -= 1;
        text.text = "" + target.Neighbors;
        if (target.Neighbors == min) minus.enabled = false;
        plus.enabled = true;
    }
}
