using TvmVr2.Api.Common;
using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    public sealed class InflateDeflateRequest : EditOperationRequest
    {
        public InflateDeflateRequest()
            : base(MethodKind.InflateDeflate)
        {
        }

        public Point3Data ReferencePoint { get; set; }
        public float Radius { get; set; }
        public float Strength { get; set; }
        public InflateDeflateMode Mode { get; set; }
    }
}
