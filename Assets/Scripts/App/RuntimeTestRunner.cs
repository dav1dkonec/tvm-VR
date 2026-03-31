using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using CoreTvm;
using UnityEngine;

public sealed class RuntimeTestRunner
{
    public RuntimeTestResult RunSingle(
        RuntimeTestScenario scenario,
        SequenceEditingService editingService,
        Brush brush,
        EditingSession session,
        Frame[] frames,
        SequenceSettings settings,
        string loadedPath,
        string loadedName)
    {
        return RunSingleInternal(scenario, editingService, brush, session, frames, settings, loadedPath, loadedName);
    }

    public IReadOnlyList<RuntimeTestResult> RunSequential(
        RuntimeTestScenario scenario,
        SequenceEditingService editingService,
        Brush brush,
        EditingSession session,
        Frame[] frames,
        SequenceSettings settings,
        string loadedPath,
        string loadedName)
    {
        var results = new List<RuntimeTestResult>();
        var repeatCount = Math.Max(scenario?.RepeatCount ?? 1, 1);

        for (var i = 0; i < repeatCount; i++)
        {
            results.Add(RunSingleInternal(scenario, editingService, brush, session, frames, settings, loadedPath, loadedName));
        }

        return results;
    }

    private RuntimeTestResult RunSingleInternal(
        RuntimeTestScenario scenario,
        SequenceEditingService editingService,
        Brush brush,
        EditingSession session,
        Frame[] frames,
        SequenceSettings settings,
        string loadedPath,
        string loadedName)
    {
        var validationError = ValidateScenario(scenario, session, frames);
        if (validationError != null)
        {
            return new RuntimeTestResult
            {
                Success = false,
                ErrorMessage = validationError
            };
        }

        var beforeFrames = FrameSnapshot.Clone(frames);
        var request = BuildRequest(scenario, frames);
        var beforePositions = CapturePositions(beforeFrames[scenario.FrameIndex], scenario.CenterIndices);

        ResetLegacyTimers(brush);
        var commitStopwatch = Stopwatch.StartNew();
        var committed = scenario.UseUnifiedPipeline
            ? editingService.CommitUnifiedRequest(session, frames, request)
            : CommitLegacyScenario(editingService, brush, session, frames, settings, loadedPath, loadedName, request);
        commitStopwatch.Stop();

        if (!committed)
        {
            return new RuntimeTestResult
            {
                Success = false,
                ErrorMessage = "Commit service returned false."
            };
        }

        var afterPositions = CapturePositions(frames[scenario.FrameIndex], scenario.CenterIndices);
        var error = ComputeErrorMetrics(afterPositions, request.NewCenterPositions);
        var affectedFrames = ComputeAffectedFrames(beforeFrames, frames);
        var localCenterMetrics = ComputeLocalCenterMetrics(beforeFrames[scenario.FrameIndex], frames[scenario.FrameIndex], scenario.CenterIndices, 5);
        var localMeshMetrics = ComputeLocalMeshMetrics(beforeFrames[scenario.FrameIndex], frames[scenario.FrameIndex], scenario.CenterIndices);
        var unifiedProfile = scenario.UseUnifiedPipeline
            ? editingService.GetUnifiedProfilingSnapshotOrDefault()
            : new UnifiedProfilingSnapshot();

        return new RuntimeTestResult
        {
            Success = true,
            PipelineName = scenario.UseUnifiedPipeline ? "UNIFIED" : "LEGACY",
            SequenceName = loadedName ?? string.Empty,
            FrameIndex = scenario.FrameIndex,
            EditedCenterIndices = (int[])scenario.CenterIndices.Clone(),
            EditedCenterErrorMean = error.mean,
            EditedCenterErrorMax = error.max,
            AffectedFrames = affectedFrames,
            LocalCenterDisplacementMean = localCenterMetrics.mean,
            LocalCenterDisplacementMax = localCenterMetrics.max,
            LocalMeshDisplacementMean = localMeshMetrics.mean,
            LocalMeshDisplacementMax = localMeshMetrics.max,
            TotalRuntimeMilliseconds = (float)commitStopwatch.Elapsed.TotalMilliseconds,
            AffinityMilliseconds = unifiedProfile.AffinityMilliseconds,
            CenterMilliseconds = scenario.UseUnifiedPipeline ? unifiedProfile.CenterMilliseconds : ReadLegacyTimerMilliseconds(brush?.centerDeformation),
            PropagationMilliseconds = scenario.UseUnifiedPipeline ? unifiedProfile.PropagationMilliseconds : ReadLegacyTimerMilliseconds(brush?.sequenceDeformation),
            SurfaceMilliseconds = scenario.UseUnifiedPipeline ? unifiedProfile.SurfaceMilliseconds : ReadLegacyTimerMilliseconds(brush?.surfaceDeformation),
            BeforePositions = beforePositions,
            RequestedPositions = (System.Numerics.Vector3[])request.NewCenterPositions.Clone(),
            AfterPositions = afterPositions
        };
    }

