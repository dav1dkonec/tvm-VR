using System;
using System.Collections.Generic;

/// <summary>
/// Mutable working copy of a sequence used by the runtime editing flow.
/// </summary>
public sealed class RuntimeState
{
    public SequenceData SequenceData { get; private set; }
    public SequenceTopology Topology => SequenceData?.Topology;
    public Frame[] Frames { get; private set; } = Array.Empty<Frame>();
    public int[] FrameRevision { get; private set; } = Array.Empty<int>();
    public HashSet<int> DirtyFrames { get; } = new HashSet<int>();

    public static RuntimeState From(SequenceData data)
    {
        var state = new RuntimeState();
        state.ResetFrom(data);
        return state;
    }

    public void ResetFrom(SequenceData data)
    {
        SequenceData = data;
        Frames = CloneRuntimeFrames(data?.OriginalFrames, data?.Topology);
        FrameRevision = Frames != null ? new int[Frames.Length] : Array.Empty<int>();
        DirtyFrames.Clear();
    }

    public void SetFrames(Frame[] frames)
    {
        Frames = frames ?? Array.Empty<Frame>();
        if (FrameRevision == null || FrameRevision.Length != Frames.Length)
            FrameRevision = new int[Frames.Length];
        DirtyFrames.Clear();
    }

    public void MarkFrameEdited(int frameIndex)
    {
        if (Frames == null || frameIndex < 0 || frameIndex >= Frames.Length)
            return;

        FrameRevision[frameIndex]++;
        DirtyFrames.Add(frameIndex);
    }

    public void MarkFramesEdited(IEnumerable<int> frameIndices)
    {
        if (frameIndices == null)
            return;

        foreach (var frameIndex in frameIndices)
            MarkFrameEdited(frameIndex);
    }

    public void MarkAllFramesEdited()
    {
        if (Frames == null)
            return;

        for (var i = 0; i < Frames.Length; i++)
            MarkFrameEdited(i);
    }

    public int[] ConsumeDirtyFrames()
    {
        if (DirtyFrames.Count == 0)
            return Array.Empty<int>();

        var frames = new int[DirtyFrames.Count];
        DirtyFrames.CopyTo(frames);
        DirtyFrames.Clear();
        return frames;
    }

    public int GetFrameRevision(int frameIndex)
    {
        if (FrameRevision == null || frameIndex < 0 || frameIndex >= FrameRevision.Length)
            return 0;

        return FrameRevision[frameIndex];
    }

    private static Frame[] CloneRuntimeFrames(Frame[] source, SequenceTopology topology)
    {
        if (source == null || source.Length == 0)
            return Array.Empty<Frame>();

        var clone = new Frame[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var frame = source[i];
            var topologyFrame = topology?.GetFrameTopology(i);

            clone[i] = new Frame
            {
                centers = CloneVectors(frame?.centers),
                centersUnedited = CloneVectors(frame?.centersUnedited),
                vertices = CloneVectors(frame?.vertices),
                verticesUnedited = CloneVectors(frame?.verticesUnedited),
                faces = topologyFrame?.Faces ?? frame?.faces ?? Array.Empty<Face>(),
                nearestCentersDist = CloneFloatJagged(frame?.nearestCentersDist),
                nearestCentersIndex = CloneIntJagged(frame?.nearestCentersIndex)
            };
        }

        return clone;
    }

    private static System.Numerics.Vector3[] CloneVectors(System.Numerics.Vector3[] source)
    {
        if (source == null)
            return Array.Empty<System.Numerics.Vector3>();

        var clone = new System.Numerics.Vector3[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static float[][] CloneFloatJagged(float[][] source)
    {
        if (source == null)
            return Array.Empty<float[]>();

        var clone = new float[source.Length][];
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
            {
                clone[i] = Array.Empty<float>();
                continue;
            }

            clone[i] = new float[source[i].Length];
            Array.Copy(source[i], clone[i], source[i].Length);
        }

        return clone;
    }

    private static int[][] CloneIntJagged(int[][] source)
    {
        if (source == null)
            return Array.Empty<int[]>();

        var clone = new int[source.Length][];
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == null)
            {
                clone[i] = Array.Empty<int>();
                continue;
            }

            clone[i] = new int[source[i].Length];
            Array.Copy(source[i], clone[i], source[i].Length);
        }

        return clone;
    }
}
