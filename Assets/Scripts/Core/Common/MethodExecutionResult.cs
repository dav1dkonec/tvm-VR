using TvmVr2.Core.Abstractions;

namespace TvmVr2.Core.Common
{
    public sealed class MethodExecutionResult : IMethodResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static MethodExecutionResult NotImplemented(string details)
        {
            return new MethodExecutionResult
            {
                Success = false,
                ErrorMessage = details
            };
        }
    }
}
