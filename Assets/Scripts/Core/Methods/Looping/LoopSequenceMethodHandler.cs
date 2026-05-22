using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.Looping
{
    /// <summary>
    /// Handler placeholder for sequence looping.
    /// </summary>
    public sealed class LoopSequenceMethodHandler : IEditingMethodHandler
    {
        /// <summary>
        /// Supported editing method.
        /// </summary>
        public MethodKind SupportedMethod => MethodKind.LoopSequence;

        /// <summary>
        /// Executes sequence looping.
        /// </summary>
        public IMethodResult Execute(IMethodInput input)
        {
            return new MethodExecutionResult { Success = false };
        }
    }
}
