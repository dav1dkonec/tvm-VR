using TvmVr2.Core.Common;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Executes the basic translate method.
    /// </summary>
    public sealed class BasicTranslateMethod
    {
        private readonly BasicTranslatePipeline _pipeline;

        /// <summary>
        /// Creates basic translate method.
        /// </summary>
        public BasicTranslateMethod(BasicTranslatePipeline pipeline = null)
        {
            _pipeline = pipeline ?? new BasicTranslatePipeline();
        }

        /// <summary>
        /// Applies basic translate input.
        /// </summary>
        public MethodExecutionResult Execute(BasicTranslateMethodInput input)
        {
            if (input == null)
            {
                return MethodExecutionResult.Failed("BasicTranslate input is missing.");
            }

            if (input.Frames == null)
            {
                return MethodExecutionResult.Failed("BasicTranslate runtime data are missing.");
            }

            var committed = _pipeline.ApplyCenterEdit(
                input.Frames,
                input.CenterIndex,
                input.FrameIndex,
                input.TargetPosition,
                input.CenterSigma,
                input.SequenceNeighborCount);

            if (!committed)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "BasicTranslate commit failed."
                };
            }

            return new MethodExecutionResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }
    }
}
