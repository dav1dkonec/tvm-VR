using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Contains references to classes that execute steps of the editing pipeline
/// as well as logic for executing the pipeline
/// </summary>
public class Brush : UnityEngine.MonoBehaviour
{

    /// <summary>
    /// Center deformation provider
    /// </summary>
    public CenterDeformation centerDeformation;

    /// <summary>
    /// Sequence deformation provider
    /// </summary>
    public SequenceDeformation sequenceDeformation;

    /// <summary>
    /// Surface deformation provider
    /// </summary>
    public SurfaceDeformation surfaceDeformation;

    /// <summary>
    /// Counts editing actions
    /// </summary>
    public int edits;

    /// <summary>
    /// Executes an editing action by making calls to the specific pipeline components
    /// </summary>
    /// <param name="center">The edited center position</param>
    /// <param name="centerIndex">Edited center index</param>
    /// <param name="frameIndex">Edited frame index</param>
    /// <param name="frames">All frames of the sequence</param>
    /// <returns></returns>
    public bool Commit(UnityEngine.Vector3 center, int centerIndex, int frameIndex, Frame[] frames)
    {
        //centerDeformation.ResetTimer();
        //sequenceDeformation.ResetTimer();
        //surfaceDeformation.ResetTimer();

        var allCenters = new Vector3[frames.Length][];

        for (int i = 0; i < frames.Length; i++)
        {
            allCenters[i] = frames[i].centers;
        }

        var p = center;
        var pv = new Vector3(p.x, p.y, p.z);
        var translation = pv - frames[frameIndex].centers[centerIndex];

        var deformedSequence = sequenceDeformation.DeformSequence(centerIndex, frameIndex, translation, allCenters[frameIndex], allCenters, centerDeformation);

        for (int i = 0; i < frames.Length; i++)
        {
            frames[i].centers = deformedSequence[i];
        }

        edits++;

        return true;
    }

    public bool CommitAll(Frame[] frames)
    {
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i].vertices = surfaceDeformation.DeformSurface(frames[i]);
        }
        return true;
    }
}
