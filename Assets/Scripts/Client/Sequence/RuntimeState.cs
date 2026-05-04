using System;

/// <summary>
/// Mutable working copy of a sequence used by the runtime editing flow.
/// </summary>
public sealed class RuntimeState
{
    public SequenceData SequenceData { get; private set; }
    public SequenceTopology Topology => SequenceData?.Topology;
    public Frame[] Frames { get; private set; } = Array.Empty<Frame>();

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
    }

    public void SetFrames(Frame[] frames)
    {
        Frames = frames ?? Array.Empty<Frame>();
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
