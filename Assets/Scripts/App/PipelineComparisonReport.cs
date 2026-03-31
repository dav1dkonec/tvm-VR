using System;

public sealed class PipelineComparisonReport
{
    public string SequenceName { get; set; } = string.Empty;
    public int FrameIndex { get; set; }
    public int[] EditedCenterIndices { get; set; } = Array.Empty<int>();
    public int LegacyAffectedFrameCount { get; set; }
    public int UnifiedAffectedFrameCount { get; set; }
    public int ComparedFrameCount { get; set; }
    public int LegacyAffectedFrameMin { get; set; } = -1;
    public int LegacyAffectedFrameMax { get; set; } = -1;
    public int UnifiedAffectedFrameMin { get; set; } = -1;
    public int UnifiedAffectedFrameMax { get; set; } = -1;
    public float MeanCenterDelta { get; set; }
    public float MaxCenterDelta { get; set; }
    public float MeanVertexDelta { get; set; }
    public float MaxVertexDelta { get; set; }
    public int ComparedCenterCount { get; set; }
    public int ComparedVertexCount { get; set; }
    public float LegacyRuntimeMilliseconds { get; set; }
    public float LegacyCenterMilliseconds { get; set; }
    public float LegacyPropagationMilliseconds { get; set; }
    public float LegacySurfaceMilliseconds { get; set; }
    public float UnifiedRuntimeMilliseconds { get; set; }
    public float UnifiedAffinityMilliseconds { get; set; }
    public float UnifiedCenterMilliseconds { get; set; }
    public float UnifiedPropagationMilliseconds { get; set; }
    public float UnifiedSurfaceMilliseconds { get; set; }
}
