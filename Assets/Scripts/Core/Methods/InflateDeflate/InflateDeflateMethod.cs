using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateMethod
    {
        private readonly TvmEditingMasterInflateDeflateAdapter _adapter;

        public InflateDeflateMethod(TvmEditingMasterInflateDeflateAdapter adapter = null)
        {
            _adapter = adapter;
        }

        public MethodExecutionResult Execute(InflateDeflateMethodInput input)
        {
            if (input == null)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate input is missing.");
            }

            if (_adapter == null)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate adapter is not registered.");
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
