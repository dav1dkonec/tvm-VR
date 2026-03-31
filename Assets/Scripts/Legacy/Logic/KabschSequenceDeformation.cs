using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

/// <summary>
/// An implementation of sequence deformation using optimal transformation
/// </summary>
public class KabschSequenceDeformation : SequenceDeformation
{
    /// <summary>
    /// Neighborhood size for optimal transformation
    /// </summary>
    public int Neighbors = 4;

    /// <summary>
    /// Initial max search distance for the KdTree
    /// </summary>
    public float InitialMaxSearchDistance = 0.1f;

    /// <summary>
    /// The timer keeps track of time spent in the class
    /// </summary>
    public long timer;

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
    public override Vector3[][] DeformSequence(int centerIndex, int frameIndex, Vector3 translation, Vector3[] centersBefore, Vector3[][] allCenters, CenterDeformation centerDeformation)
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        int frameCount = allCenters.Length;
        Vector3[][] deformedFrames = new Vector3[frameCount][];

        var nearestCenters = GetNearestNeighbors(allCenters[frameIndex][centerIndex], centersBefore);

        Carry(centerIndex, frameIndex, -1, frameCount, nearestCenters, translation, allCenters, deformedFrames, centerDeformation);
        Carry(centerIndex, frameIndex, +1, frameCount, nearestCenters, translation, allCenters, deformedFrames, centerDeformation);

        deformedFrames[frameIndex] = centerDeformation.DeformCenters(centerIndex, translation, allCenters[frameIndex]);

        stopwatch.Stop();
        timer += stopwatch.ElapsedMilliseconds;

        return deformedFrames;
    }

    /// <summary>
    /// Carries the translation vector through the sequence, deforming each frame progressively
    /// </summary>
    /// <param name="centerIndex">Index of the center C in which the deformation originally occurred</param>
    /// <param name="frameIndex">Index of the frame F in which the deformation occurred</param>
    /// <param name="frameDirection">Direction in which to progress to next frame</param>
    /// <param name="frameCount">Total frame count</param>
    /// <param name="nearestCenters">Nearest center indices to the original vector</param>
    /// <param name="translation">The translation vector by which center C moved</param>
    /// <param name="allCenters">Center positions from the whole sequence</param>
    /// <param name="deformedFrames">Output array to build</param>
    private void Carry(int centerIndex, int frameIndex, int frameDirection, int frameCount, int[] nearestCenters,
        Vector3 translation, Vector3[][] allCenters, Vector3[][] deformedFrames, CenterDeformation centerDeformation)
    {
        int nextFrameIndex = frameIndex + frameDirection;

        bool isBeforeSequence = nextFrameIndex < 0;
        bool isAfterSequence = nextFrameIndex >= frameCount;

        // Recursion stop condition
        if (isBeforeSequence || isAfterSequence)
            return;

        var P = Kabsch.MatrixFrom(centerIndex, nearestCenters, allCenters[frameIndex]);
        var Q = Kabsch.MatrixFrom(centerIndex, nearestCenters, allCenters[nextFrameIndex]);

        var avgP = Kabsch.Avg(P);
        var avgQ = Kabsch.Avg(Q);

        Kabsch.Subtract(P, avgP);
        Kabsch.Subtract(Q, avgQ);

        var R = Kabsch.GetRotation(P, Q);
        var D = Kabsch.GetDirection(translation);
        var RD = R * D;

        var rotatedTranslation = new Vector3(RD[0], RD[1], RD[2]);
        Carry(centerIndex, nextFrameIndex, frameDirection, frameCount, nearestCenters, rotatedTranslation, allCenters, deformedFrames, centerDeformation);

        deformedFrames[nextFrameIndex] = centerDeformation.DeformCenters(centerIndex, rotatedTranslation, allCenters[nextFrameIndex]);
    }

    /// <summary>
    /// Finds the indices of nearest neighbors to deformed point
    /// </summary>
    /// <param name="v">Deformed point</param>
    /// <param name="centersBefore">Centers</param>
    /// <returns>Nearest neighbor indices</returns>
    public int[] GetNearestNeighbors(Vector3 v, Vector3[] centersBefore)
    {
        // Repeat search until a sufficient number of neighbors is found
        var kdTree = new KDTree(centersBefore);
        var maxSearchDistance = InitialMaxSearchDistance;
        var indices = kdTree.findAllCloserThan(v, maxSearchDistance);

        while (indices.Count < Neighbors)
        {
            maxSearchDistance *= 2;
            indices = kdTree.findAllCloserThan(v, maxSearchDistance);
        }

        // Sort neighbors
        var distances = new List<float>();

        for (int i = 0; i < indices.Count; i++)
        {
            distances.Add(Vector3.Distance(v, centersBefore[indices[i]]));
        }

        var distancesArr = distances.ToArray();
        var indicesArr = indices.ToArray();
        Array.Sort(distancesArr, indicesArr);

        return indicesArr;
    }

    /// <summary>
    /// Reset the timer which calculates how much time had been spent in the class
    /// </summary>
    public override void ResetTimer()
    {
        timer = 0;
    }
}
