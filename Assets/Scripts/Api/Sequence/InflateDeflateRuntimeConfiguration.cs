using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Sequence
{
    /// <summary>
    /// Runtime configuration for inflate/deflate.
    /// </summary>
    public sealed class InflateDeflateRuntimeConfiguration
    {
        /// <summary>
        /// Deformation strength.
        /// </summary>
        public float Strength { get; set; } = 0.02f;

        /// <summary>
        /// Selected deformation mode.
        /// </summary>
        public InflateDeflateMode Mode { get; set; } = InflateDeflateMode.Inflate;
    }
}
