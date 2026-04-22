using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommitUI : MonoBehaviour
{
    public Sequence sequence;

    public void OnCommitClicked()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        sequence.CommitAllEdits();
    }
}
