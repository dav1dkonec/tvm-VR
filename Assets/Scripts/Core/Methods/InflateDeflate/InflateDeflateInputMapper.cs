using System.Collections.Generic;
using System.Numerics;
using TvmVr2.Api.Enums;
using TvmVr2.Api.Requests;
using TvmVr2.Api.Sequence;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class InflateDeflateInputMapper
    {
        public InflateDeflateMethodInput Map(InflateDeflateRequest request, SequenceRuntimeContext runtimeContext)
        {
            var referencePoint = new Vector3(
                request.ReferencePoint.X,
                request.ReferencePoint.Y,
                request.ReferencePoint.Z);
            var resolvedEffectors = ResolveEffectors(
                runtimeContext?.Frames,
                request.FrameIndex,
                referencePoint,
                request.Radius,
                request.Strength,
                request.Mode);

            return new InflateDeflateMethodInput
            {
                Frames = runtimeContext?.Frames,
                FrameIndex = request.FrameIndex,
                ReferencePoint = referencePoint,
                Radius = request.Radius,
                Strength = request.Strength,
                Mode = request.Mode,
                SelectedCenterIndices = resolvedEffectors.Indices,
                CenterTranslations = resolvedEffectors.Translations
            };
        }

        private static InflateDeflateResolvedEffectors ResolveEffectors(
            Frame[] frames,
            int frameIndex,
            Vector3 referencePoint,
            float radius,
            float strength,
            InflateDeflateMode mode)
        {
            if (frames == null || frameIndex < 0 || frameIndex >= frames.Length)
                return new InflateDeflateResolvedEffectors();

            var frame = frames[frameIndex];
            if (frame?.centers == null || frame.centers.Length == 0 || radius <= 0f || strength <= 0f)
                return new InflateDeflateResolvedEffectors();

            var indices = new List<int>();
            var translations = new List<Vector3>();
            var signedStrength = mode == InflateDeflateMode.Inflate ? strength : -strength;

            for (var i = 0; i < frame.centers.Length; i++)
            {
                var center = frame.centers[i];
                var distance = Vector3.Distance(center, referencePoint);
                if (distance > radius)
                    continue;

                var radialDirection = center - referencePoint;
                if (radialDirection.LengthSquared() < 1e-8f)
                    radialDirection = Vector3.UnitY;
                else
                    radialDirection = Vector3.Normalize(radialDirection);

                var falloff = 1f - (distance / radius);
                var translation = radialDirection * (signedStrength * falloff);

                indices.Add(i);
                translations.Add(translation);
            }

            return new InflateDeflateResolvedEffectors
            {
                Indices = indices.ToArray(),
                Translations = translations.ToArray()
            };
        }
    }
}
