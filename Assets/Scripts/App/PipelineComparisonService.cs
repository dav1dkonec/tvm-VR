using System;
using System.Collections.Generic;
using System.Numerics;
using CoreTvm;

public sealed class PipelineComparisonService
{
    public PipelineComparisonReport Compare(
        SequenceData legacySequence,
        int[] legacyAffectedFrames,
        SequenceData unifiedSequence,
        int[] unifiedAffectedFrames,
        EditRequest request)
    {
        if (legacySequence == null)
            throw new ArgumentNullException(nameof(legacySequence));

        if (unifiedSequence == null)
            throw new ArgumentNullException(nameof(unifiedSequence));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var frameUnion = BuildFrameUnion(legacyAffectedFrames, unifiedAffectedFrames, request.FrameIndex);

        var centerDeltaSum = 0f;
        var centerDeltaMax = 0f;
        var centerDeltaCount = 0;

        var vertexDeltaSum = 0f;
        var vertexDeltaMax = 0f;
        var vertexDeltaCount = 0;

        foreach (var frameIndex in frameUnion)
        {
            if (frameIndex < 0 ||
                frameIndex >= legacySequence.Centers.Length ||
                frameIndex >= unifiedSequence.Centers.Length)
            {
                continue;
            }

            AccumulateCenterDeltas(
                legacySequence.Centers[frameIndex],
                unifiedSequence.Centers[frameIndex],
                ref centerDeltaSum,
                ref centerDeltaMax,
                ref centerDeltaCount);

            if (frameIndex < legacySequence.MeshFrames.Length &&
                frameIndex < unifiedSequence.MeshFrames.Length)
            {
                AccumulateVertexDeltas(
                    legacySequence.MeshFrames[frameIndex]?.Vertices,
                    unifiedSequence.MeshFrames[frameIndex]?.Vertices,
                    ref vertexDeltaSum,
                    ref vertexDeltaMax,
                    ref vertexDeltaCount);
            }
        }

        return new PipelineComparisonReport
        {
            SequenceName = legacySequence.Metadata?.SequenceName ?? string.Empty,
            FrameIndex = request.FrameIndex,
            EditedCenterIndices = (int[])request.CenterIndices.Clone(),
            LegacyAffectedFrameCount = (legacyAffectedFrames ?? Array.Empty<int>()).Length,
            UnifiedAffectedFrameCount = (unifiedAffectedFrames ?? Array.Empty<int>()).Length,
            ComparedFrameCount = frameUnion.Length,
            LegacyAffectedFrameMin = GetMinFrame(legacyAffectedFrames),
            LegacyAffectedFrameMax = GetMaxFrame(legacyAffectedFrames),
            UnifiedAffectedFrameMin = GetMinFrame(unifiedAffectedFrames),
            UnifiedAffectedFrameMax = GetMaxFrame(unifiedAffectedFrames),
            MeanCenterDelta = centerDeltaCount > 0 ? centerDeltaSum / centerDeltaCount : 0f,
            MaxCenterDelta = centerDeltaMax,
            MeanVertexDelta = vertexDeltaCount > 0 ? vertexDeltaSum / vertexDeltaCount : 0f,
            MaxVertexDelta = vertexDeltaMax,
            ComparedCenterCount = centerDeltaCount,
            ComparedVertexCount = vertexDeltaCount
        };
    }

    private static void AccumulateCenterDeltas(
        Vector3[] legacyCenters,
        Vector3[] unifiedCenters,
        ref float sum,
        ref float max,
        ref int count)
    {
        if (legacyCenters == null || unifiedCenters == null)
            return;

        var compared = Math.Min(legacyCenters.Length, unifiedCenters.Length);
        for (var i = 0; i < compared; i++)
        {
            var delta = Vector3.Distance(legacyCenters[i], unifiedCenters[i]);
            sum += delta;
            if (delta > max)
                max = delta;
            count++;
        }
    }

    private static void AccumulateVertexDeltas(
        Vector3[] legacyVertices,
        Vector3[] unifiedVertices,
        ref float sum,
        ref float max,
        ref int count)
    {
        if (legacyVertices == null || unifiedVertices == null)
            return;

        var compared = Math.Min(legacyVertices.Length, unifiedVertices.Length);
        for (var i = 0; i < compared; i++)
        {
            var delta = Vector3.Distance(legacyVertices[i], unifiedVertices[i]);
            sum += delta;
            if (delta > max)
                max = delta;
            count++;
        }
    }

    private static int[] BuildFrameUnion(int[] legacyAffectedFrames, int[] unifiedAffectedFrames, int requestedFrameIndex)
    {
        var frames = new HashSet<int>();

        frames.Add(requestedFrameIndex);

        if (legacyAffectedFrames != null)
        {
            for (var i = 0; i < legacyAffectedFrames.Length; i++)
            {
                frames.Add(legacyAffectedFrames[i]);
            }
        }

        if (unifiedAffectedFrames != null)
        {
            for (var i = 0; i < unifiedAffectedFrames.Length; i++)
            {
                frames.Add(unifiedAffectedFrames[i]);
            }
        }

        var result = new int[frames.Count];
        frames.CopyTo(result);
        Array.Sort(result);
        return result;
    }

    private static int GetMinFrame(int[] frames)
    {
        if (frames == null || frames.Length == 0)
            return -1;

        var min = int.MaxValue;
        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] < min)
                min = frames[i];
        }

        return min;
    }

    private static int GetMaxFrame(int[] frames)
    {
        if (frames == null || frames.Length == 0)
            return -1;

        var max = int.MinValue;
        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] > max)
                max = frames[i];
        }

        return max;
    }
}
