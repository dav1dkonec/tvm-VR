using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreTvm
{
    public sealed class UnifiedSurfaceDeformationCalculator : ISurfaceDeformationCalculator
    {
        public MeshFrameData[] Deform(MeshFrameData[] meshFrames, Vector3[][] oldCenters, Vector3[][] newCenters, int[] affectedFrames, float[,] affinity, EditingOptions options)
        {
            if (meshFrames == null)
                return Array.Empty<MeshFrameData>();

            var result = new MeshFrameData[meshFrames.Length];
            Array.Copy(meshFrames, result, meshFrames.Length);
            var affectedSet = new HashSet<int>(affectedFrames ?? Array.Empty<int>());

            foreach (var frameIndex in affectedSet)
            {
                if (frameIndex < 0 || frameIndex >= meshFrames.Length)
                    continue;

                result[frameIndex] = DeformFrame(
                    meshFrames[frameIndex],
                    oldCenters[frameIndex],
                    newCenters[frameIndex],
                    affinity,
                    options);
            }

            return result;
        }

        private MeshFrameData DeformFrame(MeshFrameData frame, Vector3[] oldCenters, Vector3[] newCenters, float[,] affinity, EditingOptions options)
        {
            var vertices = frame?.Vertices ?? Array.Empty<Vector3>();
            var deformedVertices = new Vector3[vertices.Length];
            var centerTranslations = ComputeTranslations(oldCenters, newCenters);
            var centerNeighborMap = BuildCenterNeighborMap(oldCenters, affinity, options);

            for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                var selection = SelectSurfaceNeighbors(vertices[vertexIndex], oldCenters, centerNeighborMap, options);
                var weights = ComputeWeights(selection.AnchorIndex, selection.Distances, selection.Indices, affinity, options);

                var shift = Vector3.Zero;
                for (var i = 0; i < selection.Count; i++)
                {
                    shift += centerTranslations[selection.Indices[i]] * weights[i];
                }

                deformedVertices[vertexIndex] = vertices[vertexIndex] + shift;
            }

            return new MeshFrameData
            {
                Vertices = deformedVertices,
                Faces = frame?.Faces ?? Array.Empty<FaceData>()
            };
        }

        private static NeighborSelection SelectSurfaceNeighbors(Vector3 vertex, Vector3[] centers, int[][] centerNeighborMap, EditingOptions options)
        {
            if (centers == null || centers.Length == 0)
                return NeighborSelection.Empty();

            var neighborCount = Math.Max(1, Math.Min(options?.SurfaceNeighbors ?? 6, centers.Length));
            var indices = new int[neighborCount];
            var distances = new float[neighborCount];
            for (var i = 0; i < neighborCount; i++)
            {
                indices[i] = -1;
                distances[i] = float.PositiveInfinity;
            }

            var anchorIndex = 0;
            var anchorDistance = float.PositiveInfinity;

            for (var centerIndex = 0; centerIndex < centers.Length; centerIndex++)
            {
                var distance = Vector3.Distance(vertex, centers[centerIndex]);
                if (distance < anchorDistance)
                {
                    anchorDistance = distance;
                    anchorIndex = centerIndex;
                }
            }

            var candidateIndices = anchorIndex < centerNeighborMap.Length
                ? centerNeighborMap[anchorIndex]
                : Array.Empty<int>();

            for (var i = 0; i < candidateIndices.Length; i++)
            {
                var centerIndex = candidateIndices[i];
                var distance = Vector3.Distance(vertex, centers[centerIndex]);
                InsertDistanceCandidate(indices, distances, centerIndex, distance);
            }

            var validCount = 0;
            while (validCount < indices.Length && indices[validCount] >= 0)
            {
                validCount++;
            }

            return new NeighborSelection(anchorIndex, indices, distances, validCount);
        }

        private static int[][] BuildCenterNeighborMap(Vector3[] centers, float[,] affinity, EditingOptions options)
        {
            if (centers == null || centers.Length == 0)
                return Array.Empty<int[]>();

            var neighborCount = Math.Max(1, Math.Min(Math.Max((options?.SurfaceNeighbors ?? 6) * 4, options?.SurfaceNeighbors ?? 6), centers.Length));
            var result = new int[centers.Length][];

            for (var centerIndex = 0; centerIndex < centers.Length; centerIndex++)
            {
                var indices = new int[neighborCount];
                var scores = new float[neighborCount];
                for (var i = 0; i < neighborCount; i++)
                {
                    indices[i] = -1;
                    scores[i] = float.NegativeInfinity;
                }

                for (var candidateIndex = 0; candidateIndex < centers.Length; candidateIndex++)
                {
                    var affinityScore = affinity != null && centerIndex < affinity.GetLength(0) && candidateIndex < affinity.GetLength(1)
                        ? affinity[centerIndex, candidateIndex]
                        : 0f;
                    var distance = Vector3.Distance(centers[centerIndex], centers[candidateIndex]);
                    var score = affinityScore - 0.001f * distance;
                    InsertScoreCandidate(indices, scores, candidateIndex, score);
                }

                result[centerIndex] = Compact(indices);
            }

            return result;
        }

        private static float[] ComputeWeights(int anchorIndex, float[] neighborDistances, int[] neighbors, float[,] affinity, EditingOptions options)
        {
            if (neighbors.Length == 0)
                return Array.Empty<float>();

            var epsilon = MathF.Max(options?.SurfaceEpsilon ?? 0.0001f, 0.0001f);
            var blend = Clamp01(options?.SurfaceBlendAlpha ?? 0.55f);
            var shape = MathF.Max(options?.SurfaceShape ?? 2f, epsilon);
            var minDistance = float.PositiveInfinity;
            var maxDistance = 0f;

            for (var i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] < 0)
                    continue;

                if (neighborDistances[i] < minDistance)
                    minDistance = neighborDistances[i];
                if (neighborDistances[i] > maxDistance)
                    maxDistance = neighborDistances[i];
            }

            minDistance = MathF.Max(minDistance, epsilon);
            maxDistance = MathF.Max(maxDistance, minDistance + epsilon);

            var rawWeights = new float[neighbors.Length];
            var weightSum = 0f;

            for (var i = 0; i < neighbors.Length; i++)
            {
                var neighborIndex = neighbors[i];
                if (neighborIndex < 0)
                {
                    rawWeights[i] = 0f;
                    continue;
                }

                var distance = neighborDistances[i];
                var vrWeight = MathF.Max(1f - distance / maxDistance, 0f);
                var customWeight = MathF.Exp(-distance / (shape * minDistance + epsilon));

                if (affinity != null && anchorIndex < affinity.GetLength(0) && neighborIndex < affinity.GetLength(1))
                    customWeight *= MathF.Max(affinity[anchorIndex, neighborIndex], epsilon);

                rawWeights[i] = blend * customWeight + (1f - blend) * vrWeight;
                weightSum += rawWeights[i];
            }

            if (weightSum < epsilon)
            {
                var fallbackWeight = 1f / neighbors.Length;
                for (var i = 0; i < rawWeights.Length; i++)
                {
                    rawWeights[i] = fallbackWeight;
                }

                return rawWeights;
            }

            for (var i = 0; i < rawWeights.Length; i++)
            {
                rawWeights[i] /= weightSum;
            }

            return rawWeights;
        }

        private static void InsertDistanceCandidate(int[] indices, float[] distances, int candidateIndex, float candidateDistance)
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

        private static void InsertScoreCandidate(int[] indices, float[] scores, int candidateIndex, float candidateScore)
        {
            for (var slot = 0; slot < indices.Length; slot++)
            {
                if (candidateScore <= scores[slot])
                    continue;

                for (var move = indices.Length - 1; move > slot; move--)
                {
                    indices[move] = indices[move - 1];
                    scores[move] = scores[move - 1];
                }

                indices[slot] = candidateIndex;
                scores[slot] = candidateScore;
                return;
            }
        }

        private static int[] Compact(int[] indices)
        {
            var validCount = 0;
            while (validCount < indices.Length && indices[validCount] >= 0)
            {
                validCount++;
            }

            if (validCount == indices.Length)
                return indices;

            var compact = new int[validCount];
            Array.Copy(indices, compact, validCount);
            return compact;
        }

        private static Vector3[] ComputeTranslations(Vector3[] oldCenters, Vector3[] newCenters)
        {
            var count = Math.Min(oldCenters.Length, newCenters.Length);
            var translations = new Vector3[count];

            for (var i = 0; i < count; i++)
            {
                translations[i] = newCenters[i] - oldCenters[i];
            }

            return translations;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;

            if (value > 1f)
                return 1f;

            return value;
        }

        private readonly struct NeighborSelection
        {
            public int AnchorIndex { get; }
            public int[] Indices { get; }
            public float[] Distances { get; }
            public int Count { get; }

            public NeighborSelection(int anchorIndex, int[] indices, float[] distances, int count)
            {
                AnchorIndex = anchorIndex;
                Indices = indices;
                Distances = distances;
                Count = count;
            }

            public static NeighborSelection Empty()
            {
                return new NeighborSelection(0, Array.Empty<int>(), Array.Empty<float>(), 0);
            }
        }
    }
}
