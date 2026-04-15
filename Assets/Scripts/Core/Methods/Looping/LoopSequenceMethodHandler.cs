using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.Looping
{
    public sealed class LoopSequenceMethodHandler : IEditingMethodHandler
    {
        public MethodKind SupportedMethod => MethodKind.LoopSequence;

        public IMethodResult Execute(IMethodInput input)
        {
            return MethodExecutionResult.NotImplemented("LoopSequence method has not been connected yet.");
        }
    }
}
