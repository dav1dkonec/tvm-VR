using System.Numerics;

/// <summary>
/// Represents a deformation algorithm which applies the translation of one center to the entire frame
/// </summary>
public abstract class CenterDeformation : UnityEngine.MonoBehaviour
{
    /// <summary>
    /// Deforms the centers within one frame
    /// </summary>
    /// <param name="centerIndex">Translated center index</param>
    /// <param name="translation">Translation vector</param>
    /// <param name="centers">Center set</param>
    /// <returns>Deformed center positions</returns>
    public abstract Vector3[] DeformCenters(int centerIndex, Vector3 translation, Vector3[] centers);

    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public abstract void ResetTimer();
}
