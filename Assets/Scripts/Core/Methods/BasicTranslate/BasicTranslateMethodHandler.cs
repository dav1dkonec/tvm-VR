using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Handler for basic translate method.
    /// </summary>
    public sealed class BasicTranslateMethodHandler : IEditingMethodHandler
    {
        private readonly BasicTranslateMethod _method;

        /// <summary>
        /// Creates handler with a default pipeline.
        /// </summary>
        public BasicTranslateMethodHandler()
            : this(new BasicTranslatePipeline())
        {
        }

        /// <summary>
        /// Creates handler with a provided pipeline.
        /// </summary>
        public BasicTranslateMethodHandler(BasicTranslatePipeline pipeline)
        {
            _method = new BasicTranslateMethod(pipeline);
        }

        /// <summary>
        /// Supported editing method.
        /// </summary>
        public MethodKind SupportedMethod => MethodKind.BasicTranslate;

        /// <summary>
        /// Executes basic translate.
        /// </summary>
        public IMethodResult Execute(IMethodInput input)
        {
            if (input is not BasicTranslateMethodInput basicTranslateInput)
            {
                return MethodExecutionResult.Failed("BasicTranslate received unsupported input.");
            }
            
            return _method.Execute(basicTranslateInput);
        }
    }
}
