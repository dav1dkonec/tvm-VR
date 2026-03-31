using CoreTvm;

public sealed class LegacyPipelineResult
{
    public SequenceData Sequence { get; set; }
    public int[] AffectedFrames { get; set; } = new int[0];
    public LegacyProfilingSnapshot Profiling { get; set; } = new LegacyProfilingSnapshot();
}
