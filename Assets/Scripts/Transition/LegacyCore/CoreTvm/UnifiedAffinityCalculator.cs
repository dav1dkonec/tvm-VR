using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreTvm
{
    public sealed class UnifiedAffinityCalculator : IAffinityCalculator
    {
        private const float DistanceShape = 6f;
        private const float DirectionShape = 1f;
        private const float SmallVectorThreshold = 1e-12f;

        public float[,] Calculate(Vector3[][] centers, EditingOptions options)
        {
            if (centers == null || centers.Length == 0 || centers[0] == null)
                return new float[0, 0];

            var centerCount = centers[0].Length;
            var affinity = new float[centerCount, centerCount];

            if (centerCount == 0)
                return affinity;

            var alpha = Clamp01(options?.AffinityBlendAlpha ?? 0.5f);
            var sigma = MathF.Max(options?.GaussianSigma ?? 1f, 0.0001f);
            var neighborMap = BuildNeighborMap(centers[0], options?.AffinityNeighbors ?? 24);
            var maxDistances = new float[centerCount, centerCount];
            var averageDistances = alpha < 1f ? new float[centerCount, centerCount] : null;
            var maxDirectionDifferences = alpha > 0f ? new float[centerCount, centerCount] : null;

            ComputePairStatistics(centers, neighborMap, maxDistances, averageDistances, maxDirectionDifferences);

            for (var i = 0; i < centerCount; i++)
            {
                affinity[i, i] = 1f;

                var neighbors = neighborMap[i];
                for (var neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++)
                {
                    var j = neighbors[neighborIndex];
                    if (j <= i)
                        continue;

                    var dotnetAffinity = 0f;
                    if (alpha > 0f)
                    {
                        dotnetAffinity =
                            Rbf(DistanceShape, maxDistances[i, j]) *
                            Rbf(DirectionShape, maxDirectionDifferences[i, j]);
                    }

                    var vrGaussianAffinity = 0f;
                    if (alpha < 1f)
                    {
                        vrGaussianAffinity = GaussianFalloff(averageDistances[i, j] / centers.Length, sigma);
                    }

                    var blended = Clamp01(alpha * dotnetAffinity + (1f - alpha) * vrGaussianAffinity);

                    affinity[i, j] = blended;
                    affinity[j, i] = blended;
                }
            }

            return affinity;
        }

        private static void ComputePairStatistics(
            Vector3[][] centers,
            int[][] neighborMap,
            float[,] maxDistances,
            float[,] averageDistances,
            float[,] maxDirectionDifferences)
        {
            var frameCount = centers.Length;
            var centerCount = centers[0].Length;
            Vector3[] normalizedMotion = null;
            bool[] hasMotion = null;

            if (maxDirectionDifferences != null && frameCount > 1)
            {
                normalizedMotion = new Vector3[centerCount];
                hasMotion = new bool[centerCount];
            }

            for (var frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                if (normalizedMotion != null && frameIndex < frameCount - 1)
                {
                    PrepareNormalizedMotion(centers[frameIndex], centers[frameIndex + 1], normalizedMotion, hasMotion);
                }

                for (var i = 0; i < centerCount; i++)
                {
                    var neighbors = neighborMap[i];
                    for (var neighborIndex = 0; neighborIndex < neighbors.Length; neighborIndex++)
                    {
                        var j = neighbors[neighborIndex];
                        if (j <= i)
                            continue;

                        var distance = Vector3.Distance(centers[frameIndex][i], centers[frameIndex][j]);
                        if (distance > maxDistances[i, j])
                        {
                            maxDistances[i, j] = distance;
                        }

                        if (averageDistances != null)
                        {
                            averageDistances[i, j] += distance;
                        }

                        if (maxDirectionDifferences != null && hasMotion[i] && hasMotion[j])
                        {
                            var directionDifference = MathF.Max(-Vector3.Dot(normalizedMotion[i], normalizedMotion[j]), 0f);
                            if (directionDifference > maxDirectionDifferences[i, j])
                            {
                                maxDirectionDifferences[i, j] = directionDifference;
                            }
                        }
                    }
                }
            }
        }

        private static int[][] BuildNeighborMap(Vector3[] referenceCenters, int requestedNeighbors)
        {
            if (referenceCenters == null || referenceCenters.Length == 0)
                return Array.Empty<int[]>();

            var centerCount = referenceCenters.Length;
            var neighborCount = Math.Max(1, Math.Min(requestedNeighbors, Math.Max(centerCount - 1, 1)));
            var neighbors = new HashSet<int>[centerCount];
            for (var i = 0; i < centerCount; i++)
            {
                neighbors[i] = new HashSet<int> { i };
            }

            for (var i = 0; i < centerCount; i++)
            {
                var indices = new int[neighborCount];
                var distances = new float[neighborCount];
                for (var slot = 0; slot < neighborCount; slot++)
                {
                    indices[slot] = -1;
                    distances[slot] = float.PositiveInfinity;
                }

                for (var j = 0; j < centerCount; j++)
                {
                    if (i == j)
                        continue;

                    var distance = Vector3.Distance(referenceCenters[i], referenceCenters[j]);
                    InsertNeighbor(indices, distances, j, distance);
                }

                for (var slot = 0; slot < neighborCount; slot++)
                {
                    var neighborIndex = indices[slot];
                    if (neighborIndex < 0)
                        continue;

                    neighbors[i].Add(neighborIndex);
                    neighbors[neighborIndex].Add(i);
                }
            }

            var result = new int[centerCount][];
            for (var i = 0; i < centerCount; i++)
            {
                result[i] = new int[neighbors[i].Count];
                neighbors[i].CopyTo(result[i]);
                Array.Sort(result[i]);
            }

            return result;
        }

        private static void InsertNeighbor(int[] indices, float[] distances, int candidateIndex, float candidateDistance)
        {
            for (var slot = 0; slot < indices.Length; slot++)
            {
                if (candidateDistance >= distances[slot])
                    continue;

                for (var move = indices.Length - 1; move > slot; move--)
                {
                    indices[move] = indices[move - 1];
                    distances[move] = distances[move - 1];
                }

                indices[slot] = candidateIndex;
                distances[slot] = candidateDistance;
                return;
            }
        }

        private static void PrepareNormalizedMotion(Vector3[] currentCenters, Vector3[] nextCenters, Vector3[] normalizedMotion, bool[] hasMotion)
        {
            for (var centerIndex = 0; centerIndex < currentCenters.Length; centerIndex++)
            {
                var motion = nextCenters[centerIndex] - currentCenters[centerIndex];
                if (motion.LengthSquared() < SmallVectorThreshold)
                {
                    hasMotion[centerIndex] = false;
                    normalizedMotion[centerIndex] = Vector3.Zero;
                    continue;
                }

                hasMotion[centerIndex] = true;
                normalizedMotion[centerIndex] = Vector3.Normalize(motion);
            }
        }

        private static float Rbf(float shape, float distance)
        {
            return MathF.Exp(-(shape * shape) * distance * distance);
        }

        private static float GaussianFalloff(float distance, float sigma)
        {
            return MathF.Exp(-sigma * distance);
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }
    }
}
