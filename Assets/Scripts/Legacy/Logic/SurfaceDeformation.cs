using System;
using System.Numerics;

/// <summary>
/// Represents a deformation algorithm which applies the changes from the centers in one frame to the mesh for this frame
/// </summary>
public abstract class SurfaceDeformation : UnityEngine.MonoBehaviour
{
    /// <summary>
    /// Applies the center deformations in frame F to the surface in frame F
    /// </summary>
    /// <param name="frame">Frame to deform.</param>
    /// <returns>Deformed positions of the vertices</returns>
    public abstract Vector3[] DeformSurface(Frame frame);

    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public abstract void ResetTimer();
}

