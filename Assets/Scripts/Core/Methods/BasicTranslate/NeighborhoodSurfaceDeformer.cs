using System.Numerics;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class NeighborhoodSurfaceDeformer
    {
        public int Neighbors { get; }
        public float Epsilon { get; }

        public NeighborhoodSurfaceDeformer(int neighbors = 6, float epsilon = 0.0001f)
        {
            Neighbors = neighbors;
            Epsilon = epsilon;
        }

        public Vector3[] DeformSurface(Frame frame)
        {
            if (frame?.vertices == null || frame.verticesUnedited == null || frame.nearestCentersIndex == null || frame.nearestCentersDist == null)
                return frame?.vertices;

            var deformedVertices = new Vector3[frame.vertices.Length];

            for (var i = 0; i < frame.vertices.Length; i++)
            {
                if (i >= frame.nearestCentersIndex.Length || i >= frame.nearestCentersDist.Length)
                {
                    deformedVertices[i] = frame.verticesUnedited[i];
                    continue;
                }

                var indices = frame.nearestCentersIndex[i];
                var distances = frame.nearestCentersDist[i];

                if (indices == null || distances == null || indices.Length == 0 || distances.Length == 0)
                {
                    deformedVertices[i] = frame.verticesUnedited[i];
                    continue;
                }

                var availableNeighbors = System.Math.Min(Neighbors, System.Math.Min(indices.Length, distances.Length));
                if (availableNeighbors <= 1)
                {
                    var nearestIndex = indices[0];
                    if (nearestIndex < 0 || nearestIndex >= frame.centers.Length || nearestIndex >= frame.centersUnedited.Length)
                    {
                        deformedVertices[i] = frame.verticesUnedited[i];
                        continue;
                    }

                    var shiftSingle = frame.centers[nearestIndex] - frame.centersUnedited[nearestIndex];
                    deformedVertices[i] = frame.verticesUnedited[i] + shiftSingle;
                    continue;
                }

                var effectiveNeighborCount = availableNeighbors - 1;
                var weights = new float[effectiveNeighborCount];
                var normalizerDistance = distances[availableNeighbors - 1];

                var shift = new Vector3();
                var weightSum = 0f;

                for (var j = 0; j < effectiveNeighborCount; j++)
                {
                    weights[j] = normalizerDistance < Epsilon ? 1f : 1f - distances[j] / normalizerDistance;
                    weightSum += weights[j];
                }

                if (weightSum < Epsilon)
                {
                    var weight = 1f / effectiveNeighborCount;

                    for (var j = 0; j < effectiveNeighborCount; j++)
                    {
                        if (indices[j] < 0 || indices[j] >= frame.centers.Length || indices[j] >= frame.centersUnedited.Length)
                            continue;

                        var cb = frame.centersUnedited[indices[j]];
                        var ca = frame.centers[indices[j]];
                        shift += (ca - cb) * weight;
                    }
                }
                else
                {
                    for (var j = 0; j < effectiveNeighborCount; j++)
                    {
                        if (indices[j] < 0 || indices[j] >= frame.centers.Length || indices[j] >= frame.centersUnedited.Length)
                            continue;

                        var original = frame.centersUnedited[indices[j]];
                        var edited = frame.centers[indices[j]];
                        shift += (edited - original) * (weights[j] / weightSum);
                    }
                }

                deformedVertices[i] = frame.verticesUnedited[i] + shift;
            }

            return deformedVertices;
        }
    }
}
