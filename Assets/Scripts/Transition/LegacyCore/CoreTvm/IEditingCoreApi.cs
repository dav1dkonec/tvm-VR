namespace CoreTvm
{
    public interface IEditingCoreApi
    {
        EditResult ApplyEdit(SequenceData input, EditRequest request, EditingOptions options);
        SequenceData RebuildSurface(SequenceData input, EditingOptions options);
    }
}
