using CoreTvm;

public sealed class EditingCoreAdapter
{
    private readonly IEditingCoreApi _coreApi;

    public EditingCoreAdapter(IEditingCoreApi coreApi)
    {
        _coreApi = coreApi;
    }

    public EditResult ApplyEdit(SequenceData sequence, EditRequest request, EditingOptions options)
    {
        return _coreApi.ApplyEdit(sequence, request, options);
    }

    public SequenceData RebuildSurface(SequenceData sequence, EditingOptions options)
    {
        return _coreApi.RebuildSurface(sequence, options);
    }

    public UnifiedProfilingSnapshot GetUnifiedProfilingSnapshotOrDefault()
    {
        if (_coreApi is PrototypeUnifiedEditingCore prototype)
            return prototype.LastProfile ?? new UnifiedProfilingSnapshot();

        return new UnifiedProfilingSnapshot();
    }
}
