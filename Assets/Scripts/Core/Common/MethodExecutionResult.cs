using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Common
{
    /// <summary>
    /// Default editing method execution result.
    /// </summary>
    public sealed class MethodExecutionResult : IMethodResult
    {
        /// <summary>
        /// Whether method execution succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message when execution failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Creates a failed method execution result.
        /// </summary>
        public static MethodExecutionResult Failed(string details)
        {
            return new MethodExecutionResult
            {
                Success = false,
                ErrorMessage = details
            };
        }
    }
}
