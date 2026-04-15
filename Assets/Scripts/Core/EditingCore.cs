using System;
using TvmVr2.Api.Enums;
using TvmVr2.Api.Requests;
using TvmVr2.Api.Responses;
using TvmVr2.Api.Sequence;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Methods.BasicTranslate;
using TvmVr2.Core.Methods.InflateDeflate;

namespace TvmVr2.Core
{
    public sealed class EditingCore
    {
        private readonly EditingMethodDispatcher _dispatcher;
        private readonly BasicTranslateInputMapper _basicTranslateInputMapper;
        private readonly InflateDeflateInputMapper _inflateDeflateInputMapper;
        private readonly BasicTranslatePipeline _basicTranslatePipeline;

        public EditingCore(
            BasicTranslatePipeline basicTranslatePipeline = null,
            TvmEditingMasterInflateDeflateAdapter inflateDeflateAdapter = null)
        {
            _basicTranslatePipeline = basicTranslatePipeline ?? new BasicTranslatePipeline();
            _basicTranslateInputMapper = new BasicTranslateInputMapper();
            _inflateDeflateInputMapper = new InflateDeflateInputMapper();
            _dispatcher = new EditingMethodDispatcher(new IEditingMethodHandler[]
            {
                new BasicTranslateMethodHandler(_basicTranslatePipeline),
                new InflateDeflateMethodHandler(inflateDeflateAdapter ?? new TvmEditingMasterInflateDeflateAdapter())
            });
        }

        public ValidationResult Validate(EditOperationRequest request)
        {
            if (request == null)
                return ValidationResult.Invalid("Request is required.");

            if (string.IsNullOrWhiteSpace(request.SequenceId))
                return ValidationResult.Invalid("SequenceId is required.");

            if (request.FrameIndex < 0)
                return ValidationResult.Invalid("FrameIndex must be non-negative.");

            if (request is BasicTranslateRequest basicTranslateRequest)
            {
                if (basicTranslateRequest.CenterIndex < 0)
                    return ValidationResult.Invalid("CenterIndex must be non-negative.");

                if (float.IsNaN(basicTranslateRequest.TargetPosition.X) ||
                    float.IsNaN(basicTranslateRequest.TargetPosition.Y) ||
                    float.IsNaN(basicTranslateRequest.TargetPosition.Z))
                {
                    return ValidationResult.Invalid("TargetPosition must contain valid coordinates.");
                }
            }
            else if (request is InflateDeflateRequest inflateDeflateRequest)
            {
                if (inflateDeflateRequest.Radius <= 0f)
                    return ValidationResult.Invalid("Radius must be greater than zero.");

                if (inflateDeflateRequest.Strength <= 0f)
                    return ValidationResult.Invalid("Strength must be greater than zero.");

                if (float.IsNaN(inflateDeflateRequest.ReferencePoint.X) ||
                    float.IsNaN(inflateDeflateRequest.ReferencePoint.Y) ||
                    float.IsNaN(inflateDeflateRequest.ReferencePoint.Z))
                {
                    return ValidationResult.Invalid("ReferencePoint must contain valid coordinates.");
                }
            }

            return ValidationResult.Valid();
        }

        public EditOperationResult Execute(EditOperationRequest request, SequenceRuntimeContext runtimeContext)
        {
            var validation = Validate(request);
            if (!validation.IsValid)
            {
                return new EditOperationResult
                {
                    Succeeded = false,
                    Method = request?.MethodKind ?? MethodKind.BasicTranslate,
                    FrameIndex = request?.FrameIndex ?? -1,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            if (runtimeContext == null)
            {
                return new EditOperationResult
                {
                    Succeeded = false,
                    Method = request.MethodKind,
                    FrameIndex = request.FrameIndex,
                    ErrorMessage = "Runtime context is missing."
                };
            }

            var methodInput = MapRequest(request, runtimeContext);
            var methodResult = _dispatcher.Dispatch(request.MethodKind, methodInput);

            return new EditOperationResult
            {
                Succeeded = methodResult.Success,
                Method = request.MethodKind,
                FrameIndex = request.FrameIndex,
                ErrorMessage = methodResult.ErrorMessage
            };
        }

        public bool RebuildBasicTranslateSurface(Frame[] frames, int surfaceNeighborCount)
        {
            return _basicTranslatePipeline.RebuildSurface(frames, surfaceNeighborCount);
        }

        private IMethodInput MapRequest(EditOperationRequest request, SequenceRuntimeContext runtimeContext)
        {
            return request.MethodKind switch
            {
                MethodKind.BasicTranslate => _basicTranslateInputMapper.Map((BasicTranslateRequest)request, runtimeContext),
                MethodKind.InflateDeflate => _inflateDeflateInputMapper.Map((InflateDeflateRequest)request, runtimeContext),
                _ => throw new InvalidOperationException($"Unsupported method kind: {request.MethodKind}")
            };
        }
    }
}
