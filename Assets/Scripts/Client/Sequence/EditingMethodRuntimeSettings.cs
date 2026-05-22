using UnityEngine;
using TvmVr2.Api.Enums;

namespace TvmVr2.Client.Sequence
{
    /// <summary>
    /// Runtime settings shared by editing UI controls.
    /// </summary>
    public sealed class EditingMethodRuntimeSettings : MonoBehaviour
    {
        /// <summary>
        /// Currently selected editing method.
        /// </summary>
        public MethodKind CurrentMethod = MethodKind.BasicTranslate;

        /// <summary>
        /// Basic translate center falloff sigma.
        /// </summary>
        public float CenterSigma = 1f;

        /// <summary>
        /// Basic translate sequence neighbor count.
        /// </summary>
        public int SequenceNeighborCount = 4;

        /// <summary>
        /// Basic translate surface neighbor count.
        /// </summary>
        public int SurfaceNeighborCount = 6;

        /// <summary>
        /// Inflate/deflate strength.
        /// </summary>
        public float InflateStrength = 0.20f;

        /// <summary>
        /// Inflate/deflate mode.
        /// </summary>
        public InflateDeflateMode InflateMode = InflateDeflateMode.Inflate;

        /// <summary>
        /// Sets sequence neighbor count.
        /// </summary>
        public void SetSequenceNeighborCount(int value)
        {
            SequenceNeighborCount = Mathf.Max(1, value);
        }

        /// <summary>
        /// Sets surface neighbor count.
        /// </summary>
        public void SetSurfaceNeighborCount(int value)
        {
            SurfaceNeighborCount = Mathf.Max(2, value);
        }

        /// <summary>
        /// Sets center falloff sigma.
        /// </summary>
        public void SetCenterSigma(float value)
        {
            CenterSigma = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Sets inflate/deflate strength.
        /// </summary>
        public void SetInflateStrength(float value)
        {
            InflateStrength = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Sets inflate/deflate mode.
        /// </summary>
        public void SetInflateMode(int value)
        {
            InflateMode = value <= 0 ? InflateDeflateMode.Inflate : InflateDeflateMode.Deflate;
        }
    }
}
