using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    public abstract class EditOperationRequest
    {
        protected EditOperationRequest(MethodKind methodKind)
        {
            MethodKind = methodKind;
        }

        public MethodKind MethodKind { get; }
        public string SequenceId { get; set; } = string.Empty;
        public int FrameIndex { get; set; }
    }
}
