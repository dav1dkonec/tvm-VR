using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateMethodHandler : IEditingMethodHandler
    {
        private readonly InflateDeflateMethod _method;

        public InflateDeflateMethodHandler()
            : this(null)
        {
        }

        public InflateDeflateMethodHandler(TvmEditingMasterInflateDeflateAdapter adapter)
        {
            _method = new InflateDeflateMethod(adapter);
        }

        public MethodKind SupportedMethod => MethodKind.InflateDeflate;

        public IMethodResult Execute(IMethodInput input)
        {
            if (input is not InflateDeflateMethodInput inflateDeflateInput)
            {
                return MethodExecutionResult.NotImplemented("InflateDeflate received unsupported input.");
            }

            return _method.Execute(inflateDeflateInput);
        }
    }
}
