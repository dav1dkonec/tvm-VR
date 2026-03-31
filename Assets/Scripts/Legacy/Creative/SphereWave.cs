using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Animates centers procedurally
/// </summary>
public class SphereWave : MonoBehaviour
{
    /// <summary>
    /// Animation settings
    /// </summary>
    public SphereWaveSettings settings;
    
    /// <summary>
    /// The vector from the parent transform to this transform
    /// </summary>
    Vector3 initalDirection;

    /// <summary>
    /// The start coordinates of the object in polar coordinates
    /// </summary>
    Vector2 initalPolar;

    /// <summary>
    /// Initializes the direction from parent and polar coordinates
    /// </summary>
    void Start()
    {
        initalDirection = transform.position - transform.parent.position;
        
        // Convert direction from cartesian 
        initalPolar.y = Mathf.Atan2(initalDirection.x, initalDirection.z);
        var lengthXZ = new Vector2(initalDirection.x, initalDirection.z).magnitude;
        initalPolar.x = Mathf.Atan2(-initalPolar.y, lengthXZ);
    }

    /// <summary>
    /// Calculates a translation based on initial coordinates, time, and perlin noise
    /// </summary>
    void Update()
    {
        float noise = Mathf.PerlinNoise(initalPolar.x + Time.realtimeSinceStartup * settings.timeScale, initalPolar.y + Time.realtimeSinceStartup * settings.timeScale);
        float scale = 1 + (noise - 0.5f) * 2 * settings.maxScaleRelative;
        transform.position = transform.parent.position + initalDirection.normalized * settings.centerOffset + initalDirection * scale + Vector3.up * 1.5f;
    }
}
