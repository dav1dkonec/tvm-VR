using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    /// <summary>
    /// Executes the inflate/deflate method.
    /// </summary>
    public sealed class InflateDeflateMethod
    {
        private readonly TvmEditingMasterInflateDeflateAdapter _adapter;

        /// <summary>
        /// Creates inflate/deflate method.
        /// </summary>
        public InflateDeflateMethod(TvmEditingMasterInflateDeflateAdapter adapter = null)
        {
            _adapter = adapter;
        }

        /// <summary>
        /// Applies inflate/deflate input.
        /// </summary>
        public MethodExecutionResult Execute(InflateDeflateMethodInput input)
        {
            if (input == null)
            {
                return MethodExecutionResult.Failed("InflateDeflate input is missing.");
            }

            if (_adapter == null)
            {
                return MethodExecutionResult.Failed("InflateDeflate adapter is not registered.");
            }

            return _adapter.Execute(input) as MethodExecutionResult
                ?? new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate returned an unsupported result type."
                };
        }
    }
}
