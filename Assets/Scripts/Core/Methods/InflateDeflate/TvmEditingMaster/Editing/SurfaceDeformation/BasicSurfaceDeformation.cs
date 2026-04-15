using KdTree;
using KdTree.Math;
using System.Numerics;
using TVMEditor.Structures;

namespace TVMEditor.Editing.SurfaceDeformation
{
    public class BasicSurfaceDeformation : ISurfaceDeformation
    {
        public int Neighbors { get; set; } = 6;

        public TriangleMesh DeformSurface(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations)
        {
            var newVertices = new Vector3[vertices.Length];
            var kd = new KdTree<float, int>(3, new FloatMath());
            for (var i = 0; i < oldCenters.Length; i++)
            {
                kd.Add(new[] { oldCenters[i].X, oldCenters[i].Y, oldCenters[i].Z }, i);
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var nearestIndicesSorted = kd.GetNearestNeighbours(new[] { v.X, v.Y, v.Z }, Neighbors);
                var distances = new float[nearestIndicesSorted.Length];

                for (var j = 0; j < nearestIndicesSorted.Length; j++)
                {
                    distances[j] = Vector3.Distance(v, oldCenters[nearestIndicesSorted[j].Value]);
                }

                var totalWeight = 0f;
                var vertexShift = Vector3.Zero;

                for (var j = 0; j < distances.Length - 1; j++)
                {
                    totalWeight += 1 - distances[j] / distances[distances.Length - 1];
                }

                for (var j = 0; j < distances.Length - 1; j++)
                {
                    var op = oldCenters[nearestIndicesSorted[j].Value];
                    var np = newCenters[nearestIndicesSorted[j].Value];
                    var w = 1 - distances[j] / distances[distances.Length - 1];
                    vertexShift += (np - op) * (w / totalWeight);
                }

                newVertices[i] = vertices[i] + vertexShift;
            }

            return new TriangleMesh { Vertices = newVertices, Faces = faces };
        }

        public DualQuaternion[] ComputeDeformations(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations)
        {
            var deformations = new DualQuaternion[vertices.Length];
            var kd = new KdTree<float, int>(3, new FloatMath());
            for (var i = 0; i < oldCenters.Length; i++)
            {
                kd.Add(new[] { oldCenters[i].X, oldCenters[i].Y, oldCenters[i].Z }, i);
            }

            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var nearestIndicesSorted = kd.GetNearestNeighbours(new[] { v.X, v.Y, v.Z }, Neighbors);
                var distances = new float[nearestIndicesSorted.Length];

                for (var j = 0; j < nearestIndicesSorted.Length; j++)
                {
                    distances[j] = Vector3.Distance(v, oldCenters[nearestIndicesSorted[j].Value]);
                }

                var totalWeight = 0f;
                var vertexShift = Vector3.Zero;

                for (var j = 0; j < distances.Length - 1; j++)
                {
                    totalWeight += 1 - distances[j] / distances[distances.Length - 1];
                }

                for (var j = 0; j < distances.Length - 1; j++)
                {
                    var op = oldCenters[nearestIndicesSorted[j].Value];
                    var np = newCenters[nearestIndicesSorted[j].Value];
                    var w = 1 - distances[j] / distances[distances.Length - 1];
                    vertexShift += (np - op) * (w / totalWeight);
                }

                deformations[i] = DualQuaternion.Translation(vertexShift);
            }

            return deformations;
        }
    }
}
