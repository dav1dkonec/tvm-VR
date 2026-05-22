using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Commit button handling.
/// </summary>
public class CommitUI : MonoBehaviour
{
    /// <summary>
    /// Edited sequence.
    /// </summary>
    public Sequence sequence;

    /// <summary>
    /// Commits all pending edits.
    /// </summary>
    public void OnCommitClicked()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.CommitAllEdits();
    }
}
