namespace TvmVr2.Core.Abstractions
{
    /// <summary>
    /// Result returned by an editing method.
    /// </summary>
    public interface IMethodResult
    {
        /// <summary>
        /// Whether method execution succeeded.
        /// </summary>
        bool Success { get; }

        /// <summary>
        /// Error message when execution failed.
        /// </summary>
        string ErrorMessage { get; }
    }
}
