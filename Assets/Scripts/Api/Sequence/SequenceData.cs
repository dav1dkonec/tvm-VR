using System;

/// <summary>
/// Immutable sequence payload loaded from disk or inserted into the application.
/// </summary>
public sealed class SequenceData
{
    public string SequenceId { get; set; } = string.Empty;
    public SequenceSettings Settings { get; set; } = new SequenceSettings();
    public SequenceTopology Topology { get; set; } = new SequenceTopology();
    public Frame[] OriginalFrames { get; set; } = Array.Empty<Frame>();

    public int FrameCount => OriginalFrames?.Length ?? 0;

    public RuntimeState CreateRuntimeState()
    {
        return RuntimeState.From(this);
    }
}
