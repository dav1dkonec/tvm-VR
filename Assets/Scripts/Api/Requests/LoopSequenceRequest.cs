
using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    /// <summary>
    /// Request for looping a sequence.
    /// </summary>
    public sealed class LoopSequenceRequest : EditOperationRequest
    {
        /// <summary>
        /// Creates a loop sequence request.
        /// </summary>
        public LoopSequenceRequest()
            : base(MethodKind.LoopSequence)
        {
        }

        /// <summary>
        /// Loop transition start frame.
        /// </summary>
        public int StartFrameIndex { get; set; }

        /// <summary>
        /// Loop transition end frame.
        /// </summary>
        public int EndFrameIndex { get; set; }

        /// <summary>
        /// Whether boundary frames should stay fixed.
        /// </summary>
        public bool PreserveBoundaryFrames { get; set; } = true;
    }
}
