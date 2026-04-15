using System.Numerics;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class GaussianCenterDeformer
    {
        public float Sigma { get; }

        public GaussianCenterDeformer(float sigma = 1f)
        {
            Sigma = sigma;
        }

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
