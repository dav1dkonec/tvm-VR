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

            if (!TryEstimateSurfaceNormal(frame.centers, candidates, referencePoint, out var surfaceNormal))
                return new InflateDeflateResolvedEffectors();

            ScoreCandidates(candidates, radius);
            candidates.Sort(static (a, b) =>
            {
                var distanceComparison = a.Distance.CompareTo(b.Distance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return b.Score.CompareTo(a.Score);
            });

            var indices = new List<int>();
            var translations = new List<Vector3>();
            var selectedIndices = new HashSet<int>();
            var activeSeedCount = ResolveActiveSeedCount(candidates.Count);
            var anchorSeedCount = ResolveAnchorSeedCount(candidates.Count, activeSeedCount);
            var baseDirection = mode == InflateDeflateMode.Inflate ? surfaceNormal : -surfaceNormal;
            var activePatchRadius = System.MathF.Max(radius * 0.4f, 1e-4f);
            var anchorPatchRadius = System.MathF.Max(radius * 0.85f, activePatchRadius);

            for (var i = 0; i < candidates.Count && indices.Count < activeSeedCount; i++)
            {
                var candidate = candidates[i];
                if (candidate.Distance > activePatchRadius)
                    continue;

                var translationScale = ComputeDomeFalloff(candidate.Distance, activePatchRadius);
                if (translationScale <= 0f)
                    continue;

                var translation = baseDirection * (strength * translationScale);
                if (translation.LengthSquared() < 1e-8f)
                    continue;

                indices.Add(candidate.Index);
                translations.Add(translation);
                selectedIndices.Add(candidate.Index);
            }

            candidates.Sort(static (a, b) => a.Distance.CompareTo(b.Distance));
            for (var i = 0; i < candidates.Count && selectedIndices.Count < activeSeedCount + anchorSeedCount; i++)
            {
                var candidate = candidates[i];
                if (selectedIndices.Contains(candidate.Index))
                    continue;

                if (candidate.Distance <= activePatchRadius || candidate.Distance > anchorPatchRadius)
                    continue;

                indices.Add(candidate.Index);
                translations.Add(Vector3.Zero);
                selectedIndices.Add(candidate.Index);
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

        private static void ScoreCandidates(List<CandidateCenter> candidates, float radius)
        {
            var densityRadius = System.MathF.Max(radius * 0.25f, 1e-4f);

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

                var distancePriority = 1f - (candidates[i].Distance / System.MathF.Max(radius, 1e-4f));
                var candidate = candidates[i];
                candidate.Score = distancePriority * 8f + density;
                candidates[i] = candidate;
            }
        }

        private static float ComputeDomeFalloff(float distance, float activePatchRadius)
        {
            if (activePatchRadius <= 1e-8f)
                return 0f;

            var normalizedDistance = System.MathF.Min(distance / activePatchRadius, 1f);
            var inverse = 1f - normalizedDistance;
            return inverse * inverse;
        }

        private static int ResolveActiveSeedCount(int candidateCount)
        {
            if (candidateCount <= 6)
                return System.Math.Min(4, candidateCount);

            if (candidateCount <= 14)
                return 6;

            if (candidateCount <= 24)
                return 10;

            return System.Math.Min(16, candidateCount);
        }

        private static int ResolveAnchorSeedCount(int candidateCount, int activeSeedCount)
        {
            if (candidateCount <= activeSeedCount)
                return 0;

            var desiredAnchors = System.Math.Max(activeSeedCount, candidateCount / 2);
            return System.Math.Min(desiredAnchors, candidateCount - activeSeedCount);
        }

        private static bool TryEstimateSurfaceNormal(
            Vector3[] allCenters,
            List<CandidateCenter> candidates,
            Vector3 referencePoint,
            out Vector3 normal)
        {
            if (candidates.Count == 0)
            {
                normal = Vector3.Zero;
                return false;
            }

            var candidateCentroid = Vector3.Zero;
            for (var i = 0; i < candidates.Count; i++)
                candidateCentroid += candidates[i].Position;

            candidateCentroid /= candidates.Count;

            if (candidates.Count < 3)
            {
                var fallback = referencePoint - ComputeCentroid(allCenters);
                if (fallback.LengthSquared() < 1e-8f)
                {
                    normal = Vector3.Zero;
                    return false;
                }

                normal = Vector3.Normalize(fallback);
                return true;
            }

            var xx = 0f;
            var xy = 0f;
            var xz = 0f;
            var yy = 0f;
            var yz = 0f;
            var zz = 0f;

            for (var i = 0; i < candidates.Count; i++)
            {
                var delta = candidates[i].Position - candidateCentroid;
                xx += delta.X * delta.X;
                xy += delta.X * delta.Y;
                xz += delta.X * delta.Z;
                yy += delta.Y * delta.Y;
                yz += delta.Y * delta.Z;
                zz += delta.Z * delta.Z;
            }

            var matrix = new float[,]
            {
                { xx, xy, xz },
                { xy, yy, yz },
                { xz, yz, zz }
            };

            var eigenVectors = new float[,]
            {
                { 1f, 0f, 0f },
                { 0f, 1f, 0f },
                { 0f, 0f, 1f }
            };

            JacobiEigenDecomposition(matrix, eigenVectors);

            var smallestIndex = 0;
            if (matrix[1, 1] < matrix[smallestIndex, smallestIndex])
                smallestIndex = 1;
            if (matrix[2, 2] < matrix[smallestIndex, smallestIndex])
                smallestIndex = 2;

            normal = new Vector3(
                eigenVectors[0, smallestIndex],
                eigenVectors[1, smallestIndex],
                eigenVectors[2, smallestIndex]);

            if (normal.LengthSquared() < 1e-8f)
                return false;

            normal = Vector3.Normalize(normal);

            var globalOutwardHint = referencePoint - ComputeCentroid(allCenters);
            if (globalOutwardHint.LengthSquared() >= 1e-8f && Vector3.Dot(normal, globalOutwardHint) < 0f)
                normal = -normal;

            return true;
        }

        private static Vector3 ComputeCentroid(Vector3[] points)
        {
            if (points == null || points.Length == 0)
                return Vector3.Zero;

            var centroid = Vector3.Zero;
            for (var i = 0; i < points.Length; i++)
                centroid += points[i];

            return centroid / points.Length;
        }

        private static void JacobiEigenDecomposition(float[,] matrix, float[,] eigenVectors)
        {
            const int maxIterations = 12;
            const float epsilon = 1e-6f;

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                var p = 0;
                var q = 1;
                var maxOffDiagonal = System.MathF.Abs(matrix[0, 1]);

                if (System.MathF.Abs(matrix[0, 2]) > maxOffDiagonal)
                {
                    p = 0;
                    q = 2;
                    maxOffDiagonal = System.MathF.Abs(matrix[0, 2]);
                }

                if (System.MathF.Abs(matrix[1, 2]) > maxOffDiagonal)
                {
                    p = 1;
                    q = 2;
                    maxOffDiagonal = System.MathF.Abs(matrix[1, 2]);
                }

                if (maxOffDiagonal < epsilon)
                    break;

                var app = matrix[p, p];
                var aqq = matrix[q, q];
                var apq = matrix[p, q];
                var tau = (aqq - app) / (2f * apq);
                var t = tau >= 0f
                    ? 1f / (tau + System.MathF.Sqrt(1f + tau * tau))
                    : -1f / (-tau + System.MathF.Sqrt(1f + tau * tau));
                var c = 1f / System.MathF.Sqrt(1f + t * t);
                var s = t * c;

                matrix[p, p] = app - t * apq;
                matrix[q, q] = aqq + t * apq;
                matrix[p, q] = 0f;
                matrix[q, p] = 0f;

                for (var r = 0; r < 3; r++)
                {
                    if (r == p || r == q)
                        continue;

                    var arp = matrix[r, p];
                    var arq = matrix[r, q];
                    matrix[r, p] = c * arp - s * arq;
                    matrix[p, r] = matrix[r, p];
                    matrix[r, q] = c * arq + s * arp;
                    matrix[q, r] = matrix[r, q];
                }

                for (var r = 0; r < 3; r++)
                {
                    var vrp = eigenVectors[r, p];
                    var vrq = eigenVectors[r, q];
                    eigenVectors[r, p] = c * vrp - s * vrq;
                    eigenVectors[r, q] = c * vrq + s * vrp;
                }
            }
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
