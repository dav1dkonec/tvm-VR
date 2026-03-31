using System;
using System.Numerics;
using CoreTvm;

public static class SequenceDataSnapshot
{
    public static SequenceData Clone(SequenceData source)
    {
        if (source == null)
            return null;

        return new SequenceData
        {
            MeshFrames = CloneMeshFrames(source.MeshFrames),
            Centers = CloneCenters(source.Centers),
            Metadata = CloneMetadata(source.Metadata)
        };
    }

    private static MeshFrameData[] CloneMeshFrames(MeshFrameData[] meshFrames)
    {
        if (meshFrames == null)
            return Array.Empty<MeshFrameData>();

        var clone = new MeshFrameData[meshFrames.Length];
        for (var i = 0; i < meshFrames.Length; i++)
        {
            clone[i] = new MeshFrameData
            {
                Vertices = CloneVertices(meshFrames[i]?.Vertices),
                Faces = CloneFaces(meshFrames[i]?.Faces)
            };
        }

        return clone;
    }

    private static Vector3[][] CloneCenters(Vector3[][] centers)
    {
        if (centers == null)
            return Array.Empty<Vector3[]>();

        var clone = new Vector3[centers.Length][];
        for (var i = 0; i < centers.Length; i++)
        {
            clone[i] = CloneVertices(centers[i]);
        }

        return clone;
    }

    private static Vector3[] CloneVertices(Vector3[] vertices)
    {
        if (vertices == null)
            return Array.Empty<Vector3>();

        var clone = new Vector3[vertices.Length];
        Array.Copy(vertices, clone, vertices.Length);
        return clone;
    }

    private static FaceData[] CloneFaces(FaceData[] faces)
    {
        if (faces == null)
            return Array.Empty<FaceData>();

        var clone = new FaceData[faces.Length];
        Array.Copy(faces, clone, faces.Length);
        return clone;
    }

    private static SequenceMetadata CloneMetadata(SequenceMetadata metadata)
    {
        return new SequenceMetadata
        {
            FrameRate = metadata?.FrameRate ?? 0,
            SourcePath = metadata?.SourcePath ?? string.Empty,
            SequenceName = metadata?.SequenceName ?? string.Empty
        };
    }
}
