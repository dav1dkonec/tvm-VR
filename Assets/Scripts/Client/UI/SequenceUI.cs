using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System;

/// <summary>
/// Sequence canvas
/// </summary>
public class SequenceUI : MonoBehaviour
{
    /// <summary>
    /// Scroll view content
    /// </summary>
    public RectTransform contentTarget;
    
    /// <summary>
    /// Prefab for the button which loads a sequence
    /// </summary>
    public SequenceButtonUI contentPrefab;

    /// <summary>
    /// Sequence object
    /// </summary>
    public Sequence sequence;

    /// <summary>
    /// Adds a sequence loading button to the canveas
    /// </summary>
    /// <param name="sequenceName">The name of the sequence</param>
    /// <param name="sequencePath">The path to the sequence</param>
    public void Add(string sequenceName, string sequencePath)
    {
        var content = Instantiate(contentPrefab, contentTarget);
        content.sequence = sequence;
        content.sequenceName.text = sequenceName;
        content.sequencePath = sequencePath;
    }
}
