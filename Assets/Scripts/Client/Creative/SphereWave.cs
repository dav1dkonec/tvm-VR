using UnityEngine;

/// <summary>
/// Animates centers procedurally.
/// </summary>
public class SphereWave : MonoBehaviour
{
    /// <summary>
    /// Animation settings.
    /// </summary>
    public SphereWaveSettings settings;
    
    /// <summary>
    /// The vector from the parent transform to this transform.
    /// </summary>
    private Vector3 initialDirection;

    /// <summary>
    /// The start coordinates of the object in polar coordinates.
    /// </summary>
    private Vector2 initialPolar;

    /// <summary>
    /// Initializes the direction from parent and polar coordinates.
    /// </summary>
    private void Start()
    {
        initialDirection = transform.position - transform.parent.position;
        
        // Convert direction from cartesian coordinates.
        initialPolar.y = Mathf.Atan2(initialDirection.x, initialDirection.z);
        var lengthXZ = new Vector2(initialDirection.x, initialDirection.z).magnitude;
        initialPolar.x = Mathf.Atan2(-initialPolar.y, lengthXZ);
    }

    /// <summary>
    /// Calculates a translation based on initial coordinates, time, and perlin noise.
    /// </summary>
    private void Update()
    {
        float noise = Mathf.PerlinNoise(initialPolar.x + Time.realtimeSinceStartup * settings.timeScale, initialPolar.y + Time.realtimeSinceStartup * settings.timeScale);
        float scale = 1 + (noise - 0.5f) * 2 * settings.maxScaleRelative;
        transform.position = transform.parent.position + initialDirection.normalized * settings.centerOffset + initialDirection * scale + Vector3.up * 1.5f;
    }
}
