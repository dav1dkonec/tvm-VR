using CoreTvm;

namespace TvmVr2.Application.Sessions
{
    public sealed class EditingSessionService : IEditingSessionService
    {
        public EditingSession CreateSession(
            Frame[] frames,
            SequenceSettings settings,
            string sourcePath,
            string sequenceName,
            int currentFrameIndex,
            EditingOptions options)
        {
            return new EditingSession
            {
                Sequence = SequenceAdapter.FromLegacyFrames(frames, settings, sourcePath, sequenceName),
                Options = options,
                CurrentFrameIndex = currentFrameIndex,
                IsDirty = false
            };
        }

        public void StoreLastEditRequest(EditingSession session, EditRequest request)
        {
            if (session == null)
                return;

            session.LastEditRequest = request;
        }

        public void SyncFromLegacyFrames(
            EditingSession session,
            Frame[] frames,
            SequenceSettings settings,
            string sourcePath,
            string sequenceName,
            int currentFrameIndex,
            bool isDirty)
        {
            if (session == null)
                return;

            session.Sequence = SequenceAdapter.FromLegacyFrames(frames, settings, sourcePath, sequenceName);
            session.CurrentFrameIndex = currentFrameIndex;
            session.IsDirty = isDirty;
        }

        public void UpdateCurrentFrame(EditingSession session, int currentFrameIndex)
        {
            if (session == null)
                return;

            session.CurrentFrameIndex = currentFrameIndex;
        }
    }
}
