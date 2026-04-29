using System.Numerics;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Structures;

namespace TVMEditor.Editing.CenterDeformation
{
    public class AffinityCenterDeformation : ICenterDeformation
    {
        public IAffinityCalculation AffinityCalculation { get; set; }

        public AffinityCenterDeformation(IAffinityCalculation affinityCalculation)
        {
            AffinityCalculation = affinityCalculation;
        }

        public Vector3[] DeformCenters(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, ref DualQuaternion[] transformations)
        {
            var newCenters = new Vector3[centers.Length];
            var centersAffinity = AffinityCalculation.GetCentersAffinity();
            var newTransformations = new DualQuaternion[centers.Length];

            for (var i = 0; i < centers.Length; i++)
            {
                var weightedDifference = DualQuaternion.Zero();
                var weightSum = 0f;

                for (var j = 0; j < centerIndices.Length; j++)
                {
                    var centerIndex = centerIndices[j];
                    var w = centersAffinity[i, centerIndex];
                    weightedDifference += w * transformations[centerIndex];
                    weightSum += w;
                }

                if (!float.IsFinite(weightSum) || weightSum <= 1e-8f)
                {
                    weightedDifference = DualQuaternion.Identity();
                }
                else
                {
                    weightedDifference /= weightSum;
                    weightedDifference = weightedDifference.Normalize();
                }
                newTransformations[i] = weightedDifference;
                newCenters[i] = weightedDifference.Transform(centers[i]);
            }

            for (var c = 0; c < centerIndices.Length; c++)
            {
                newCenters[centerIndices[c]] = newPositions[c];
                newTransformations[centerIndices[c]] = transformations[centerIndices[c]];
            }

            transformations = newTransformations;
            for (var c = 0; c < newCenters.Length; c++)
            {
                newCenters[c] = transformations[c].Transform(centers[c]);
            }

            return newCenters;
        }

        public Vector3[] DeformCenters2(Vector3[] centers, int[] centerIndices, Vector3[] newPositions, ref DualQuaternion[] transformations)
        {
            var newCenters = new Vector3[centers.Length];
            var centersAffinity = AffinityCalculation.GetCentersAffinity();
            var newTransformations = new DualQuaternion[centers.Length];

            for (var i = 0; i < centers.Length; i++)
            {
                var weightedDifference = DualQuaternion.Zero();

                for (var j = 0; j < centerIndices.Length; j++)
                {
                    var centerIndex = centerIndices[j];
                    var w = centersAffinity[i, centerIndex];
                    weightedDifference += w * transformations[centerIndex];
                }

                weightedDifference += (1 - 1) * DualQuaternion.Identity();
                weightedDifference = weightedDifference.Normalize();
                newTransformations[i] = weightedDifference;
                newCenters[i] = weightedDifference.Transform(centers[i]);
            }

            for (var c = 0; c < centerIndices.Length; c++)
            {
                newCenters[centerIndices[c]] = newPositions[c];
                newTransformations[centerIndices[c]] = transformations[centerIndices[c]];
            }

            transformations = newTransformations;
            for (var c = 0; c < newCenters.Length; c++)
            {
                newCenters[c] = transformations[c].Transform(centers[c]);
            }

            return newCenters;
        }
    }
}
