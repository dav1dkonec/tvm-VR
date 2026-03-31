using System;
using System.Collections.Generic;
using System.Numerics;

namespace CoreTvm
{
    public sealed class UnifiedCenterDeformationCalculator : ICenterDeformationCalculator
    {
        private const float Epsilon = 0.0001f;

        public Vector3[] Deform(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, float[,] affinity, EditingOptions options)
        {
            if (centers == null)
                return Array.Empty<Vector3>();

            if (centerIndices == null || newPositions == null || centerIndices.Length != newPositions.Length)
                throw new ArgumentException("Center indices and target positions must have the same length.");

            var result = new Vector3[centers.Length];
            Array.Copy(centers, result, centers.Length);

            if (centers.Length == 0 || centerIndices.Length == 0)
                return result;

            var sigma = MathF.Max(options?.GaussianSigma ?? 1f, Epsilon);
            var centerBlend = Clamp01(options?.CenterBlendAlpha ?? 0.6f);
            var editedCenters = BuildEditedCenterLookup(centerIndices, newPositions);

            for (var i = 0; i < centers.Length; i++)
            {
                if (editedCenters.TryGetValue(i, out var editedPosition))
                {
                    result[i] = editedPosition;
                    continue;
                }

                var affinityDelta = Vector3.Zero;
                var affinityWeightSum = 0f;
                var gaussianDelta = Vector3.Zero;
                var gaussianWeightSum = 0f;

                for (var j = 0; j < centerIndices.Length; j++)
                {
                    var effectorIndex = centerIndices[j];
                    var translation = newPositions[j] - centers[effectorIndex];

                    var affinityWeight = GetAffinityWeight(affinity, i, effectorIndex);
                    affinityDelta += translation * affinityWeight;
                    affinityWeightSum += affinityWeight;

                    var gaussianWeight = GaussianFalloff(centers[i], centers[effectorIndex], sigma);
                    gaussianDelta += translation * gaussianWeight;
                    gaussianWeightSum += gaussianWeight;
                }

                var affinityContribution = affinityWeightSum > Epsilon
                    ? affinityDelta
                    : Vector3.Zero;
                var gaussianContribution = gaussianWeightSum > Epsilon
                    ? gaussianDelta
                    : Vector3.Zero;

                var blendedTranslation =
                    centerBlend * affinityContribution +
                    (1f - centerBlend) * gaussianContribution;

                result[i] = centers[i] + blendedTranslation;
            }

            return result;
        }

        private static Dictionary<int, Vector3> BuildEditedCenterLookup(int[] centerIndices, Vector3[] newPositions)
        {
            var lookup = new Dictionary<int, Vector3>(centerIndices.Length);

            for (var i = 0; i < centerIndices.Length; i++)
            {
                lookup[centerIndices[i]] = newPositions[i];
            }

            return lookup;
        }

        private static float GetAffinityWeight(float[,] affinity, int centerIndex, int effectorIndex)
        {
            if (affinity == null)
                return 0f;

            if (centerIndex < 0 || effectorIndex < 0)
                return 0f;

            if (centerIndex >= affinity.GetLength(0) || effectorIndex >= affinity.GetLength(1))
                return 0f;

            return affinity[centerIndex, effectorIndex];
        }

        private static float GaussianFalloff(Vector3 source, Vector3 target, float sigma)
        {
            return MathF.Exp(-sigma * Vector3.Distance(source, target));
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
