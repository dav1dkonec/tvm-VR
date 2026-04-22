using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// A loadable sequence canvas representation
/// </summary>
public class SequenceButtonUI : MonoBehaviour
{
    /// <summary>
    /// Sequence name text
    /// </summary>
    public TMP_Text sequenceName;

    /// <summary>
    /// Sequence object
    /// </summary>
    internal Sequence sequence;

    /// <summary>
    ///  Path to the sequence to be loaded
    /// </summary>
    internal string sequencePath;

    /// <summary>
    /// Starts loading a sequence
    /// </summary>
    public void OnClick()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        FindFirstObjectByType<PlaybackUI>().Enable();
        sequence.Load(sequencePath, sequenceName.text);
    }
}
