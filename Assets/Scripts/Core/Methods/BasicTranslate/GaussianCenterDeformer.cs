using System.Numerics;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Applies a distance-based Gaussian falloff to centers around the edited center.
    /// </summary>
    public sealed class GaussianCenterDeformer
    {
        /// <summary>
        /// Falloff sharpness used by the Gaussian influence function.
        /// </summary>
        public float Sigma { get; }

        /// <summary>
        /// Creates a Gaussian center deformer.
        /// </summary>
        public GaussianCenterDeformer(float sigma = 1f)
        {
            Sigma = sigma;
        }

        /// <summary>
        /// Translates all centers by the edited center translation scaled by Gaussian falloff.
        /// </summary>
        public Vector3[] DeformCenters(int centerIndex, Vector3 translation, Vector3[] centers)
        {
            var deformedCenters = new Vector3[centers.Length];
            var translatedCenter = centers[centerIndex];

            for (var i = 0; i < centers.Length; i++)
            {
                var targetCenter = centers[i];
                var strength = GetGaussianFalloff(translatedCenter, targetCenter);
                deformedCenters[i] = targetCenter + translation * strength;
            }

            return deformedCenters;
        }

        private float GetGaussianFalloff(Vector3 from, Vector3 to)
        {
            return UnityEngine.Mathf.Exp(-Sigma * Vector3.Distance(from, to));
        }
    }
}
