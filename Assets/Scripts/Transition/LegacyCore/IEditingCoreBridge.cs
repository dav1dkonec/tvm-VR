using CoreTvm;

namespace TvmVr2.Integration.CoreTvm
{
    public interface IEditingCoreBridge
    {
        EditResult ApplyEdit(EditingSession session, EditRequest request);
        SequenceData RebuildSurface(EditingSession session);
        UnifiedProfilingSnapshot GetUnifiedProfilingSnapshotOrDefault();
    }
}
