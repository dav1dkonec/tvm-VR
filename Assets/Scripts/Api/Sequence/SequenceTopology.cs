using System;

/// <summary>
/// Immutable structural data shared by a sequence at runtime.
/// </summary>
public sealed class SequenceTopology
{
    /// <summary>
    /// Structural data for one frame.
    /// </summary>
    public sealed class FrameTopology
    {
        /// <summary>
        /// Frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Number of volume centers.
        /// </summary>
        public int CenterCount { get; set; }

        /// <summary>
        /// Number of mesh vertices.
        /// </summary>
        public int VertexCount { get; set; }

        /// <summary>
        /// Mesh faces.
        /// </summary>
        public Face[] Faces { get; set; } = Array.Empty<Face>();
    }

    /// <summary>
    /// Topology data for all frames.
    /// </summary>
    public FrameTopology[] Frames { get; set; } = Array.Empty<FrameTopology>();

    /// <summary>
    /// Number of topology frames.
    /// </summary>
    public int FrameCount => Frames?.Length ?? 0;

    /// <summary>
    /// Creates sequence topology from frame data.
    /// </summary>
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

    /// <summary>
    /// Gets topology for the selected frame.
    /// </summary>
    public FrameTopology GetFrameTopology(int frameIndex)
    {
        if (Frames == null || frameIndex < 0 || frameIndex >= Frames.Length)
            return null;

        return Frames[frameIndex];
    }
}
