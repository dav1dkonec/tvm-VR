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

            var candidates = CollectCandidates(frame.centers, referencePoint, radius);
            if (candidates.Count == 0)
                return new InflateDeflateResolvedEffectors();

            ScoreCandidates(candidates, radius);
            var seedCandidates = CollectSeedCandidates(candidates, radius);
            if (seedCandidates.Count == 0)
                return new InflateDeflateResolvedEffectors();

            seedCandidates.Sort(static (a, b) =>
            {
                var scoreComparison = b.Score.CompareTo(a.Score);
                if (scoreComparison != 0)
                    return scoreComparison;

                return a.Distance.CompareTo(b.Distance);
            });

            var indices = new List<int>();
            var translations = new List<Vector3>();
            var seedCount = ResolveSeedCount(seedCandidates.Count);

            for (var i = 0; i < seedCandidates.Count && indices.Count < seedCount; i++)
            {
                var candidate = seedCandidates[i];
                if (!TryResolveSeedDirection(candidate, referencePoint, out var direction))
                    continue;

                if (mode == InflateDeflateMode.Deflate)
                    direction = -direction;

                var falloff = 1f - (candidate.Distance / radius);
                var translation = direction * (strength * falloff);
                if (translation.LengthSquared() < 1e-8f)
                    continue;

                indices.Add(candidate.Index);
                translations.Add(translation);
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

        private static List<CandidateCenter> CollectSeedCandidates(List<CandidateCenter> candidates, float radius)
        {
            var seedCandidates = new List<CandidateCenter>(candidates.Count);
            var centerExclusionRadius = System.MathF.Max(radius * 0.12f, 1e-4f);

            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance <= centerExclusionRadius)
                    continue;

                seedCandidates.Add(candidates[i]);
            }

            if (seedCandidates.Count > 0)
                return seedCandidates;

            // Fallback for very small radii: keep at least non-zero-distance candidates.
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance > 1e-5f)
                    seedCandidates.Add(candidates[i]);
            }

            return seedCandidates;
        }

        private static void ScoreCandidates(List<CandidateCenter> candidates, float radius)
        {
            var densityRadius = System.MathF.Max(radius * 0.45f, 1e-4f);

            for (var i = 0; i < candidates.Count; i++)
            {
                var density = 0f;
                for (var j = 0; j < candidates.Count; j++)
                {
                    if (i == j)
                        continue;

                    var neighborDistance = Vector3.Distance(candidates[i].Position, candidates[j].Position);
                    if (neighborDistance > densityRadius)
                        continue;

                    density += 1f - (neighborDistance / densityRadius);
                }

                var distancePriority = 1f - (candidates[i].Distance / radius);
                var candidate = candidates[i];
                candidate.Score = density * 2f + distancePriority;
                candidates[i] = candidate;
            }
        }

        private static int ResolveSeedCount(int candidateCount)
        {
            if (candidateCount <= 4)
                return System.Math.Min(2, candidateCount);

            if (candidateCount <= 12)
                return 3;

            return 4;
        }

        private static bool TryResolveSeedDirection(
            CandidateCenter candidate,
            Vector3 referencePoint,
            out Vector3 direction)
        {
            var toReference = referencePoint - candidate.Position;
            if (toReference.LengthSquared() >= 1e-8f)
            {
                direction = Vector3.Normalize(toReference);
                return true;
            }

            direction = Vector3.Zero;
            return false;
        }

        private struct CandidateCenter
        {
            public int Index;
            public Vector3 Position;
            public float Distance;
            public float Score;
        }
    }
}
