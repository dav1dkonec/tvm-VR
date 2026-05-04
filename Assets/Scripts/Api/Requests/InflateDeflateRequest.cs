using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    public sealed class InflateDeflateRequest : EditOperationRequest
    {
        public InflateDeflateRequest()
            : base(MethodKind.InflateDeflate)
        {
        }

        public int SelectedCenterIndex { get; set; } = -1;
        public float Radius { get; set; }
        public float Strength { get; set; }
        public InflateDeflateMode Mode { get; set; }
    }
}