    private static string ValidateScenario(RuntimeTestScenario scenario, EditingSession session, Frame[] frames)
    {
        if (scenario == null)
            return "Runtime test failed: scenario is null.";

        if (session?.Sequence == null)
            return "Runtime test failed: sequence is not loaded.";

        if (frames == null || frames.Length == 0)
            return "Runtime test failed: legacy frames are not available.";

        if (scenario.FrameIndex < 0 || scenario.FrameIndex >= frames.Length)
            return "Runtime test failed: current frame index is out of range.";

        if (scenario.CenterIndices == null || scenario.Translations == null || scenario.CenterIndices.Length == 0)
            return "Runtime test failed: center indices and translations must be configured.";

        if (scenario.CenterIndices.Length != scenario.Translations.Length)
            return "Runtime test failed: center indices and translations must have the same length.";

        var centerCount = frames[scenario.FrameIndex].centers.Length;
        for (var i = 0; i < scenario.CenterIndices.Length; i++)
        {
            if (scenario.CenterIndices[i] < 0 || scenario.CenterIndices[i] >= centerCount)
                return $"Runtime test failed: center index {scenario.CenterIndices[i]} is out of range.";
        }

        return null;
    }

    private static EditRequest BuildRequest(RuntimeTestScenario scenario, Frame[] frames)
    {
        var currentCenters = frames[scenario.FrameIndex].centers;
        var requestedPositions = new System.Numerics.Vector3[scenario.CenterIndices.Length];

        for (var i = 0; i < scenario.CenterIndices.Length; i++)
        {
            var centerIndex = scenario.CenterIndices[i];
            var currentCenter = currentCenters[centerIndex];
            var translation = scenario.Translations[i];
            requestedPositions[i] = new System.Numerics.Vector3(
                currentCenter.X + translation.x,
                currentCenter.Y + translation.y,
                currentCenter.Z + translation.z);
        }

        return new EditRequest
        {
            FrameIndex = scenario.FrameIndex,
            CenterIndices = (int[])scenario.CenterIndices.Clone(),
            NewCenterPositions = requestedPositions
        };
    }

    private static bool CommitLegacyScenario(
        SequenceEditingService editingService,
        Brush brush,
        EditingSession session,
        Frame[] frames,
        SequenceSettings settings,
        string loadedPath,
        string loadedName,
        EditRequest request)
    {
        return request.CenterIndices.Length == 1
            ? editingService.CommitLegacyRequest(session, brush, frames, settings, loadedPath, loadedName, request)
            : editingService.CommitLegacyBatchRequest(session, brush, frames, settings, loadedPath, loadedName, request);
    }

    private static System.Numerics.Vector3[] CapturePositions(Frame frame, int[] centerIndices)
    {
        var positions = new System.Numerics.Vector3[centerIndices.Length];
        for (var i = 0; i < centerIndices.Length; i++)
        {
            positions[i] = frame.centers[centerIndices[i]];
        }

        return positions;
    }

    private static (float mean, float max) ComputeErrorMetrics(System.Numerics.Vector3[] actual, System.Numerics.Vector3[] expected)
    {
        if (actual == null || expected == null || actual.Length == 0 || actual.Length != expected.Length)
            return (0f, 0f);

        var sum = 0f;
        var max = 0f;
        for (var i = 0; i < actual.Length; i++)
        {
            var error = System.Numerics.Vector3.Distance(actual[i], expected[i]);
            sum += error;
            if (error > max)
                max = error;
        }

        return (sum / actual.Length, max);
    }

    private static void ResetLegacyTimers(Brush brush)
    {
        brush?.centerDeformation?.ResetTimer();
        brush?.sequenceDeformation?.ResetTimer();
        brush?.surfaceDeformation?.ResetTimer();
    }

    private static float ReadLegacyTimerMilliseconds(object target)
    {
        if (target == null)
            return 0f;

        var field = target.GetType().GetField("timer", BindingFlags.Instance | BindingFlags.Public);
        if (field == null)
            return 0f;

        var value = field.GetValue(target);
        if (value is long longValue)
            return longValue;
        if (value is int intValue)
            return intValue;

        return 0f;
    }

