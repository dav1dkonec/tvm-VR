using UnityEngine;
using TvmVr2.Api.Requests;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    public sealed class BasicTranslateInputMapper
    {
        public BasicTranslateMethodInput Map(BasicTranslateRequest request, SequenceRuntimeContext runtimeContext)
        {
            return new BasicTranslateMethodInput
            {
                Frames = runtimeContext.Frames,
                FrameIndex = request.FrameIndex,
                CenterIndex = request.CenterIndex,
                CenterSigma = runtimeContext.BasicTranslate?.CenterSigma ?? 1f,
                SequenceNeighborCount = runtimeContext.BasicTranslate?.SequenceNeighborCount ?? 4,
                SurfaceNeighborCount = runtimeContext.BasicTranslate?.SurfaceNeighborCount ?? 6,
                TargetPosition = new Vector3(
                    request.TargetPosition.X,
                    request.TargetPosition.Y,
                    request.TargetPosition.Z)
            };
        }
    }
}
