using System.Numerics;
using CoreTvm;

public static class SequenceAdapter
{
    public static SequenceData FromLegacyFrames(Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName)
    {
        var sequence = new SequenceData
        {
            MeshFrames = new MeshFrameData[frames.Length],
            Centers = new Vector3[frames.Length][],
            Metadata = new SequenceMetadata
            {
                FrameRate = settings?.framerate ?? 24,
                SourcePath = sourcePath ?? string.Empty,
                SequenceName = sequenceName ?? string.Empty
            }
        };

        for (var i = 0; i < frames.Length; i++)
        {
            sequence.Centers[i] = (Vector3[])frames[i].centers.Clone();
            sequence.MeshFrames[i] = new MeshFrameData
            {
                Vertices = (Vector3[])frames[i].vertices.Clone(),
                Faces = ConvertFaces(frames[i].faces)
            };
        }

        return sequence;
    }

    public static void ApplyToLegacyFrames(SequenceData sequence, Frame[] frames)
    {
        if (sequence == null || frames == null)
            return;

        var frameCount = System.Math.Min(frames.Length, sequence.Centers.Length);
        frameCount = System.Math.Min(frameCount, sequence.MeshFrames.Length);

        for (var i = 0; i < frameCount; i++)
        {
            frames[i].centers = CloneVectors(sequence.Centers[i]);
            frames[i].vertices = CloneVectors(sequence.MeshFrames[i].Vertices);
            frames[i].faces = ConvertFaces(sequence.MeshFrames[i].Faces);
        }
    }

    private static FaceData[] ConvertFaces(Face[] faces)
    {
        var converted = new FaceData[faces.Length];
        for (var i = 0; i < faces.Length; i++)
        {
            converted[i] = new FaceData(faces[i].V1, faces[i].V2, faces[i].V3);
        }

        return converted;
    }

    private static Face[] ConvertFaces(FaceData[] faces)
    {
        var converted = new Face[faces.Length];
        for (var i = 0; i < faces.Length; i++)
        {
            converted[i] = new Face(faces[i].V1, faces[i].V2, faces[i].V3);
        }

        return converted;
    }

    private static Vector3[] CloneVectors(Vector3[] source)
    {
        var clone = new Vector3[source.Length];
        System.Array.Copy(source, clone, source.Length);
        return clone;
    }
}
