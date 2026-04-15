using CoreTvm;

namespace TvmVr2.Integration.CoreTvm
{
    public sealed class CoreTvmEditingBridge : IEditingCoreBridge
    {
        private readonly EditingCoreAdapter _coreAdapter;

        public CoreTvmEditingBridge(EditingCoreAdapter coreAdapter)
        {
            _coreAdapter = coreAdapter;
        }

        public EditResult ApplyEdit(EditingSession session, EditRequest request)
        {
            if (session == null)
                return null;

            var result = _coreAdapter.ApplyEdit(session.Sequence, request, session.Options);
            session.LastEditRequest = request;
            return result;
        }

        public SequenceData RebuildSurface(EditingSession session)
        {
            if (session == null)
                return null;

            return _coreAdapter.RebuildSurface(session.Sequence, session.Options);
        }

        public UnifiedProfilingSnapshot GetUnifiedProfilingSnapshotOrDefault()
        {
            return _coreAdapter.GetUnifiedProfilingSnapshotOrDefault();
        }
    }
}
