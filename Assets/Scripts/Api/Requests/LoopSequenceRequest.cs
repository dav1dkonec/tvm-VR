using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    public sealed class LoopSequenceRequest : EditOperationRequest
    {
        public LoopSequenceRequest()
            : base(MethodKind.LoopSequence)
        {
        }

        public int StartFrameIndex { get; set; }
        public int EndFrameIndex { get; set; }
        public bool PreserveBoundaryFrames { get; set; } = true;
    }
}
