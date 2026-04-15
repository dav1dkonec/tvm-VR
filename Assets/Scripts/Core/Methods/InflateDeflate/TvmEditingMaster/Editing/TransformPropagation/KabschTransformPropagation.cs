using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Extensions;
using TVMEditor.Math;
using TVMEditor.Structures;

namespace TVMEditor.Editing.TransformPropagation
{
    public class KabschTransformPropagation : ITransformPropagation
    {
        private int Neighbors { get; set; } = 7;
        private float Shape { get; set; } = 1.0f;
        private TimeAttenuation TimeAttenuationFunction { get; set; } = TimeAttenuation.None;
        private float TimeAttenuationShape { get; set; } = 1f;
        private bool MultiplyByAffinity { get; set; } = true;
        private int[,] NeighborIndices { get; set; }
        private Dictionary<int, float[,]> NeighborWeights { get; set; } = new Dictionary<int, float[,]>();
        private Dictionary<int, float[]> NeighborWeightsSums { get; set; } = new Dictionary<int, float[]>();

        public IAffinityCalculation AffinityCalculation { get; set; }

        public KabschTransformPropagation(IAffinityCalculation affinityCalculation)
        {
            AffinityCalculation = affinityCalculation;
        }

        public Vector3[][] PropagateTransform(Vector3[][] centers, int frameIndex, Vector3[] oldCenters, Vector3[] newCenters, DualQuaternion[] transformations, out DualQuaternion[][] propagatedTransformations)
        {
            var result = new Vector3[centers.Length][];
            propagatedTransformations = new DualQuaternion[centers.Length][];

            if (NeighborIndices == null && AffinityCalculation != null)
                PrepareNeighborIndices();

            for (var f = 0; f < centers.Length; f++)
            {
                if (frameIndex == f)
                {
                    result[f] = newCenters;
                    propagatedTransformations[f] = transformations;
                }
                else
                {
                    result[f] = PropagateTransformToFrame(centers[f], frameIndex, f, oldCenters, newCenters, transformations, out propagatedTransformations[f]);
                }
            }

            return result;
        }

        public bool FrameIsAffected(int editedFrameIndex, int frameIndex)
        {
            return GetTimeAttenuation(editedFrameIndex, frameIndex) > 1e-3;
        }

        private void PrepareNeighborIndices()
        {
            var affinity = AffinityCalculation.GetCentersAffinity();
            var centersNum = affinity.GetLength(0);
            var neighbors = new int[centersNum, Neighbors];

            for (var c = 0; c < centersNum; c++)
            {
                var centerAffinities = new Tuple<int, float>[centersNum];
                for (var d = 0; d < centersNum; d++)
                {
                    centerAffinities[d] = new Tuple<int, float>(d, affinity[c, d]);
                }

                var centerNeighbors = centerAffinities.OrderByDescending(x => x.Item2).Select(x => x.Item1).Take(Neighbors).ToArray();
                for (var d = 0; d < Neighbors; d++)
                {
                    neighbors[c, d] = centerNeighbors[d];
                }
            }

            NeighborIndices = neighbors;
        }

        private bool NeighborsPrecomputed(int frameIndex)
        {
            return NeighborWeights.ContainsKey(frameIndex);
        }

        private void PrecomputeNeighbors(int frameIndex, Vector3[] centers)
        {
            float[,] affinity = null;
            if (AffinityCalculation != null)
            {
                affinity = AffinityCalculation.GetCentersAffinity();
            }
            else
            {
                affinity = new float[centers.Length, centers.Length];
                for (var i = 0; i < centers.Length; i++)
                {
                    for (var j = i + 1; j < centers.Length; j++)
                    {
                        affinity[i, j] = affinity[j, i] = -(centers[i] - centers[j]).Length();
                    }
                }

                MultiplyByAffinity = false;
            }

            var weights = new float[centers.Length, Neighbors];
            var weightsSums = new float[centers.Length];

            for (var c = 0; c < centers.Length; c++)
            {
                var weightsSum = 0f;
                for (var d = 0; d < Neighbors; d++)
                {
                    var weight = 1f;
                    if (MultiplyByAffinity)
                        weight *= AffinityCalculation.GetCentersAffinity()[c, NeighborIndices[c, d]];

                    weights[c, d] = weight;
                    weightsSum += weight;
                }

                weightsSums[c] = weightsSum;
                for (var d = 0; d < Neighbors; d++)
                {
                    weights[c, d] /= weightsSum;
                }
            }

            NeighborWeights[frameIndex] = weights;
            NeighborWeightsSums[frameIndex] = weightsSums;
        }

