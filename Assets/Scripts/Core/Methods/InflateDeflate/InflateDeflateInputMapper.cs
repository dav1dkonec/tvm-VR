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
            EstimateExpansionAxis(candidates, expansionCenter, out var expansionAxis, out var anisotropy);

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
                var shapedOffset = ShapeOffset(offset, expansionAxis, anisotropy);
                translations.Add(shapedOffset * (directionSign * strength * influence * sparseRegionBoost));
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

        private static Vector3 ShapeOffset(Vector3 offset, Vector3 expansionAxis, float anisotropy)
        {
            if (offset.LengthSquared() < 1e-8f)
                return Vector3.Zero;

            if (expansionAxis.LengthSquared() < 1e-8f || anisotropy <= 0f)
                return offset;

            var axial = expansionAxis * Vector3.Dot(offset, expansionAxis);
            var radial = offset - axial;

            if (radial.LengthSquared() < 1e-8f)
            {
                // Preserve a little axial motion so line-like regions do not collapse to zero response.
                return axial * (0.15f + 0.15f * anisotropy);
            }

            // Blend between isotropic expansion and radial thickening.
            var isotropicWeight = 1f - anisotropy;
            var radialWeight = 1f + anisotropy * 0.35f;
            var axialWeight = 1f - anisotropy * 0.85f;

            return offset * isotropicWeight + radial * radialWeight + axial * axialWeight;
        }

        private static void EstimateExpansionAxis(
            List<CandidateCenter> candidates,
            Vector3 expansionCenter,
            out Vector3 axis,
            out float anisotropy)
        {
            axis = Vector3.Zero;
            anisotropy = 0f;

            if (candidates == null || candidates.Count < 3)
                return;

            var xx = 0f;
            var xy = 0f;
            var xz = 0f;
            var yy = 0f;
            var yz = 0f;
            var zz = 0f;

            for (var i = 0; i < candidates.Count; i++)
            {
                var delta = candidates[i].Position - expansionCenter;
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

            var largestIndex = 0;
            if (matrix[1, 1] > matrix[largestIndex, largestIndex])
                largestIndex = 1;
            if (matrix[2, 2] > matrix[largestIndex, largestIndex])
                largestIndex = 2;

            var smallestIndex = 0;
            if (matrix[1, 1] < matrix[smallestIndex, smallestIndex])
                smallestIndex = 1;
            if (matrix[2, 2] < matrix[smallestIndex, smallestIndex])
                smallestIndex = 2;

            axis = new Vector3(
                eigenVectors[0, largestIndex],
                eigenVectors[1, largestIndex],
                eigenVectors[2, largestIndex]);

            if (axis.LengthSquared() < 1e-8f)
            {
                axis = Vector3.Zero;
                anisotropy = 0f;
                return;
            }

            axis = Vector3.Normalize(axis);

            var largestEigenvalue = System.MathF.Max(matrix[largestIndex, largestIndex], 1e-8f);
            var smallestEigenvalue = System.MathF.Max(matrix[smallestIndex, smallestIndex], 0f);
            anisotropy = 1f - (smallestEigenvalue / largestEigenvalue);
            anisotropy = System.Math.Clamp(anisotropy, 0f, 1f);
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
        }
    }
}
