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
            var deformedVertices = new Vector3[frame.vertices.Length];
            var weights = new float[Neighbors - 1];

            for (var i = 0; i < frame.vertices.Length; i++)
            {
                var indices = frame.nearestCentersIndex[i];
                var distances = frame.nearestCentersDist[i];

                var shift = new Vector3();
                var weightSum = 0f;

                for (var j = 0; j < Neighbors - 1; j++)
                {
                    weights[j] = 1f - distances[j] / distances[Neighbors - 1];
                    weightSum += weights[j];
                }

                if (weightSum < Epsilon)
                {
                    var weight = 1f / (Neighbors - 1);

                    for (var j = 0; j < Neighbors - 1; j++)
                    {
                        var cb = frame.centersUnedited[indices[j]];
                        var ca = frame.centers[indices[j]];
                        shift += (ca - cb) * weight;
                    }
                }
                else
                {
                    for (var j = 0; j < Neighbors - 1; j++)
                    {
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
