using CoreTvm;
using UnityEngine;

public sealed class SequenceEditingService
{
    private readonly EditingCoreAdapter _coreAdapter;

    public SequenceEditingService(EditingCoreAdapter coreAdapter)
    {
        _coreAdapter = coreAdapter;
    }

    public EditingSession CreateSession(Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, int currentFrameIndex, EditingOptions options)
    {
        return new EditingSession
        {
            Sequence = SequenceAdapter.FromLegacyFrames(frames, settings, sourcePath, sequenceName),
            Options = options,
            CurrentFrameIndex = currentFrameIndex,
            IsDirty = false
        };
    }

    public EditRequest CreateSingleCenterEditRequest(EditingSession session, int frameIndex, int centerIndex, UnityEngine.Vector3 newPosition)
    {
        return new EditRequest
        {
            FrameIndex = frameIndex,
            CenterIndices = new[] { centerIndex },
            NewCenterPositions = new[]
            {
                new System.Numerics.Vector3(newPosition.x, newPosition.y, newPosition.z)
            }
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

    public bool CommitLegacyEdit(EditingSession session, Brush brush, Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, int currentFrameIndex, CenterUI center)
    {
        var position = center.transform.localPosition;
        var request = CreateSingleCenterEditRequest(session, currentFrameIndex, center.centerIndex, position);

        StoreLastEditRequest(session, request);

        var committed = brush.Commit(position, center.centerIndex, currentFrameIndex, frames);
        SyncFromLegacyFrames(session, frames, settings, sourcePath, sequenceName, currentFrameIndex, true);
        return committed;
    }

    public bool CommitLegacyEdit(EditingSession session, Brush brush, Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, int currentFrameIndex, int centerIndex, UnityEngine.Vector3 position)
    {
        var request = CreateSingleCenterEditRequest(session, currentFrameIndex, centerIndex, position);

        StoreLastEditRequest(session, request);

        var committed = brush.Commit(position, centerIndex, currentFrameIndex, frames);
        SyncFromLegacyFrames(session, frames, settings, sourcePath, sequenceName, currentFrameIndex, true);
        return committed;
    }

    public bool CommitAllLegacyEdits(EditingSession session, Brush brush, Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, int currentFrameIndex)
    {
        var committed = brush.CommitAll(frames);
        SyncFromLegacyFrames(session, frames, settings, sourcePath, sequenceName, currentFrameIndex, true);
        return committed;
    }

    public bool CommitUnifiedEdit(EditingSession session, Frame[] frames, int currentFrameIndex, CenterUI center)
    {
        if (session == null)
            return false;

        var position = center.transform.localPosition;
        var request = CreateSingleCenterEditRequest(session, currentFrameIndex, center.centerIndex, position);
        var result = ApplyEdit(session, request);
        if (result?.Sequence == null)
            return false;

        session.Sequence = result.Sequence;
        session.CurrentFrameIndex = currentFrameIndex;
        session.IsDirty = true;
        SequenceAdapter.ApplyToLegacyFrames(result.Sequence, frames);
        return true;
    }

    public bool CommitUnifiedEdit(EditingSession session, Frame[] frames, int currentFrameIndex, int centerIndex, UnityEngine.Vector3 position)
    {
        if (session == null)
            return false;

        var request = CreateSingleCenterEditRequest(session, currentFrameIndex, centerIndex, position);
        var result = ApplyEdit(session, request);
        if (result?.Sequence == null)
            return false;

        session.Sequence = result.Sequence;
        session.CurrentFrameIndex = currentFrameIndex;
        session.IsDirty = true;
        SequenceAdapter.ApplyToLegacyFrames(result.Sequence, frames);
        return true;
    }

    public bool CommitUnifiedRequest(EditingSession session, Frame[] frames, EditRequest request)
    {
        if (session == null)
            return false;

        var result = ApplyEdit(session, request);
        if (result?.Sequence == null)
            return false;

        session.Sequence = result.Sequence;
        session.CurrentFrameIndex = request.FrameIndex;
        session.IsDirty = true;
        SequenceAdapter.ApplyToLegacyFrames(result.Sequence, frames);
        return true;
    }

    public bool CommitLegacyRequest(EditingSession session, Brush brush, Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, EditRequest request)
    {
        if (request == null || request.CenterIndices == null || request.NewCenterPositions == null || request.CenterIndices.Length != 1 || request.NewCenterPositions.Length != 1)
            return false;

        var position = request.NewCenterPositions[0];
        var unityPosition = new UnityEngine.Vector3(position.X, position.Y, position.Z);
        StoreLastEditRequest(session, request);

        var committed = brush.Commit(unityPosition, request.CenterIndices[0], request.FrameIndex, frames);
        if (committed)
            committed = brush.CommitAll(frames);
        SyncFromLegacyFrames(session, frames, settings, sourcePath, sequenceName, request.FrameIndex, true);
        return committed;
    }

    public bool CommitLegacyBatchRequest(EditingSession session, Brush brush, Frame[] frames, SequenceSettings settings, string sourcePath, string sequenceName, EditRequest request)
    {
        if (request == null || request.CenterIndices == null || request.NewCenterPositions == null)
            return false;

        if (request.CenterIndices.Length == 0 || request.CenterIndices.Length != request.NewCenterPositions.Length)
            return false;

        StoreLastEditRequest(session, request);

        var committed = true;
        for (var i = 0; i < request.CenterIndices.Length; i++)
        {
            var position = request.NewCenterPositions[i];
            var unityPosition = new UnityEngine.Vector3(position.X, position.Y, position.Z);
            committed &= brush.Commit(unityPosition, request.CenterIndices[i], request.FrameIndex, frames);
        }

        if (committed)
            committed = brush.CommitAll(frames);

        SyncFromLegacyFrames(session, frames, settings, sourcePath, sequenceName, request.FrameIndex, true);
        return committed;
    }

    public bool CommitAllUnifiedEdits(EditingSession session, Frame[] frames)
    {
        if (session == null)
            return false;

        var rebuilt = RebuildSurface(session);
        if (rebuilt == null)
            return false;

        session.Sequence = rebuilt;
        session.IsDirty = true;
        SequenceAdapter.ApplyToLegacyFrames(rebuilt, frames);
        return true;
    }
}
