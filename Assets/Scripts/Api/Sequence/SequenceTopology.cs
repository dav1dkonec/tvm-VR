using System;

/// <summary>
/// Immutable structural data shared by a sequence at runtime.
/// </summary>
public sealed class SequenceTopology
{
    public sealed class FrameTopology
    {
        public int FrameIndex { get; set; }
        public int CenterCount { get; set; }
        public int VertexCount { get; set; }
        public Face[] Faces { get; set; } = Array.Empty<Face>();
    }

    public FrameTopology[] Frames { get; set; } = Array.Empty<FrameTopology>();

    public int FrameCount => Frames?.Length ?? 0;

    public static SequenceTopology FromFrames(Frame[] frames)
    {
        if (frames == null || frames.Length == 0)
            return new SequenceTopology();

        var topologyFrames = new FrameTopology[frames.Length];
        for (var i = 0; i < frames.Length; i++)
        {
            var frame = frames[i];
            topologyFrames[i] = new FrameTopology
            {
                FrameIndex = i,
                CenterCount = frame?.centers?.Length ?? 0,
                VertexCount = frame?.vertices?.Length ?? 0,
                Faces = frame?.faces ?? Array.Empty<Face>()
            };
        }

        return new SequenceTopology
        {
            Frames = topologyFrames
        };
    }

    public FrameTopology GetFrameTopology(int frameIndex)
    {
        if (Frames == null || frameIndex < 0 || frameIndex >= Frames.Length)
            return null;

        return Frames[frameIndex];
    }
}
