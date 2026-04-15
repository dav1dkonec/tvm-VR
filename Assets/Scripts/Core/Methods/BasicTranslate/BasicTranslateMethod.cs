using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class BasicTranslateMethod
    {
        private readonly BasicTranslatePipeline _pipeline;

        public BasicTranslateMethod(BasicTranslatePipeline pipeline = null)
        {
            _pipeline = pipeline ?? new BasicTranslatePipeline();
        }

        public MethodExecutionResult Execute(BasicTranslateMethodInput input)
        {
            if (input == null)
            {
                return MethodExecutionResult.NotImplemented("BasicTranslate input is missing.");
            }

            if (input.Frames == null)
            {
                return MethodExecutionResult.NotImplemented("BasicTranslate runtime data are missing.");
            }

            var committed = _pipeline.ApplyCenterEdit(
                input.Frames,
                input.CenterIndex,
                input.FrameIndex,
                input.TargetPosition,
                input.CenterSigma,
                input.SequenceNeighborCount);

            return new MethodExecutionResult
            {
                Success = committed,
                ErrorMessage = committed ? string.Empty : "BasicTranslate commit failed."
            };
        }
    }
}
