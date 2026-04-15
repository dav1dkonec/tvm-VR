using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Sequence
{
    public sealed class InflateDeflateRuntimeConfiguration
    {
        public float Radius { get; set; } = 0.08f;
        public float Strength { get; set; } = 0.02f;
        public InflateDeflateMode Mode { get; set; } = InflateDeflateMode.Inflate;
    }
}
