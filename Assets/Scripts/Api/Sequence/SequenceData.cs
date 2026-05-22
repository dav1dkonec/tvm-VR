using System;

/// <summary>
/// Immutable sequence payload loaded from disk or inserted into the application.
/// </summary>
public sealed class SequenceData
{
    /// <summary>
    /// Sequence identifier.
    /// </summary>
    public string SequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence settings.
    /// </summary>
    public SequenceSettings Settings { get; set; } = new SequenceSettings();

    /// <summary>
    /// Sequence topology.
    /// </summary>
    public SequenceTopology Topology { get; set; } = new SequenceTopology();

    /// <summary>
    /// Original loaded frames.
    /// </summary>
    public Frame[] OriginalFrames { get; set; } = Array.Empty<Frame>();

    /// <summary>
    /// Number of sequence frames.
    /// </summary>
    public int FrameCount => OriginalFrames?.Length ?? 0;

    /// <summary>
    /// Creates mutable runtime state.
    /// </summary>
    public RuntimeState CreateRuntimeState()
    {
        return RuntimeState.From(this);
    }
}
