using System;
using System.Diagnostics;
using System.Reflection;
using CoreTvm;
using UnityEngine;

public sealed class LegacyPipelineHarness
{
    private readonly Brush _brush;

    public LegacyPipelineHarness(Brush brush)
    {
        _brush = brush ?? throw new ArgumentNullException(nameof(brush));
    }

    public LegacyPipelineResult RunSingleEdit(
        Frame[] sourceFrames,
        SequenceSettings settings,
        string sourcePath,
        string sequenceName,
        EditRequest request)
    {
        if (sourceFrames == null)
            throw new ArgumentNullException(nameof(sourceFrames));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.CenterIndices == null || request.NewCenterPositions == null || request.CenterIndices.Length != 1 || request.NewCenterPositions.Length != 1)
            throw new ArgumentException("Legacy offline harness currently supports exactly one edited center.");

        var workingFrames = FrameSnapshot.Clone(sourceFrames);
        var editedFrameIndex = request.FrameIndex;
        var editedCenterIndex = request.CenterIndices[0];
        var targetPosition = request.NewCenterPositions[0];
        var unityTarget = new UnityEngine.Vector3(targetPosition.X, targetPosition.Y, targetPosition.Z);

        var previousEditCount = _brush.edits;
        ResetLegacyTimers();
        var totalStopwatch = Stopwatch.StartNew();
        try
        {
            _brush.Commit(unityTarget, editedCenterIndex, editedFrameIndex, workingFrames);
            _brush.CommitAll(workingFrames);
        }
        finally
        {
            _brush.edits = previousEditCount;
        }
        totalStopwatch.Stop();

        return new LegacyPipelineResult
        {
            Sequence = SequenceAdapter.FromLegacyFrames(workingFrames, settings, sourcePath, sequenceName),
            AffectedFrames = BuildAllFrames(workingFrames.Length),
            Profiling = new LegacyProfilingSnapshot
            {
                TotalMilliseconds = (float)totalStopwatch.Elapsed.TotalMilliseconds,
                CenterMilliseconds = ReadTimerMilliseconds(_brush.centerDeformation),
                PropagationMilliseconds = ReadTimerMilliseconds(_brush.sequenceDeformation),
                SurfaceMilliseconds = ReadTimerMilliseconds(_brush.surfaceDeformation)
            }
        };
    }

    private static int[] BuildAllFrames(int count)
    {
        var frames = new int[count];
        for (var i = 0; i < count; i++)
        {
            frames[i] = i;
        }

        return frames;
    }

    private void ResetLegacyTimers()
    {
        _brush.centerDeformation?.ResetTimer();
        _brush.sequenceDeformation?.ResetTimer();
        _brush.surfaceDeformation?.ResetTimer();
    }

    private static float ReadTimerMilliseconds(object target)
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
}
