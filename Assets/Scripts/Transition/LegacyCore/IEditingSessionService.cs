using CoreTvm;

namespace TvmVr2.Application.Sessions
{
    public interface IEditingSessionService
    {
        EditingSession CreateSession(
            Frame[] frames,
            SequenceSettings settings,
            string sourcePath,
            string sequenceName,
            int currentFrameIndex,
            EditingOptions options);

        void StoreLastEditRequest(EditingSession session, EditRequest request);

        void SyncFromLegacyFrames(
            EditingSession session,
            Frame[] frames,
            SequenceSettings settings,
            string sourcePath,
            string sequenceName,
            int currentFrameIndex,
            bool isDirty);

        void UpdateCurrentFrame(EditingSession session, int currentFrameIndex);
    }
}
