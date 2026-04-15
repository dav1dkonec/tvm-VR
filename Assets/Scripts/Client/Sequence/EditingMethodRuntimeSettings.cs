using UnityEngine;
using TvmVr2.Api.Enums;

namespace TvmVr2.Client.Sequence
{
    public sealed class EditingMethodRuntimeSettings : MonoBehaviour
    {
        public MethodKind CurrentMethod = MethodKind.BasicTranslate;
        public float CenterSigma = 1f;
        public int SequenceNeighborCount = 4;
        public int SurfaceNeighborCount = 6;
        public float InflateRadius = 0.08f;
        public float InflateStrength = 0.02f;
        public InflateDeflateMode InflateMode = InflateDeflateMode.Inflate;

        public void SetSequenceNeighborCount(int value)
        {
            SequenceNeighborCount = Mathf.Max(1, value);
        }

        public void SetSurfaceNeighborCount(int value)
        {
            SurfaceNeighborCount = Mathf.Max(2, value);
        }

        public void SetInflateRadius(float value)
        {
            InflateRadius = Mathf.Max(0.001f, value);
        }

        public void SetInflateStrength(float value)
        {
            InflateStrength = Mathf.Max(0f, value);
        }

        public void SetInflateMode(int value)
        {
            InflateMode = value <= 0 ? InflateDeflateMode.Inflate : InflateDeflateMode.Deflate;
        }
    }
}
