using System;
using System.Numerics;

public static class FrameSnapshot
{
    public static Frame[] Clone(Frame[] source)
    {
        if (source == null)
            return Array.Empty<Frame>();

        var clone = new Frame[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            clone[i] = CloneFrame(source[i]);
        }

        return clone;
    }

    private static Frame CloneFrame(Frame frame)
    {
        if (frame == null)
            return new Frame();

        return new Frame
        {
            centers = CloneVectors(frame.centers),
            centersUnedited = CloneVectors(frame.centersUnedited),
            vertices = CloneVectors(frame.vertices),
            verticesUnedited = CloneVectors(frame.verticesUnedited),
            faces = CloneFaces(frame.faces),
            nearestCentersDist = CloneFloatJagged(frame.nearestCentersDist),
            nearestCentersIndex = CloneIntJagged(frame.nearestCentersIndex)
        };
    }

    private static Vector3[] CloneVectors(Vector3[] source)
    {
        if (source == null)
            return Array.Empty<Vector3>();

        var clone = new Vector3[source.Length];
        Array.Copy(source, clone, source.Length);
        return clone;
    }

    private static Face[] CloneFaces(Face[] source)
    {
        if (source == null)
            return Array.Empty<Face>();

        var clone = new Face[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            clone[i] = new Face(source[i].V1, source[i].V2, source[i].V3);
        }

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
