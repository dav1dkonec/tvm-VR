using System;
using System.Text;
using System.Linq;
using System.Numerics;
using System.Threading;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Methods.InflateDeflate.Cache;
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
        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private CachedExecutionContext _cachedExecutionContext;
        private bool _useStreamingAssetsCache = true;

        public void SetStreamingAssetsCacheEnabled(bool enabled)
        {
            lock (_contextLock)
            {
                if (_useStreamingAssetsCache == enabled)
                    return;

                _useStreamingAssetsCache = enabled;
                _cachedExecutionContext = null;
            }
        }

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

            _operationGate.Wait();
            try
            {
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
            }
            finally
            {
                _operationGate.Release();
            }

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

                _cachedExecutionContext = CreateExecutionContext(cacheKey);
                TryHydrateExecutionContextFromStreamingAssets(input, _cachedExecutionContext, out var loadError);
                if (!string.IsNullOrWhiteSpace(loadError))
                    UnityEngine.Debug.LogWarning(loadError);

                return _cachedExecutionContext;
            }
        }

        public bool ExportCacheToStreamingAssets(string sequenceId, Frame[] frames, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                errorMessage = "InflateDeflate cache export requires a sequence id.";
                return false;
            }

            if (frames == null || frames.Length == 0)
            {
                errorMessage = "InflateDeflate cache export requires loaded frames.";
                return false;
            }

            var exportInput = new InflateDeflateMethodInput
            {
                SequenceId = sequenceId,
                Frames = frames,
                FrameIndex = 0,
                ReferencePoint = Vector3.Zero,
                Radius = 0f,
                Strength = 0f,
                Mode = InflateDeflateMode.Inflate
            };

            var executionContext = CreateExecutionContext(BuildCacheKey(exportInput));
            PrecomputeCache(executionContext, frames);

            var bundle = BuildCacheBundle(executionContext, exportInput);
            var rootPath = UnityEngine.Application.streamingAssetsPath;
            if (!InflateDeflateCacheStore.TrySaveBundle(rootPath, bundle, out errorMessage))
                return false;

            lock (_contextLock)
            {
                if (_cachedExecutionContext != null && _cachedExecutionContext.CacheKey == executionContext.CacheKey)
                    HydrateExecutionContext(_cachedExecutionContext, bundle);
            }

            return true;
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

        private static CachedExecutionContext CreateExecutionContext(string cacheKey)
        {
            var affinityCalculation = new DistanceDirectionAffinityCalculation();
            var surfaceDeformation = new CustomSurfaceDeformation(affinityCalculation);
            var transformPropagation = new KabschTransformPropagation(affinityCalculation);
            var meshEditor = new MeshEditor(
                affinityCalculation,
                new AffinityCenterDeformation(affinityCalculation),
                null,
                surfaceDeformation,
                transformPropagation);

            return new CachedExecutionContext
            {
                CacheKey = cacheKey,
                MeshEditor = meshEditor,
                AffinityCalculation = affinityCalculation,
                SurfaceDeformation = surfaceDeformation,
                TransformPropagation = transformPropagation
            };
        }

        private void TryHydrateExecutionContextFromStreamingAssets(InflateDeflateMethodInput input, CachedExecutionContext context, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!_useStreamingAssetsCache ||
                input?.Frames == null ||
                input.Frames.Length == 0 ||
                context == null ||
                string.IsNullOrWhiteSpace(input.SequenceId))
                return;

            var rootPath = UnityEngine.Application.streamingAssetsPath;
            if (!InflateDeflateCacheStore.TryLoadBundle(rootPath, input.SequenceId, out var bundle, out var loadError))
            {
                if (!string.IsNullOrWhiteSpace(loadError) && !loadError.Contains("was not found"))
                    errorMessage = loadError;

                return;
            }

            var manifest = BuildCacheManifest(input, context);
            if (!bundle.Manifest.IsCompatibleWith(manifest))
            {
                errorMessage = $"InflateDeflate cache manifest mismatch for sequence '{input.SequenceId}'.";
                return;
            }

            HydrateExecutionContext(context, bundle);
        }

        private static void PrecomputeCache(CachedExecutionContext context, Frame[] frames)
        {
            if (context == null || frames == null || frames.Length == 0)
                return;

            var centers = frames.Select(frame => frame.centers).ToArray();

            if (context.AffinityCalculation != null)
                context.AffinityCalculation.CalculateCentersAffinity(centers);

            if (context.TransformPropagation != null)
            {
                context.TransformPropagation.ClearNeighborCaches();
                context.TransformPropagation.SetNeighborIndices(null);
                for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
                    context.TransformPropagation.PrecomputeNeighborWeights(frameIndex, centers[frameIndex]);
            }

            if (context.SurfaceDeformation != null)
            {
                context.SurfaceDeformation.ClearFrameWeightCaches();
                for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
                {
                    context.SurfaceDeformation.PrecomputeFrameWeightCache(
                        frames[frameIndex].vertices,
                        frames[frameIndex].centers,
                        frameIndex);
                }
            }
        }

        private static void HydrateExecutionContext(CachedExecutionContext context, InflateDeflateCacheBundle bundle)
        {
            if (context == null || bundle?.Manifest == null)
                return;

            if (context.AffinityCalculation != null && bundle.Affinity != null)
                context.AffinityCalculation.SetCentersAffinity(bundle.Affinity);

            if (context.TransformPropagation != null)
            {
                context.TransformPropagation.SetNeighborIndices(bundle.NeighborIndices);
                context.TransformPropagation.ClearNeighborCaches();
                foreach (var neighborCache in bundle.KabschFrameCaches.Values)
                    context.TransformPropagation.ImportNeighborCache(neighborCache);
            }

            if (context.SurfaceDeformation != null)
            {
                context.SurfaceDeformation.ClearFrameWeightCaches();
                foreach (var surfaceCache in bundle.SurfaceFrameCaches.Values)
                    context.SurfaceDeformation.ImportFrameWeightCache(surfaceCache);
            }
        }

        private static InflateDeflateCacheBundle BuildCacheBundle(CachedExecutionContext context, InflateDeflateMethodInput input)
        {
            var bundle = new InflateDeflateCacheBundle
            {
                Manifest = BuildCacheManifest(input, context),
                Affinity = context.AffinityCalculation?.GetCentersAffinity(),
                NeighborIndices = context.TransformPropagation?.GetNeighborIndices()
            };

            var frames = input.Frames ?? Array.Empty<Frame>();
            for (var frameIndex = 0; frameIndex < frames.Length; frameIndex++)
            {
                if (context.SurfaceDeformation != null &&
                    context.SurfaceDeformation.TryExportFrameWeightCache(frameIndex, out var surfaceCache))
                {
                    bundle.SurfaceFrameCaches[frameIndex] = surfaceCache;
                }

                if (context.TransformPropagation != null &&
                    context.TransformPropagation.TryExportNeighborCache(frameIndex, out var neighborCache))
                {
                    bundle.KabschFrameCaches[frameIndex] = neighborCache;
                }
            }

            return bundle;
        }

        private static InflateDeflateCacheManifest BuildCacheManifest(InflateDeflateMethodInput input, CachedExecutionContext context)
        {
            var firstFrame = input.Frames != null && input.Frames.Length > 0 ? input.Frames[0] : null;
            return new InflateDeflateCacheManifest
            {
                SchemaVersion = InflateDeflateCacheManifest.CurrentSchemaVersion,
                SequenceId = input.SequenceId ?? string.Empty,
                FrameCount = input.Frames?.Length ?? 0,
                CenterCount = firstFrame?.centers?.Length ?? 0,
                VertexCount = firstFrame?.vertices?.Length ?? 0,
                FaceCount = firstFrame?.faces?.Length ?? 0,
                Neighbors = context.SurfaceDeformation?.Neighbors ?? 0,
                Shape = context.SurfaceDeformation?.Shape ?? 0f,
                LimitEpsilon = context.SurfaceDeformation?.LimitEpsilon ?? 0f,
                MaxSplitIterations = context.SurfaceDeformation?.MaxSplitIterations ?? 0,
                AffinityShapeDistance = context.AffinityCalculation?.ShapeDistance ?? 0f,
                AffinityShapeDirection = context.AffinityCalculation?.ShapeDirection ?? 0f,
                AffinityPower = context.AffinityCalculation?.Power ?? 0,
                SourceHash = string.Empty,
                GeneratedAtUtcTicks = DateTime.UtcNow.Ticks
            };
        }

        private sealed class CachedExecutionContext
        {
            public string CacheKey { get; set; }
            public MeshEditor MeshEditor { get; set; }
            public DistanceDirectionAffinityCalculation AffinityCalculation { get; set; }
            public CustomSurfaceDeformation SurfaceDeformation { get; set; }
            public KabschTransformPropagation TransformPropagation { get; set; }
        }
    }
}
