using KdTree;
using KdTree.Math;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Structures;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TVMEditor.Editing.SurfaceDeformation
{
    public class CustomSurfaceDeformation : ISurfaceDeformation
    {
        public int MaxSplitIterations { get; set; } = 3;
        public int Neighbors { get; set; } = 5;
        public float LimitEpsilon { get; set; } = 1e-6f;
        public float Shape { get; set; } = 2f;
        public bool ResampleMesh { get; set; } = false;
        public IAffinityCalculation AffinityCalculation { get; set; }
        public List<CustomSurfaceDeformationCallProfile> CallProfiles { get; } = new List<CustomSurfaceDeformationCallProfile>();

        private readonly ConcurrentDictionary<int, FrameWeightCache> frameWeightCaches = new ConcurrentDictionary<int, FrameWeightCache>();

        public CustomSurfaceDeformation(IAffinityCalculation affinityCalculation)
        {
            AffinityCalculation = affinityCalculation;
        }

        public void ResetProfiling()
        {
            lock (CallProfiles)
            {
                CallProfiles.Clear();
            }
        }

        public void PrecomputeAllFrames(Vector3[][] verticesPerFrame, Vector3[][] centersPerFrame, int maxDegreeOfParallelism, CancellationToken cancellationToken)
        {
            if (verticesPerFrame == null || centersPerFrame == null)
                return;

            var frameCount = System.Math.Min(verticesPerFrame.Length, centersPerFrame.Length);
            if (frameCount == 0)
                return;

            var frameIndices = Enumerable.Range(0, frameCount);
            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = System.Math.Max(1, maxDegreeOfParallelism)
            };

            Parallel.ForEach(frameIndices, parallelOptions, frameIndex =>
            {
                if (frameWeightCaches.ContainsKey(frameIndex))
                    return;

                ComputeWeights(verticesPerFrame[frameIndex], centersPerFrame[frameIndex], frameIndex, parallelizeVertices: false, cancellationToken);
            });
        }

        public TriangleMesh DeformSurface(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations)
        {
            var profile = new CustomSurfaceDeformationCallProfile
            {
                FrameIndex = frameIndex,
                UsedCachedWeights = frameWeightCaches.ContainsKey(frameIndex)
            };
            var totalTimer = Stopwatch.StartNew();
            var stageTimer = Stopwatch.StartNew();

            if (!profile.UsedCachedWeights)
            {
                ComputeWeights(vertices, oldCenters, frameIndex, parallelizeVertices: true, CancellationToken.None);
            }
            stageTimer.Stop();
            profile.ComputeWeightsMs = stageTimer.Elapsed.TotalMilliseconds;

            var frameCache = frameWeightCaches[frameIndex];
            var centersArray = frameCache.Centers;
            var weightsArray = frameCache.Weights;
            var verticesList = new List<Vector3>();
            var vertexTransformations = new Dictionary<int, DualQuaternion>();

            stageTimer.Restart();
            for (var v = 0; v < vertices.Length; v++)
            {
                var weightedTransformation = DualQuaternion.Zero();
                for (var c = 0; c < centersArray[v].Length; c++)
                {
                    var centerIndex = centersArray[v][c];
                    var weight = weightsArray[v][c];
                    weightedTransformation += weight * transformations[centerIndex];
                }

                verticesList.Add(weightedTransformation.Normalize().Transform(vertices[v]));
                vertexTransformations[v] = weightedTransformation.Normalize();
            }
            stageTimer.Stop();
            profile.BlendVerticesMs = stageTimer.Elapsed.TotalMilliseconds;

            if (!ResampleMesh)
            {
                totalTimer.Stop();
                profile.TotalMs = totalTimer.Elapsed.TotalMilliseconds;
                RecordProfile(profile);

                return new TriangleMesh
                {
                    Vertices = verticesList.ToArray(),
                    Faces = faces
                };
            }

            stageTimer.Restart();
            var kdTree = new KdTree<float, int>(3, new FloatMath());
            for (var c = 0; c < oldCenters.Length; c++)
            {
                var centerPosition = oldCenters[c];
                kdTree.Add(new[] { centerPosition.X, centerPosition.Y, centerPosition.Z }, c);
            }

            var newFaces = new HashSet<TVMEditor.Structures.Face>(faces);
            var edgesMidPoints = new Dictionary<Edge, int>();
            var resampleIteration = 0;

            while (resampleIteration++ < MaxSplitIterations)
            {
                var oppositeVertices = new Dictionary<Edge, int>();
                foreach (var face in newFaces)
                {
                    oppositeVertices[new Edge(face.V1, face.V2)] = face.V3;
                    oppositeVertices[new Edge(face.V2, face.V3)] = face.V1;
                    oppositeVertices[new Edge(face.V3, face.V1)] = face.V2;
                }

                var edgesToSplit = new List<Edge>();
                var facesToSplit = new List<TVMEditor.Structures.Face>();

                foreach (var face in newFaces)
                {
                    if (CheckSplitEdge(new Edge(face.V1, face.V2), vertexTransformations, verticesList))
                    {
                        edgesToSplit.Add(new Edge(face.V1, face.V2));
                        facesToSplit.Add(face);
                    }
                    if (CheckSplitEdge(new Edge(face.V2, face.V3), vertexTransformations, verticesList))
                    {
                        edgesToSplit.Add(new Edge(face.V2, face.V3));
                        facesToSplit.Add(face);
                    }
                    if (CheckSplitEdge(new Edge(face.V3, face.V1), vertexTransformations, verticesList))
                    {
                        edgesToSplit.Add(new Edge(face.V3, face.V1));
                        facesToSplit.Add(face);
                    }
                }

                if (edgesToSplit.Count == 0)
                    break;

                for (var i = 0; i < edgesToSplit.Count; i++)
                {
                    var edgeToSplit = edgesToSplit[i];
                    var midPointIndex = verticesList.Count;
                    if (edgesMidPoints.ContainsKey(edgeToSplit.Unoriented()))
                    {
                        midPointIndex = edgesMidPoints[edgeToSplit.Unoriented()];
                    }
                    else
                    {
                        var oldMid = 0.5f * (
                            vertexTransformations[edgeToSplit.V1].Conjugate().Transform(verticesList[edgeToSplit.V1]) +
                            vertexTransformations[edgeToSplit.V2].Conjugate().Transform(verticesList[edgeToSplit.V2]));
                        var affinity = AffinityCalculation.GetCentersAffinity();
                        var affinityThreshold = 0.1;
                        var (vertexIndices, vertexWeights) = ComputeCustomWeightsForVertex(kdTree, vertices, oldMid, affinity, affinityThreshold, oldCenters);
                        frameCache.Centers.Add(vertexIndices);
                        frameCache.Weights.Add(vertexWeights);
                        var newMid = TransformPoint(oldMid, midPointIndex, frameIndex, transformations, out var transform);
                        vertexTransformations.Add(midPointIndex, transform);
                        edgesMidPoints[edgeToSplit.Unoriented()] = midPointIndex;
                        verticesList.Add(newMid);
                    }

                    var faceToSplit = facesToSplit[i];
                    if (!newFaces.Contains(faceToSplit))
                        faceToSplit = newFaces.Single(f => f.Edges.Contains(edgeToSplit));

                    newFaces.Remove(faceToSplit);
                    newFaces.Add(new TVMEditor.Structures.Face { V1 = oppositeVertices[edgeToSplit], V2 = edgeToSplit.V1, V3 = midPointIndex });
                    newFaces.Add(new TVMEditor.Structures.Face { V1 = oppositeVertices[edgeToSplit], V2 = midPointIndex, V3 = edgeToSplit.V2 });

                    oppositeVertices[new Edge(edgeToSplit.V1, midPointIndex)] = oppositeVertices[edgeToSplit];
                    oppositeVertices[new Edge(midPointIndex, edgeToSplit.V2)] = oppositeVertices[edgeToSplit];
                    oppositeVertices[new Edge(oppositeVertices[edgeToSplit], midPointIndex)] = edgeToSplit.V2;
                    oppositeVertices[new Edge(midPointIndex, oppositeVertices[edgeToSplit])] = edgeToSplit.V1;
                    oppositeVertices[new Edge(oppositeVertices[edgeToSplit], edgeToSplit.V1)] = midPointIndex;
                    oppositeVertices[new Edge(edgeToSplit.V2, oppositeVertices[edgeToSplit])] = midPointIndex;
                    oppositeVertices.Remove(edgeToSplit);
                }
            }

            stageTimer.Stop();
            profile.ResampleMs = stageTimer.Elapsed.TotalMilliseconds;
            totalTimer.Stop();
            profile.TotalMs = totalTimer.Elapsed.TotalMilliseconds;
            RecordProfile(profile);

            return new TriangleMesh { Vertices = verticesList.ToArray(), Faces = newFaces.ToArray() };
        }

        public DualQuaternion[] ComputeDeformations(Vector3[] vertices, TVMEditor.Structures.Face[] faces, Vector3[] oldCenters, Vector3[] newCenters, int frameIndex, DualQuaternion[] transformations)
        {
            if (!frameWeightCaches.ContainsKey(frameIndex))
                ComputeWeights(vertices, oldCenters, frameIndex, parallelizeVertices: true, CancellationToken.None);

            var frameCache = frameWeightCaches[frameIndex];
            var centersArray = frameCache.Centers;
            var weightsArray = frameCache.Weights;
            var verticesList = new List<Vector3>();
            var vertexTransformations = new Dictionary<int, DualQuaternion>();

            for (var v = 0; v < vertices.Length; v++)
            {
                var weightedTransformation = DualQuaternion.Zero();
                for (var c = 0; c < centersArray[v].Length; c++)
                {
                    var centerIndex = centersArray[v][c];
                    var weight = weightsArray[v][c];
                    weightedTransformation += weight * transformations[centerIndex];
                }

                verticesList.Add(weightedTransformation.Normalize().Transform(vertices[v]));
                vertexTransformations[v] = weightedTransformation.Normalize();
            }

            var deformations = new DualQuaternion[vertices.Length];
            for (var i = 0; i < deformations.Length; i++)
            {
                deformations[i] = vertexTransformations[i];
            }

            return deformations;
        }

        private bool CheckSplitEdge(Edge edge, Dictionary<int, DualQuaternion> vertexTransformations, IList<Vector3> newVerticesList)
        {
            var a = vertexTransformations[edge.V1];
            var b = vertexTransformations[edge.V2];
            var a2b = a * b.Conjugate();
            var cos = a2b.Real.W;
            return cos < System.Math.Cos(System.Math.PI * 0.01);
        }

        private Vector3 TransformPoint(Vector3 point, int pointIndex, int frameIndex, DualQuaternion[] transformations, out DualQuaternion transform)
        {
            var frameCache = frameWeightCaches[frameIndex];
            var weightedTransformation = DualQuaternion.Zero();
            for (var c = 0; c < frameCache.Centers[pointIndex].Length; c++)
            {
                var centerIndex = frameCache.Centers[pointIndex][c];
                var weight = frameCache.Weights[pointIndex][c];
                weightedTransformation += weight * transformations[centerIndex];
            }

            transform = weightedTransformation.Normalize();
            return transform.Transform(point);
        }

        private FrameWeightCache ComputeWeights(Vector3[] vertices, Vector3[] oldCenters, int frameIndex, bool parallelizeVertices, CancellationToken cancellationToken)
        {
            if (frameWeightCaches.TryGetValue(frameIndex, out var existingCache))
                return existingCache;

            var indices = new int[vertices.Length][];
            var weights = new float[vertices.Length][];
            var kdTree = new KdTree<float, int>(3, new FloatMath());
            for (var c = 0; c < oldCenters.Length; c++)
            {
                var centerPosition = oldCenters[c];
                kdTree.Add(new[] { centerPosition.X, centerPosition.Y, centerPosition.Z }, c);
            }

            var affinity = AffinityCalculation.GetCentersAffinity();
            var affinityThreshold = 0.1;

            if (parallelizeVertices)
            {
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = cancellationToken
                };

                Parallel.For(0, vertices.Length, parallelOptions, v =>
                {
                    var (vertexIndices, vertexWeights) = ComputeCustomWeightsForVertex(kdTree, vertices, vertices[v], affinity, affinityThreshold, oldCenters);
                    indices[v] = vertexIndices;
                    weights[v] = vertexWeights;
                });
            }
            else
            {
                for (var v = 0; v < vertices.Length; v++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (vertexIndices, vertexWeights) = ComputeCustomWeightsForVertex(kdTree, vertices, vertices[v], affinity, affinityThreshold, oldCenters);
                    indices[v] = vertexIndices;
                    weights[v] = vertexWeights;
                }
            }

            var cache = new FrameWeightCache(indices.ToList(), weights.ToList());
            frameWeightCaches[frameIndex] = cache;
            return cache;
        }

        private (int[], float[]) ComputeCustomWeightsForVertex(KdTree<float, int> centersKdTree, Vector3[] vertices, Vector3 vertex, float[,] affinity, double affinityThreshold, Vector3[] oldCenters)
        {
            var searchCount = Neighbors;
            var found = 0;
            KdTreeNode<float, int>[] nearestCenters = null;
            int nearestCenter;

            do
            {
                nearestCenters = centersKdTree.GetNearestNeighbours(new[] { vertex.X, vertex.Y, vertex.Z }, searchCount);
                searchCount *= 2;
                nearestCenter = nearestCenters[0].Value;
                found = 0;
                for (var i = 0; i < nearestCenters.Length; i++)
                {
                    if (nearestCenters[i] == null)
                        continue;

                    var centerIndex = nearestCenters[i].Value;
                    if (affinity[nearestCenter, centerIndex] > affinityThreshold)
                    {
                        found++;
                    }
                }

                if (searchCount > 200)
                    affinityThreshold = 0;
            }
            while (found < Neighbors);

            var mostAffineCenters = nearestCenters.Select(n => n.Value).Take(Neighbors).ToArray();
            var distances = new float[Neighbors];
            var distMin = float.PositiveInfinity;

            for (var i = 0; i < distances.Length; i++)
            {
                distances[i] = (oldCenters[mostAffineCenters[i]] - vertex).Length();
                if (distances[i] < distMin)
                    distMin = distances[i];
            }

            var softmin = new float[Neighbors];
            var softminSum = 0f;
            var softminMin = float.PositiveInfinity;
            for (var i = 0; i < softmin.Length; i++)
            {
                softminSum += (float)System.Math.Exp(-distances[i] / (Shape * distMin + LimitEpsilon));
            }
            for (var i = 0; i < softmin.Length; i++)
            {
                softmin[i] = (float)System.Math.Exp(-distances[i] / (Shape * distMin + LimitEpsilon)) / softminSum;
                if (softmin[i] < softminMin)
                    softminMin = softmin[i];
            }

            var weights1 = new float[Neighbors];
            var weightsSum = 0f;
            for (var i = 0; i < weights1.Length; i++)
            {
                weights1[i] = softmin[i] - softminMin + 1e-6f;
                weightsSum += weights1[i];
            }

            for (var i = 0; i < weights1.Length; i++)
            {
                weights1[i] /= weightsSum;
            }

            return (mostAffineCenters, weights1);
        }

        private void RecordProfile(CustomSurfaceDeformationCallProfile profile)
        {
            lock (CallProfiles)
            {
                CallProfiles.Add(profile);
            }
        }

        private sealed class FrameWeightCache
        {
            public FrameWeightCache(List<int[]> centers, List<float[]> weights)
            {
                Centers = centers;
                Weights = weights;
            }

            public List<int[]> Centers { get; }
            public List<float[]> Weights { get; }
        }
    }
}
