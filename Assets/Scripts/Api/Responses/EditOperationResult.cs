using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Responses
{
    /// <summary>
    /// Result of an editing operation.
    /// </summary>
    public sealed class EditOperationResult
    {
        /// <summary>
        /// Whether the operation succeeded.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Executed editing method.
        /// </summary>
        public MethodKind Method { get; set; }

        /// <summary>
        /// Edited frame index.
        /// </summary>
        public int FrameIndex { get; set; }

        /// <summary>
        /// Error message when the operation failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