        public Vector3[] PropagateTransformToFrame(Vector3[] frameCenters, int deformedFrameIndex, int frameIndex, Vector3[] oldCenters, Vector3[] newCenters, DualQuaternion[] transformations, out DualQuaternion[] propagatedTransformations)
        {
            var propagatedCenters = new Vector3[frameCenters.Length];
            propagatedTransformations = new DualQuaternion[frameCenters.Length];
            var att = GetTimeAttenuation(deformedFrameIndex, frameIndex);

            if (!FrameIsAffected(deformedFrameIndex, frameIndex))
            {
                for (var c = 0; c < oldCenters.Length; c++)
                {
                    propagatedCenters[c] = frameCenters[c];
                    propagatedTransformations[c] = DualQuaternion.Identity();
                }

                return propagatedCenters;
            }

            if (!NeighborsPrecomputed(deformedFrameIndex))
                PrecomputeNeighbors(deformedFrameIndex, oldCenters);

            var weightsSums = NeighborWeightsSums[deformedFrameIndex];
            var neighbors = NeighborIndices;

            for (var c = 0; c < oldCenters.Length; c++)
            {
                var oldCentroid = new DenseVector(3);
                var frameCentroid = new DenseVector(3);

                for (var d = 0; d < Neighbors; d++)
                {
                    var weight = NeighborWeights[deformedFrameIndex][c, d];
                    oldCentroid[0] += weight * oldCenters[neighbors[c, d]].X;
                    oldCentroid[1] += weight * oldCenters[neighbors[c, d]].Y;
                    oldCentroid[2] += weight * oldCenters[neighbors[c, d]].Z;
                    frameCentroid[0] += weight * frameCenters[neighbors[c, d]].X;
                    frameCentroid[1] += weight * frameCenters[neighbors[c, d]].Y;
                    frameCentroid[2] += weight * frameCenters[neighbors[c, d]].Z;
                }

                oldCentroid /= weightsSums[c];
                frameCentroid /= weightsSums[c];

                var matrixP = new DenseMatrix(Neighbors, 3);
                var matrixQ = new DenseMatrix(Neighbors, 3);
                for (var d = 0; d < Neighbors; d++)
                {
                    matrixP[d, 0] = oldCenters[neighbors[c, d]].X - oldCentroid[0];
                    matrixP[d, 1] = oldCenters[neighbors[c, d]].Y - oldCentroid[1];
                    matrixP[d, 2] = oldCenters[neighbors[c, d]].Z - oldCentroid[2];

                    matrixQ[d, 0] = NeighborWeights[deformedFrameIndex][c, d] * (frameCenters[neighbors[c, d]].X - frameCentroid[0]);
                    matrixQ[d, 1] = NeighborWeights[deformedFrameIndex][c, d] * (frameCenters[neighbors[c, d]].Y - frameCentroid[1]);
                    matrixQ[d, 2] = NeighborWeights[deformedFrameIndex][c, d] * (frameCenters[neighbors[c, d]].Z - frameCentroid[2]);
                }

                var matrixH = matrixP.TransposeThisAndMultiply(matrixQ);
                var hSvd = matrixH.Svd();
                var det = System.Math.Sign((hSvd.VT.Transpose() * hSvd.U.Transpose()).Determinant());
                var matrixI = new DenseMatrix(3, 3);
                matrixI[0, 0] = 1;
                matrixI[1, 1] = 1;
                matrixI[2, 2] = det;
                var rotationMatrix = hSvd.VT.Transpose() * matrixI * hSvd.U.Transpose();
                var rotationQuaternion = Geometry.QuaternionFromMatrix(rotationMatrix);

                var x = frameCenters[c];
                x -= frameCentroid.ToVector3();
                x = Vector3.Transform(x, Quaternion.Inverse(rotationQuaternion));
                x += oldCentroid.ToVector3();
                x = transformations[c].Transform(x);
                x -= oldCentroid.ToVector3();
                x = Vector3.Transform(x, rotationQuaternion);
                x += frameCentroid.ToVector3();

                propagatedCenters[c] =
                    (
                        DualQuaternion.Translation(frameCentroid.ToVector3()) *
                        DualQuaternion.Rotation(rotationQuaternion) *
                        DualQuaternion.Translation(-oldCentroid.ToVector3()) *
                        transformations[c] *
                        DualQuaternion.Translation(oldCentroid.ToVector3()) *
                        DualQuaternion.Rotation(Quaternion.Inverse(rotationQuaternion)) *
                        DualQuaternion.Translation(-frameCentroid.ToVector3())
                    ).Transform(frameCenters[c]);

                propagatedTransformations[c] =
                    DualQuaternion.Translation(frameCentroid.ToVector3()) *
                    DualQuaternion.Rotation(rotationQuaternion) *
                    DualQuaternion.Translation(-oldCentroid.ToVector3()) *
                    transformations[c] *
                    DualQuaternion.Translation(oldCentroid.ToVector3()) *
                    DualQuaternion.Rotation(Quaternion.Inverse(rotationQuaternion)) *
                    DualQuaternion.Translation(-frameCentroid.ToVector3());

                propagatedTransformations[c] = (att * propagatedTransformations[c] + (1 - att) * DualQuaternion.Identity()).Normalize();
                propagatedCenters[c] = propagatedTransformations[c].Transform(frameCenters[c]);
            }

            return propagatedCenters;
        }

        private float GetTimeAttenuation(int f1, int f2)
        {
            var tDist = System.Math.Abs(f1 - f2);
            var fun = (TimeAttenuation)TimeAttenuationFunction;
            switch (fun)
            {
                case TimeAttenuation.None:
                    return 1f;
                case TimeAttenuation.Gauss:
                    return (float)System.Math.Exp(-TimeAttenuationShape * tDist * tDist);
                case TimeAttenuation.Bump:
                    if (tDist > -1 / TimeAttenuationShape && tDist < 1 / TimeAttenuationShape)
                        return (float)System.Math.Exp(-1f / (1 - TimeAttenuationShape * (tDist * tDist)));
                    return 0;
                case TimeAttenuation.Linear:
                    return System.Math.Max(1 - (tDist / TimeAttenuationShape), 0);
            }

            throw new NotImplementedException($"Attenuation {fun} not implemented.");
        }
    }
}
