using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    /// <summary>
    /// Handler for inflate/deflate method.
    /// </summary>
    public sealed class InflateDeflateMethodHandler : IEditingMethodHandler
    {
        private readonly InflateDeflateMethod _method;

        /// <summary>
        /// Creates handler without an adapter.
        /// </summary>
        public InflateDeflateMethodHandler()
            : this(null)
        {
        }

        /// <summary>
        /// Creates handler with an adapter.
        /// </summary>
        public InflateDeflateMethodHandler(TvmEditingMasterInflateDeflateAdapter adapter)
        {
            _method = new InflateDeflateMethod(adapter);
        }

        /// <summary>
        /// Supported editing method.
        /// </summary>
        public MethodKind SupportedMethod => MethodKind.InflateDeflate;

        /// <summary>
        /// Executes inflate/deflate.
        /// </summary>
        public IMethodResult Execute(IMethodInput input)
        {
            if (input is not InflateDeflateMethodInput inflateDeflateInput)
            {
                return MethodExecutionResult.Failed("InflateDeflate received unsupported input.");
            }

            return _method.Execute(inflateDeflateInput);
        }
    }
}
