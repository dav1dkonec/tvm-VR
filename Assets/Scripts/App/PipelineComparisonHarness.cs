using System;
using System.Diagnostics;
using CoreTvm;

public sealed class PipelineComparisonHarness
{
    private readonly IEditingCoreApi _unifiedCore;
    private readonly PipelineComparisonService _comparisonService;

    public PipelineComparisonHarness()
        : this(new PrototypeUnifiedEditingCore(), new PipelineComparisonService())
    {
    }

    public PipelineComparisonHarness(IEditingCoreApi unifiedCore, PipelineComparisonService comparisonService)
    {
        _unifiedCore = unifiedCore ?? throw new ArgumentNullException(nameof(unifiedCore));
        _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
    }

    public PipelineComparisonReport CompareSingleEdit(
        SequenceData preEditSequence,
        SequenceData legacyPostEditSequence,
        EditRequest request,
        EditingOptions options,
        int[] legacyAffectedFrames = null)
    {
        if (preEditSequence == null)
            throw new ArgumentNullException(nameof(preEditSequence));

        if (legacyPostEditSequence == null)
            throw new ArgumentNullException(nameof(legacyPostEditSequence));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var inputSnapshot = SequenceDataSnapshot.Clone(preEditSequence);
        var stopwatch = Stopwatch.StartNew();
        var unifiedResult = _unifiedCore.ApplyEdit(inputSnapshot, request, options ?? new EditingOptions());
        stopwatch.Stop();

        var report = _comparisonService.Compare(
            legacyPostEditSequence,
            legacyAffectedFrames ?? Array.Empty<int>(),
            unifiedResult.Sequence,
            unifiedResult.AffectedFrames,
            request);

        report.UnifiedRuntimeMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds;

        if (_unifiedCore is PrototypeUnifiedEditingCore profiledCore)
        {
            report.UnifiedRuntimeMilliseconds = profiledCore.LastProfile.TotalMilliseconds;
            report.UnifiedAffinityMilliseconds = profiledCore.LastProfile.AffinityMilliseconds;
            report.UnifiedCenterMilliseconds = profiledCore.LastProfile.CenterMilliseconds;
            report.UnifiedPropagationMilliseconds = profiledCore.LastProfile.PropagationMilliseconds;
            report.UnifiedSurfaceMilliseconds = profiledCore.LastProfile.SurfaceMilliseconds;
        }

        return report;
    }
}
