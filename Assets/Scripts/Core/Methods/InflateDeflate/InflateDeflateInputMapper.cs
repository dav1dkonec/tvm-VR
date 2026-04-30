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

            EnsureMinimumCandidateCount(frame.centers, selectedCenterIndex, candidates);

            var indices = new List<int>();
            var translations = new List<Vector3>();
            var activePatchRadius = System.MathF.Max(radius * 0.72f, 1e-4f);
            var outerRingRadius = System.MathF.Max(radius, activePatchRadius);
            var directionSign = mode == InflateDeflateMode.Inflate ? 1f : -1f;
            var expansionCenter = ComputeExpansionCenter(candidates, activePatchRadius, selectedCenterPosition);
            var sparseRegionBoost = ComputeSparseRegionBoost(candidates.Count);

            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (candidate.Distance > outerRingRadius)
                    continue;

                indices.Add(candidate.Index);

                var offset = candidate.Position - expansionCenter;
                if (offset.LengthSquared() < 1e-8f)
                {
                    translations.Add(Vector3.Zero);
                    continue;
                }

                var influence = ComputeInfluence(candidate.Distance, activePatchRadius, outerRingRadius);
                translations.Add(offset * (directionSign * strength * influence * sparseRegionBoost));
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

        private static void EnsureMinimumCandidateCount(
            Vector3[] centers,
            int selectedCenterIndex,
            List<CandidateCenter> candidates)
        {
            const int minimumCandidateCount = 8;
            if (centers == null || centers.Length == 0 || candidates.Count >= minimumCandidateCount)
                return;

            var present = new HashSet<int>();
            for (var i = 0; i < candidates.Count; i++)
                present.Add(candidates[i].Index);

            var ordered = new List<CandidateCenter>(centers.Length);
            for (var i = 0; i < centers.Length; i++)
            {
                if (present.Contains(i))
                    continue;

                var distance = Vector3.Distance(centers[i], centers[selectedCenterIndex]);
                ordered.Add(new CandidateCenter
                {
                    Index = i,
                    Position = centers[i],
                    Distance = distance
                });
            }

            ordered.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            for (var i = 0; i < ordered.Count && candidates.Count < minimumCandidateCount; i++)
                candidates.Add(ordered[i]);
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

        private static float ComputeInfluence(float distance, float activePatchRadius, float outerRingRadius)
        {
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
                return 0.1f;

            var ringDistance = System.MathF.Min((distance - activePatchRadius) / outerSpan, 1f);
            // Outer ring still participates a little to keep the transition smooth.
            return 0.12f * (1f - ringDistance) + 0.02f * ringDistance;
        }

        private static Vector3 ComputeExpansionCenter(
            List<CandidateCenter> candidates,
            float activePatchRadius,
            Vector3 fallbackCenter)
        {
            if (candidates == null || candidates.Count == 0)
                return fallbackCenter;

            var centroid = Vector3.Zero;
            var weightSum = 0f;

            for (var i = 0; i < candidates.Count; i++)
            {
                var weight = candidates[i].Distance <= activePatchRadius ? 1f : 0.25f;
                centroid += candidates[i].Position * weight;
                weightSum += weight;
            }

            if (weightSum <= 1e-8f)
                return fallbackCenter;

            return centroid / weightSum;
        }

        private static float ComputeSparseRegionBoost(int candidateCount)
        {
            if (candidateCount >= 8)
                return 1f;

            // Sparse parts like wrists/arms need slightly stronger scaling to create visible volume.
            return 1f + (8 - candidateCount) * 0.12f;
        }

        private struct CandidateCenter
        {
            public int Index;
            public Vector3 Position;
            public float Distance;
        }
    }
}
