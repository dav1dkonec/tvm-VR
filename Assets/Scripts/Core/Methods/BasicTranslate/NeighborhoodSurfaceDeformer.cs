using System.Numerics;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Reconstructs mesh vertices from nearby center displacement vectors.
    /// </summary>
    public sealed class NeighborhoodSurfaceDeformer
    {
        /// <summary>
        /// Number of nearest centers stored per vertex.
        /// </summary>
        public int Neighbors { get; }

        /// <summary>
        /// Minimum usable weight sum before falling back to uniform weights.
        /// </summary>
        public float Epsilon { get; }

        /// <summary>
        /// Creates a neighborhood surface deformer.
        /// </summary>
        public NeighborhoodSurfaceDeformer(int neighbors = 6, float epsilon = 0.0001f)
        {
            Neighbors = neighbors;
            Epsilon = epsilon;
        }

        /// <summary>
        /// Applies weighted center shifts to the original vertices of a frame.
        /// </summary>
        public Vector3[] DeformSurface(Frame frame)
        {
            var deformedVertices = new Vector3[frame.vertices.Length];
            var weights = new float[Neighbors - 1];

            for (var i = 0; i < frame.vertices.Length; i++)
            {
                (var indices, var distances) = (frame.nearestCentersIndex[i], frame.nearestCentersDist[i]);

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
                        var original = frame.centersUnedited[indices[j]];
                        var edited = frame.centers[indices[j]];
                        shift += (edited - original) * weight;
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
