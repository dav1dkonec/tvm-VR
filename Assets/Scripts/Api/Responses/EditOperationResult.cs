using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Responses
{
    public sealed class EditOperationResult
    {
        public bool Succeeded { get; set; }
        public MethodKind Method { get; set; }
        public int FrameIndex { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
