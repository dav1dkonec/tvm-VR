using System.Numerics;

/// <summary>
/// Represents a deformation algorithm which applies the changes from one frame to the entire sequence
/// </summary>
public abstract class SequenceDeformation : UnityEngine.MonoBehaviour
{
    /// <summary>
    /// Applies the deformation of one frame to all frames of the sequence
    /// </summary>
    /// <param name="centerIndex">Index of the center C in which the deformation originally occurred</param>
    /// <param name="frameIndex">Index of the frame F in which the deformation occurred</param>
    /// <param name="translation">The translation vector by which center C moved</param>
    /// <param name="centersBefore">Center postions in frame F before deformation</param>
    /// <param name="allCenters">Center positions from the whole sequence</param>
    /// <param name="centerDeformation">Center deformation to use</param>
    /// <returns>Deformed centers for the whole sequence</returns>
    public abstract Vector3[][] DeformSequence(int centerIndex, int frameIndex, Vector3 translation, Vector3[] centersBefore, Vector3[][] allCenters, CenterDeformation centerDeformation);

    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public abstract void ResetTimer();
}
