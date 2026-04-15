using TvmVr2.Api.Common;
using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    public sealed class BasicTranslateRequest : EditOperationRequest
    {
        public BasicTranslateRequest()
            : base(MethodKind.BasicTranslate)
        {
        }

        public int CenterIndex { get; set; }
        public Point3Data TargetPosition { get; set; }
    }
}
