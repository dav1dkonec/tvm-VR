using System.Collections.Generic;
using System.Numerics;
using TvmVr2.Api.Enums;
using TvmVr2.Api.Requests;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateInputMapper
    {
        public InflateDeflateMethodInput Map(InflateDeflateRequest request, SequenceRuntimeContext runtimeContext)
        {
            var referencePoint = new Vector3(
                request.ReferencePoint.X,
                request.ReferencePoint.Y,
                request.ReferencePoint.Z);
            var resolvedEffectors = ResolveEffectors(
                runtimeContext?.Frames,
                request.FrameIndex,
                referencePoint,
                request.Radius,
                request.Strength,
                request.Mode);

            return new InflateDeflateMethodInput
            {
                SequenceId = runtimeContext?.SequenceId ?? string.Empty,
                Frames = runtimeContext?.Frames,
                FrameIndex = request.FrameIndex,
                ReferencePoint = referencePoint,
                Radius = request.Radius,
                Strength = request.Strength,
                Mode = request.Mode,
                SelectedCenterIndices = resolvedEffectors.Indices,
                CenterTranslations = resolvedEffectors.Translations
            };
        }

        private static InflateDeflateResolvedEffectors ResolveEffectors(
            Frame[] frames,
            int frameIndex,
            Vector3 referencePoint,
            float radius,
            float strength,
            InflateDeflateMode mode)
        {
            if (frames == null || frameIndex < 0 || frameIndex >= frames.Length)
                return new InflateDeflateResolvedEffectors();

            var frame = frames[frameIndex];
            if (frame?.centers == null || frame.centers.Length == 0 || radius <= 0f || strength <= 0f)
                return new InflateDeflateResolvedEffectors();

            var selectedCenterIndex = FindNearestCenterIndex(frame.centers, referencePoint);
            if (selectedCenterIndex < 0)
                return new InflateDeflateResolvedEffectors();

            var selectedCenterPosition = frame.centers[selectedCenterIndex];
            var candidates = CollectCandidates(frame.centers, selectedCenterPosition, radius);
            if (candidates.Count == 0)
                return new InflateDeflateResolvedEffectors();

            candidates.Sort(static (a, b) =>
            {
                var distanceComparison = a.Distance.CompareTo(b.Distance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return a.Index.CompareTo(b.Index);
            });

            var indices = new List<int>();
            var translations = new List<Vector3>();
            var activePatchRadius = System.MathF.Max(radius * 0.72f, 1e-4f);
            var outerRingRadius = System.MathF.Max(radius, activePatchRadius);
            var directionSign = mode == InflateDeflateMode.Inflate ? 1f : -1f;

            indices.Add(selectedCenterIndex);
            translations.Add(Vector3.Zero);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Index == selectedCenterIndex)
                    continue;

                if (candidate.Distance > outerRingRadius)
                    continue;

                indices.Add(candidate.Index);

                var offset = candidate.Position - selectedCenterPosition;
                if (offset.LengthSquared() < 1e-8f)
                {
                    translations.Add(Vector3.Zero);
                    continue;
                }

                var influence = mode == InflateDeflateMode.Inflate
                    ? ComputeInflateInfluence(candidate.Distance, activePatchRadius, outerRingRadius)
                    : ComputeDeflateInfluence(candidate.Distance, activePatchRadius, outerRingRadius);
                translations.Add(offset * (directionSign * strength * influence));
            }

            return new InflateDeflateResolvedEffectors
            {
                Indices = indices.ToArray(),
                Translations = translations.ToArray()
            };
        }

        private static List<CandidateCenter> CollectCandidates(Vector3[] centers, Vector3 referencePoint, float radius)
        {
            var candidates = new List<CandidateCenter>();
            for (var i = 0; i < centers.Length; i++)
            {
                var distance = Vector3.Distance(centers[i], referencePoint);
                if (distance > radius)
                    continue;

                candidates.Add(new CandidateCenter
                {
                    Index = i,
                    Position = centers[i],
                    Distance = distance
                });
            }

            return candidates;
        }

        private static int FindNearestCenterIndex(Vector3[] centers, Vector3 referencePoint)
        {
            if (centers == null || centers.Length == 0)
                return -1;

            var bestIndex = 0;
            var bestDistanceSquared = Vector3.DistanceSquared(centers[0], referencePoint);

            for (var i = 1; i < centers.Length; i++)
            {
                var distanceSquared = Vector3.DistanceSquared(centers[i], referencePoint);
                if (distanceSquared >= bestDistanceSquared)
                    continue;

                bestDistanceSquared = distanceSquared;
                bestIndex = i;
            }

            return bestIndex;
        }

        private static float ComputeInflateInfluence(float distance, float activePatchRadius, float outerRingRadius)
        {
            const float inflateOuterRingStartInfluence = 0.12f;
            const float inflateOuterRingEndInfluence = 0.02f;

            if (activePatchRadius <= 1e-8f || outerRingRadius <= 1e-8f)
                return 0f;

            if (distance <= activePatchRadius)
            {
                var normalizedDistance = System.MathF.Min(distance / activePatchRadius, 1f);
                // Keep strong influence inside the active patch, but taper slightly toward its edge.
                return 1f - 0.35f * normalizedDistance * normalizedDistance;
            }

            var outerSpan = outerRingRadius - activePatchRadius;
            if (outerSpan <= 1e-8f)
                return inflateOuterRingStartInfluence;

            var ringDistance = System.MathF.Min((distance - activePatchRadius) / outerSpan, 1f);
            // Outer ring still participates a little to keep the transition smooth.
            return inflateOuterRingStartInfluence * (1f - ringDistance) + inflateOuterRingEndInfluence * ringDistance;
        }

        private static float ComputeDeflateInfluence(float distance, float activePatchRadius, float outerRingRadius)
        {
            const float deflateOuterRingStartInfluence = 0.18f;
            const float deflateOuterRingEndInfluence = 0.06f;

            if (activePatchRadius <= 1e-8f || outerRingRadius <= 1e-8f)
                return 0f;

            if (distance <= activePatchRadius)
            {
                var normalizedDistance = System.MathF.Min(distance / activePatchRadius, 1f);
                // Keep the center softer and let middle shells participate more,
                // so the local region shrinks compactly instead of collapsing inward.
                return 0.45f + 0.35f * normalizedDistance;
            }

            var outerSpan = outerRingRadius - activePatchRadius;
            if (outerSpan <= 1e-8f)
                return deflateOuterRingStartInfluence;

            var ringDistance = System.MathF.Min((distance - activePatchRadius) / outerSpan, 1f);
            // Broader and stronger transition ring for deflate to avoid a gap behind the border.
            return deflateOuterRingStartInfluence * (1f - ringDistance) + deflateOuterRingEndInfluence * ringDistance;
        }

        private struct CandidateCenter
        {
            public int Index;
            public Vector3 Position;
            public float Distance;
        }
    }
}
