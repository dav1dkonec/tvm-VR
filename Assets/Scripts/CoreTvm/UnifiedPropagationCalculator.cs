using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreTvm
{
    public sealed class UnifiedPropagationCalculator : IPropagationCalculator
    {
        private const float SmallVectorThreshold = 1e-12f;
        private const int QuaternionPowerIterations = 12;

        public PropagationResult Propagate(Vector3[][] centers, int editedFrameIndex, Vector3[] oldCenters, Vector3[] newCenters, float[,] affinity, EditingOptions options)
        {
            if (centers == null || centers.Length == 0)
            {
                return new PropagationResult();
            }

            var result = CloneCenters(centers);
            var affectedFrames = new List<int>();
            var propagationBlend = Clamp01(options?.PropagationBlendAlpha ?? 0.65f);
            var threshold = MathF.Max(options?.AffectedFrameThreshold ?? 0.001f, 0f);
            var editedTranslations = ComputeTranslations(oldCenters, newCenters);
            var neighborMap = BuildNeighborMap(oldCenters, affinity, options?.PropagationNeighbors ?? 4);

            for (var frameIndex = 0; frameIndex < centers.Length; frameIndex++)
            {
                if (frameIndex == editedFrameIndex)
                {
                    result[frameIndex] = CloneCenters(newCenters);
                    affectedFrames.Add(frameIndex);
                    continue;
                }

                var attenuation = GetTimeAttenuation(editedFrameIndex, frameIndex, options);
                if (attenuation <= threshold)
                    continue;

                result[frameIndex] = PropagateFrame(
                    centers[frameIndex],
                    oldCenters,
                    editedTranslations,
                    affinity,
                    neighborMap,
                    options,
                    attenuation,
                    propagationBlend);

                affectedFrames.Add(frameIndex);
            }

            return new PropagationResult
            {
                Centers = result,
                AffectedFrames = affectedFrames.ToArray()
            };
        }

        private Vector3[] PropagateFrame(
            Vector3[] frameCenters,
            Vector3[] referenceCenters,
            Vector3[] editedTranslations,
            float[,] affinity,
            int[][] neighborMap,
            EditingOptions options,
            float attenuation,
            float propagationBlend)
        {
            var count = frameCenters.Length;
            var propagated = new Vector3[count];

            for (var centerIndex = 0; centerIndex < count; centerIndex++)
            {
                var neighbors = centerIndex < neighborMap.Length ? neighborMap[centerIndex] : Array.Empty<int>();
                var rotation = EstimateRotation(referenceCenters, frameCenters, neighbors);

                var affinityTranslation = ComputeAffinityTranslation(centerIndex, editedTranslations, affinity, neighbors);
                var rotatedAffinityTranslation = Vector3.Transform(affinityTranslation, rotation);
                var gaussianTranslation = ComputeGaussianTranslation(centerIndex, referenceCenters, editedTranslations, neighbors, options);

                var blendedTranslation =
                    propagationBlend * rotatedAffinityTranslation +
                    (1f - propagationBlend) * gaussianTranslation;

                propagated[centerIndex] = frameCenters[centerIndex] + attenuation * blendedTranslation;
            }

            return propagated;
        }

        private static int[][] BuildNeighborMap(Vector3[] referenceCenters, float[,] affinity, int requestedNeighbors)
        {
            if (referenceCenters == null || referenceCenters.Length == 0)
                return Array.Empty<int[]>();

            var centerCount = referenceCenters.Length;
            var neighborCount = Math.Max(1, Math.Min(requestedNeighbors, centerCount));
            var neighborMap = new int[centerCount][];

            for (var centerIndex = 0; centerIndex < centerCount; centerIndex++)
            {
                var scores = new List<(int Index, float Score, float Distance)>(centerCount);
                var source = referenceCenters[centerIndex];

                for (var i = 0; i < centerCount; i++)
                {
                    var score = affinity != null && centerIndex < affinity.GetLength(0) && i < affinity.GetLength(1)
                        ? affinity[centerIndex, i]
                        : 0f;
                    var distance = Vector3.Distance(source, referenceCenters[i]);
                    scores.Add((i, score, distance));
                }

                scores.Sort((left, right) =>
                {
                    var scoreCompare = right.Score.CompareTo(left.Score);
                    return scoreCompare != 0 ? scoreCompare : left.Distance.CompareTo(right.Distance);
                });

                var neighbors = new int[neighborCount];
                for (var i = 0; i < neighborCount; i++)
                {
                    neighbors[i] = scores[i].Index;
                }

                neighborMap[centerIndex] = neighbors;
            }

            return neighborMap;
        }

        private static Quaternion EstimateRotation(Vector3[] referenceCenters, Vector3[] frameCenters, int[] neighbors)
        {
            if (neighbors.Length < 2)
                return Quaternion.Identity;

            var referenceCentroid = Vector3.Zero;
            var frameCentroid = Vector3.Zero;

            for (var i = 0; i < neighbors.Length; i++)
            {
                referenceCentroid += referenceCenters[neighbors[i]];
                frameCentroid += frameCenters[neighbors[i]];
            }

            referenceCentroid /= neighbors.Length;
            frameCentroid /= neighbors.Length;

            var sxx = 0f;
            var sxy = 0f;
            var sxz = 0f;
            var syx = 0f;
            var syy = 0f;
            var syz = 0f;
            var szx = 0f;
            var szy = 0f;
            var szz = 0f;

            for (var i = 0; i < neighbors.Length; i++)
            {
                var p = referenceCenters[neighbors[i]] - referenceCentroid;
                var q = frameCenters[neighbors[i]] - frameCentroid;

                sxx += p.X * q.X;
                sxy += p.X * q.Y;
                sxz += p.X * q.Z;
                syx += p.Y * q.X;
                syy += p.Y * q.Y;
                syz += p.Y * q.Z;
                szx += p.Z * q.X;
                szy += p.Z * q.Y;
                szz += p.Z * q.Z;
            }

            var trace = sxx + syy + szz;
            if (MathF.Abs(trace) < SmallVectorThreshold &&
                MathF.Abs(sxy) < SmallVectorThreshold &&
                MathF.Abs(sxz) < SmallVectorThreshold &&
                MathF.Abs(syx) < SmallVectorThreshold &&
                MathF.Abs(syz) < SmallVectorThreshold &&
                MathF.Abs(szx) < SmallVectorThreshold &&
                MathF.Abs(szy) < SmallVectorThreshold)
            {
                return Quaternion.Identity;
            }

            var horn = new float[4, 4];
            horn[0, 0] = trace;
            horn[0, 1] = syz - szy;
            horn[0, 2] = szx - sxz;
            horn[0, 3] = sxy - syx;

            horn[1, 0] = syz - szy;
            horn[1, 1] = sxx - syy - szz;
            horn[1, 2] = sxy + syx;
            horn[1, 3] = szx + sxz;

            horn[2, 0] = szx - sxz;
            horn[2, 1] = sxy + syx;
            horn[2, 2] = -sxx + syy - szz;
            horn[2, 3] = syz + szy;

            horn[3, 0] = sxy - syx;
            horn[3, 1] = szx + sxz;
            horn[3, 2] = syz + szy;
            horn[3, 3] = -sxx - syy + szz;

            var eigenVector = new Vector4(1f, 0f, 0f, 0f);
            for (var iteration = 0; iteration < QuaternionPowerIterations; iteration++)
            {
                eigenVector = Multiply(horn, eigenVector);
                if (eigenVector.LengthSquared() < SmallVectorThreshold)
                    return Quaternion.Identity;

                eigenVector = Vector4.Normalize(eigenVector);
            }

            var rotation = new Quaternion(eigenVector.Y, eigenVector.Z, eigenVector.W, eigenVector.X);
            if (rotation.LengthSquared() < SmallVectorThreshold)
                return Quaternion.Identity;

            return Quaternion.Normalize(rotation);
        }

        private static Vector3 ComputeAffinityTranslation(int centerIndex, Vector3[] editedTranslations, float[,] affinity, int[] neighbors)
        {
            if (neighbors.Length == 0)
                return Vector3.Zero;

            var blended = Vector3.Zero;
            var weightSum = 0f;

            for (var i = 0; i < neighbors.Length; i++)
            {
                var neighborIndex = neighbors[i];
                var weight = 1f;

                if (affinity != null && centerIndex < affinity.GetLength(0) && neighborIndex < affinity.GetLength(1))
                    weight = affinity[centerIndex, neighborIndex];

                blended += editedTranslations[neighborIndex] * weight;
                weightSum += weight;
            }

            return weightSum > SmallVectorThreshold ? blended / weightSum : Vector3.Zero;
        }

        private static Vector3 ComputeGaussianTranslation(int centerIndex, Vector3[] referenceCenters, Vector3[] editedTranslations, int[] neighbors, EditingOptions options)
        {
            if (neighbors.Length == 0)
                return Vector3.Zero;

            var sigma = MathF.Max(options?.GaussianSigma ?? 1f, 0.0001f);
            var source = referenceCenters[centerIndex];
            var blended = Vector3.Zero;
            var weightSum = 0f;

            for (var i = 0; i < neighbors.Length; i++)
            {
                var neighborIndex = neighbors[i];
                var weight = MathF.Exp(-sigma * Vector3.Distance(source, referenceCenters[neighborIndex]));
                blended += editedTranslations[neighborIndex] * weight;
                weightSum += weight;
            }

            return weightSum > SmallVectorThreshold ? blended / weightSum : Vector3.Zero;
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

        private static float GetTimeAttenuation(int editedFrameIndex, int frameIndex, EditingOptions options)
        {
            var frameDistance = MathF.Abs(frameIndex - editedFrameIndex);
            var shape = MathF.Max(options?.TimeAttenuationShape ?? 0.15f, 0.0001f);
            return MathF.Exp(-shape * frameDistance * frameDistance);
        }

        private static Vector4 Multiply(float[,] matrix, Vector4 vector)
        {
            return new Vector4(
                matrix[0, 0] * vector.X + matrix[0, 1] * vector.Y + matrix[0, 2] * vector.Z + matrix[0, 3] * vector.W,
                matrix[1, 0] * vector.X + matrix[1, 1] * vector.Y + matrix[1, 2] * vector.Z + matrix[1, 3] * vector.W,
                matrix[2, 0] * vector.X + matrix[2, 1] * vector.Y + matrix[2, 2] * vector.Z + matrix[2, 3] * vector.W,
                matrix[3, 0] * vector.X + matrix[3, 1] * vector.Y + matrix[3, 2] * vector.Z + matrix[3, 3] * vector.W);
        }

        private static Vector3[][] CloneCenters(Vector3[][] centers)
        {
            var clone = new Vector3[centers.Length][];
            for (var i = 0; i < centers.Length; i++)
            {
                clone[i] = CloneCenters(centers[i]);
            }

            return clone;
        }

        private static Vector3[] CloneCenters(Vector3[] centers)
        {
            var clone = new Vector3[centers.Length];
            Array.Copy(centers, clone, centers.Length);
            return clone;
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
