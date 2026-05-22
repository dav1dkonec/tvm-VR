
using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    /// <summary>
    /// Base request for an editing operation.
    /// </summary>
    public abstract class EditOperationRequest
    {
        /// <summary>
        /// Creates an edit request for the given method.
        /// </summary>
        protected EditOperationRequest(MethodKind methodKind)
        {
            MethodKind = methodKind;
        }

        /// <summary>
        /// Requested editing method.
        /// </summary>
        public MethodKind MethodKind { get; }

        /// <summary>
        /// Edited sequence identifier.
        /// </summary>
        public string SequenceId { get; set; } = string.Empty;

        /// <summary>
        /// Edited frame index.
        /// </summary>
        public int FrameIndex { get; set; }
    }
}
