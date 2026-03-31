using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Settings for the sphere wave animation
/// </summary>
public class SphereWaveSettings : MonoBehaviour
{
    /// <summary>
    /// Scales the size of the effect
    /// </summary>
    public float maxScaleRelative = 0.05f;

    /// <summary>
    /// Changes animation speed
    /// </summary>
    public float timeScale = 0.25f;

    /// <summary>
    /// Offset from the parent transform
    /// </summary>
    public float centerOffset = 0.25f;
}
