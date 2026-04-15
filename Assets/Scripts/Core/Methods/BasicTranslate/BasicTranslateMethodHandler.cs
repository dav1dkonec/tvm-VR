using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class BasicTranslateMethodHandler : IEditingMethodHandler
    {
        private readonly BasicTranslateMethod _method;

        public BasicTranslateMethodHandler()
            : this(new BasicTranslatePipeline())
        {
        }

        public BasicTranslateMethodHandler(BasicTranslatePipeline pipeline)
        {
            _method = new BasicTranslateMethod(pipeline);
        }

        public MethodKind SupportedMethod => MethodKind.BasicTranslate;

        public IMethodResult Execute(IMethodInput input)
        {
            if (input is not BasicTranslateMethodInput basicTranslateInput)
            {
                return MethodExecutionResult.NotImplemented("BasicTranslate received unsupported input.");
            }
            
            return _method.Execute(basicTranslateInput);
        }
    }
}