    private static List<int> ComputeAffectedFrames(Frame[] beforeFrames, Frame[] afterFrames)
    {
        var affected = new List<int>();
        if (beforeFrames == null || afterFrames == null)
            return affected;

        var frameCount = Math.Min(beforeFrames.Length, afterFrames.Length);
        for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            if (FrameChanged(beforeFrames[frameIndex], afterFrames[frameIndex]))
                affected.Add(frameIndex);
        }

        return affected;
    }

    private static bool FrameChanged(Frame beforeFrame, Frame afterFrame)
    {
        if (beforeFrame == null || afterFrame == null)
            return beforeFrame != afterFrame;

        return AnyVectorChanged(beforeFrame.centers, afterFrame.centers) ||
               AnyVectorChanged(beforeFrame.vertices, afterFrame.vertices);
    }

    private static bool AnyVectorChanged(System.Numerics.Vector3[] before, System.Numerics.Vector3[] after)
    {
        if (before == null || after == null)
            return before != after;

        var count = Math.Min(before.Length, after.Length);
        for (var i = 0; i < count; i++)
        {
            if (System.Numerics.Vector3.DistanceSquared(before[i], after[i]) > 1e-12f)
                return true;
        }

        return before.Length != after.Length;
    }

    private static (float mean, float max) ComputeLocalCenterMetrics(Frame beforeFrame, Frame afterFrame, int[] editedCenterIndices, int neighborCount)
    {
        if (beforeFrame?.centers == null || afterFrame?.centers == null || beforeFrame.centers.Length == 0 || editedCenterIndices == null || editedCenterIndices.Length == 0)
            return (0f, 0f);

        var editedSet = new HashSet<int>(editedCenterIndices);
        var neighborCandidates = new List<(int index, float distance)>(beforeFrame.centers.Length);

        foreach (var editedCenterIndex in editedCenterIndices)
        {
            var source = beforeFrame.centers[editedCenterIndex];
            for (var i = 0; i < beforeFrame.centers.Length; i++)
            {
                if (editedSet.Contains(i))
                    continue;

                neighborCandidates.Add((i, System.Numerics.Vector3.Distance(source, beforeFrame.centers[i])));
            }
        }

        neighborCandidates.Sort((left, right) => left.distance.CompareTo(right.distance));
        var uniqueNeighbors = new List<int>(neighborCount);
        for (var i = 0; i < neighborCandidates.Count && uniqueNeighbors.Count < neighborCount; i++)
        {
            if (!uniqueNeighbors.Contains(neighborCandidates[i].index))
                uniqueNeighbors.Add(neighborCandidates[i].index);
        }

        if (uniqueNeighbors.Count == 0)
            return (0f, 0f);

        var sum = 0f;
        var max = 0f;
        for (var i = 0; i < uniqueNeighbors.Count; i++)
        {
            var neighborIndex = uniqueNeighbors[i];
            var displacement = System.Numerics.Vector3.Distance(beforeFrame.centers[neighborIndex], afterFrame.centers[neighborIndex]);
            sum += displacement;
            if (displacement > max)
                max = displacement;
        }

        return (sum / uniqueNeighbors.Count, max);
    }

    private static (float mean, float max) ComputeLocalMeshMetrics(Frame beforeFrame, Frame afterFrame, int[] editedCenterIndices)
    {
        if (beforeFrame?.vertices == null || afterFrame?.vertices == null || beforeFrame.nearestCentersIndex == null || editedCenterIndices == null || editedCenterIndices.Length == 0)
            return (0f, 0f);

        var editedSet = new HashSet<int>(editedCenterIndices);
        var sum = 0f;
        var max = 0f;
        var count = 0;

        for (var vertexIndex = 0; vertexIndex < beforeFrame.vertices.Length; vertexIndex++)
        {
            var nearestCenters = beforeFrame.nearestCentersIndex[vertexIndex];
            if (!ContainsAnyCenterIndex(nearestCenters, editedSet))
                continue;

            var displacement = System.Numerics.Vector3.Distance(beforeFrame.vertices[vertexIndex], afterFrame.vertices[vertexIndex]);
            sum += displacement;
            if (displacement > max)
                max = displacement;
            count++;
        }

        return count > 0 ? (sum / count, max) : (0f, 0f);
    }

    private static bool ContainsAnyCenterIndex(int[] indices, HashSet<int> targetIndices)
    {
        if (indices == null || targetIndices == null || targetIndices.Count == 0)
            return false;

        for (var i = 0; i < indices.Length; i++)
        {
            if (targetIndices.Contains(indices[i]))
                return true;
        }

        return false;
    }
}
