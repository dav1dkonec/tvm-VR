using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reset positions button press handling
/// </summary>
public class ResetPositionUI : MonoBehaviour
{
    /// <summary>
    /// The object being moved
    /// </summary>
    public GameObject positionable;

    /// <summary>
    /// Initial position
    /// </summary>
    Vector3 position;

    /// <summary>
    /// Initial rotation
    /// </summary>
    Quaternion rotation;

    /// <summary>
    /// Initial scale
    /// </summary>
    Vector3 scale;

    /// <summary>
    /// Saves initial values
    /// </summary>
    void Start()
    {
        position = positionable.transform.localPosition;
        rotation = positionable.transform.localRotation;
        scale = positionable.transform.localScale;
    }

    /// <summary>
    /// Resets the values
    /// </summary>
    public void OnClicked()
    {
        if (InflateDeflateUI.BlockIfPickActive())
            return;

        positionable.transform.localPosition = position;
        positionable.transform.localRotation = rotation;
        positionable.transform.localScale = scale;
    }
}
