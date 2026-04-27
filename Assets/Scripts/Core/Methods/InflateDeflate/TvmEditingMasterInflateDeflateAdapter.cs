using System.Text;
using System.Linq;
using System.Numerics;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;
using TvmVr2.Core.Methods.InflateDeflate.Profiling;
using TVMEditor.Editing;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Editing.CenterDeformation;
using TVMEditor.Editing.SurfaceDeformation;
using TVMEditor.Editing.TransformPropagation;
using TVMEditor.Structures;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    public sealed class TvmEditingMasterInflateDeflateAdapter
    {
        private readonly object _contextLock = new object();
        private CachedExecutionContext _cachedExecutionContext;

        public IMethodResult Execute(InflateDeflateMethodInput input)
        {
            return ExecuteInternal(input, out _);
        }

        public MethodExecutionResult ExecuteProfiled(InflateDeflateMethodInput input, out InflateDeflateQuickProfile profile)
        {
            return ExecuteInternal(input, out profile);
        }

        private MethodExecutionResult ExecuteInternal(InflateDeflateMethodInput input, out InflateDeflateQuickProfile profile)
        {
            profile = new InflateDeflateQuickProfile();

            var validationResult = ValidateInput(input);
            if (validationResult != null)
            {
                profile.Success = false;
                profile.ErrorMessage = validationResult.ErrorMessage;
                return validationResult;
            }

            profile.AffectedCenterCount = input.SelectedCenterIndices.Length;

            TriangleMeshSequence sequence;
            UnityEngine.Debug.Log("InflateDeflateQuickProfiler: running test in phase PrepareSequence.");
            var stageTimer = Stopwatch.StartNew();
            sequence = new TriangleMeshSequence
            {
                Meshes = input.Frames.Select(frame => new TriangleMesh
                {
                    Vertices = frame.vertices,
                    Faces = frame.faces.Select(face => new TVMEditor.Structures.Face(face.V1, face.V2, face.V3)).ToArray()
                }).ToArray()
            };
            stageTimer.Stop();
            profile.PrepareSequenceMs = stageTimer.Elapsed.TotalMilliseconds;

            UnityEngine.Debug.Log("InflateDeflateQuickProfiler: running test in phase PrepareTransforms.");
            stageTimer.Restart();
            var centers = input.Frames.Select(frame => frame.centers).ToArray();
            var transformations = Enumerable
                .Repeat(DualQuaternion.Identity(), centers[input.FrameIndex].Length)
                .ToArray();

            for (var i = 0; i < input.SelectedCenterIndices.Length; i++)
            {
                var centerIndex = input.SelectedCenterIndices[i];
                if (centerIndex < 0 || centerIndex >= transformations.Length)
                {
                    profile.Success = false;
                    profile.ErrorMessage = $"InflateDeflate selected center index {centerIndex} is outside valid range 0..{transformations.Length - 1}.";
                    return new MethodExecutionResult
                    {
                        Success = false,
                        ErrorMessage = profile.ErrorMessage
                    };
                }

                var translation = input.CenterTranslations[i];
                transformations[centerIndex] = DualQuaternion.Translation(new Vector3(translation.X, translation.Y, translation.Z));
            }
            stageTimer.Stop();
            profile.PrepareTransformsMs = stageTimer.Elapsed.TotalMilliseconds;

            var executionContext = GetOrCreateExecutionContext(input);

            UnityEngine.Debug.Log("InflateDeflateQuickProfiler: running test in phase Deform.");
            stageTimer.Restart();
            executionContext.MeshEditor.Deform(
                sequence,
                centers,
                input.SelectedCenterIndices,
                transformations,
                input.FrameIndex,
                out var deformedSequence,
                out var deformedCenters,
                out var deformProfile);
            stageTimer.Stop();
            profile.DeformMs = stageTimer.Elapsed.TotalMilliseconds;
            profile.DeformCloneInputsMs = deformProfile.CloneInputsMs;
            profile.DeformResolveNewPositionsMs = deformProfile.ResolveNewPositionsMs;
            profile.DeformAffinityMs = deformProfile.AffinityMs;
            profile.DeformCenterDeformationMs = deformProfile.CenterDeformationMs;
            profile.DeformEditedSurfaceMs = deformProfile.EditedSurfaceMs;
            profile.DeformPropagateTransformsMs = deformProfile.PropagateTransformsMs;
            profile.DeformPropagateSurfaceMs = deformProfile.PropagateSurfaceMs;
            profile.DeformPropagateTotalMs = deformProfile.PropagateTotalMs;
            profile.DeformPropagatedSurfaceFrames = deformProfile.PropagatedSurfaceFrames;
            profile.DeformSurfaceEditedComputeWeightsMs = deformProfile.SurfaceEditedComputeWeightsMs;
            profile.DeformSurfaceEditedBlendVerticesMs = deformProfile.SurfaceEditedBlendVerticesMs;
            profile.DeformSurfaceEditedResampleMs = deformProfile.SurfaceEditedResampleMs;
            profile.DeformSurfaceEditedUsedCachedWeights = deformProfile.SurfaceEditedUsedCachedWeights;
            profile.DeformSurfacePropagatedComputeWeightsMs = deformProfile.SurfacePropagatedComputeWeightsMs;
            profile.DeformSurfacePropagatedBlendVerticesMs = deformProfile.SurfacePropagatedBlendVerticesMs;
            profile.DeformSurfacePropagatedResampleMs = deformProfile.SurfacePropagatedResampleMs;
            profile.DeformSurfacePropagatedCacheMisses = deformProfile.SurfacePropagatedCacheMisses;

            UnityEngine.Debug.Log("InflateDeflateQuickProfiler: running test in phase WriteBack.");
            stageTimer.Restart();
            for (var i = 0; i < input.Frames.Length; i++)
            {
                input.Frames[i].centers = deformedCenters[i];
                input.Frames[i].vertices = deformedSequence.Meshes[i].Vertices;
            }
            stageTimer.Stop();
            profile.WriteBackMs = stageTimer.Elapsed.TotalMilliseconds;

            profile.AdapterTotalMs = profile.PrepareSequenceMs
                + profile.PrepareTransformsMs
                + profile.DeformMs
                + profile.WriteBackMs;
            profile.Success = true;
            UnityEngine.Debug.Log("InflateDeflateQuickProfiler: adapter phases completed.");

            return new MethodExecutionResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        private static MethodExecutionResult ValidateInput(InflateDeflateMethodInput input)
        {
            if (input == null)
                return MethodExecutionResult.NotImplemented("InflateDeflate input is missing.");

            if (input.Frames == null || input.Frames.Length == 0)
                return MethodExecutionResult.NotImplemented("InflateDeflate runtime data are missing.");

            if (input.SelectedCenterIndices == null || input.CenterTranslations == null)
                return MethodExecutionResult.NotImplemented("InflateDeflate effectors were not resolved.");

            if (input.SelectedCenterIndices.Length != input.CenterTranslations.Length)
                return MethodExecutionResult.NotImplemented("InflateDeflate effectors are inconsistent.");

            if (input.FrameIndex < 0 || input.FrameIndex >= input.Frames.Length)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate frame index is outside loaded frame range."
                };
            }

            if (input.SelectedCenterIndices.Length == 0)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate resolved no affected centers for the current request."
                };
            }

            return null;
        }

        private CachedExecutionContext GetOrCreateExecutionContext(InflateDeflateMethodInput input)
        {
            var cacheKey = BuildCacheKey(input);

            lock (_contextLock)
            {
                if (_cachedExecutionContext != null && _cachedExecutionContext.CacheKey == cacheKey)
                    return _cachedExecutionContext;

                var affinityCalculation = new DistanceDirectionAffinityCalculation();
                var meshEditor = new MeshEditor(
                    affinityCalculation,
                    new AffinityCenterDeformation(affinityCalculation),
                    null,
                    new CustomSurfaceDeformation(affinityCalculation),
                    new KabschTransformPropagation(affinityCalculation));

                _cachedExecutionContext = new CachedExecutionContext
                {
                    CacheKey = cacheKey,
                    MeshEditor = meshEditor
                };

                return _cachedExecutionContext;
            }
        }

        private static string BuildCacheKey(InflateDeflateMethodInput input)
        {
            var builder = new StringBuilder();
            builder.Append(input.MethodKind);
            builder.Append('|');
            builder.Append(input.SequenceId ?? string.Empty);
            builder.Append('|');
            builder.Append(input.Frames?.Length ?? 0);

            if (input.Frames == null)
                return builder.ToString();

            for (var i = 0; i < input.Frames.Length; i++)
            {
                var frame = input.Frames[i];
                builder.Append('|');
                builder.Append(frame?.centers?.Length ?? 0);
                builder.Append(':');
                builder.Append(frame?.vertices?.Length ?? 0);
                builder.Append(':');
                builder.Append(frame?.faces?.Length ?? 0);
            }

            return builder.ToString();
        }

        private sealed class CachedExecutionContext
        {
            public string CacheKey { get; set; }
            public MeshEditor MeshEditor { get; set; }
        }
    }
}
