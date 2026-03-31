using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

/// <summary>
/// A nearest-neighbors based surface deformation implementation
/// </summary>
public class NeighborhoodSurfaceDeformation : SurfaceDeformation
{
    /// <summary>
    /// Neighborhood size for optimal transformation
    /// </summary>
    public int Neighbors = 6;

    /// <summary>
    /// Initial max search distance for the KdTree
    /// </summary>
    public float InitialMaxSearchDistance = 0.1f;

    /// <summary>
    /// Epsilon - detects weight sums close to 0
    /// </summary>
    public float Epsilon = 0.0001f;

    /// <summary>
    /// The timer keeps track of time spent in the class
    /// </summary>
    public long timer;

    public static float maxShift = 0f;

    /// <summary>
    /// Applies the center deformations in frame F to the surface in frame F
    /// </summary>
    /// <param name="frame">Frame to deform.</param>
    /// <returns>Deformed positions of the vertices</returns>
    public override Vector3[] DeformSurface(Frame frame)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();

        var deformedVertices = new Vector3[frame.vertices.Length];
        var weights = new float[Neighbors - 1];


        for (int i = 0; i < frame.vertices.Length; i++)
        {
            (var indices, var distances) = (frame.nearestCentersIndex[i], frame.nearestCentersDist[i]);

            var shift = new Vector3();
            var weightSum = 0f;

            for (int j = 0; j < Neighbors - 1; j++)
            {
                weights[j] = 1f - distances[j] / distances[Neighbors - 1];
                weightSum += weights[j];
            }

            if (weightSum < Epsilon)
            {
                var weight = 1f / (Neighbors - 1);

                for (int j = 0; j < Neighbors - 1; j++)
                {
                    var cb = frame.centersUnedited[indices[j]];
                    var ca = frame.centers[indices[j]];
                    shift += (ca - cb) * weight;

                    if (shift.Length() > maxShift)
                        maxShift = shift.Length();
                }
            }
            else
            {
                for (int j = 0; j < Neighbors - 1; j++)
                {
                    var op = frame.centersUnedited[indices[j]];
                    var np = frame.centers[indices[j]];
                    shift += (np - op) * (weights[j] / weightSum);
                    
                    if (shift.Length() > maxShift)
                        maxShift = shift.Length();
                }
            }

            deformedVertices[i] = frame.verticesUnedited[i] + shift;
        }

        stopwatch.Stop();
        timer += stopwatch.ElapsedMilliseconds;

        return deformedVertices;
    }


    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public override void ResetTimer()
    {
        timer = 0;
    }
}
