using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Numerics;
using System.Threading;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Methods.InflateDeflate.Cache;
using TVMEditor.Editing;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Editing.CenterDeformation;
using TVMEditor.Editing.SurfaceDeformation;
using TVMEditor.Editing.TransformPropagation;
using TVMEditor.Structures;

namespace TvmVr2.Core.Methods.InflateDeflate
{
    /// <summary>
    /// Adapts tvm-editing-master inflate/deflate computation to the application runtime data model.
    /// </summary>
    public sealed class TvmEditingMasterInflateDeflateAdapter
    {
        private readonly object _contextLock = new object();
        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private CachedExecutionContext _cachedExecutionContext;

        /// <summary>
        /// Executes inflate/deflate on runtime frames using cached data built from the original sequence.
        /// </summary>
        public IMethodResult Execute(InflateDeflateMethodInput input)
        {
            return ExecuteInternal(input);
        }

        /// <summary>
        /// Recreates the execution context and hydrates it from precomputed StreamingAssets cache.
        /// </summary>
        public bool ResetExecutionContextToDefaultCache(
            string sequenceId,
            Frame[] frames,
            out string cacheHydrationState,
            out string errorMessage)
        {
            cacheHydrationState = "not_attempted";
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(sequenceId))
            {
                errorMessage = "InflateDeflate cache reset requires a sequence id.";
                return false;
            }

            if (frames == null || frames.Length == 0)
            {
                errorMessage = "InflateDeflate cache reset requires loaded frames.";
                return false;
            }

            var input = new InflateDeflateMethodInput
            {
                SequenceId = sequenceId,
                Frames = frames,
                FrameIndex = 0,
                SelectedCenterIndex = -1,
                Strength = 0f,
                Mode = InflateDeflateMode.Inflate
            };

            var cacheKey = BuildCacheKey(input);
            _operationGate.Wait();
            try
            {
                lock (_contextLock)
                {
                    _cachedExecutionContext = CreateExecutionContext(cacheKey);
                    TryHydrateExecutionContextFromStreamingAssets(
                        input,
                        _cachedExecutionContext,
                        out errorMessage,
                        out cacheHydrationState);
                }
            }
            finally
            {
                _operationGate.Release();
            }

            if (!string.Equals(cacheHydrationState, "hit", StringComparison.Ordinal))
            {
                UnityEngine.Debug.LogWarning(
                    $"InflateDeflateCache: precomputed cache was not hydrated for sequence '{sequenceId}' " +
                    $"(hydrationState={cacheHydrationState}).");
            }

            return string.Equals(cacheHydrationState, "hit", StringComparison.Ordinal);
        }

        /// <summary>
        /// Converts runtime frames to tvm-editing-master structures, resolves effectors and writes deformed data back.
        /// </summary>
        private MethodExecutionResult ExecuteInternal(InflateDeflateMethodInput input)
        {
            var validationResult = ValidateInput(input);
            if (validationResult != null)
                return validationResult;

            var sequence = new TriangleMeshSequence
            {
                Meshes = input.Frames.Select(frame => new TriangleMesh
                {
                    Vertices = frame.vertices,
                    Faces = frame.faces.Select(face => new TVMEditor.Structures.Face(face.V1, face.V2, face.V3)).ToArray()
                }).ToArray()
            };

            var centers = input.Frames.Select(frame => frame.centers).ToArray();
            var cacheFrames = ResolveCacheFrames(input);
            var cacheCenters = cacheFrames.Select(frame => frame.centers).ToArray();

            _operationGate.Wait();
            try
            {
                var executionContext = GetOrCreateExecutionContext(input, out var executionContextCacheHit, out var cacheHydrationState);
                if (!executionContextCacheHit && !string.Equals(cacheHydrationState, "hit", StringComparison.Ordinal))
                {
                    return new MethodExecutionResult
                    {
                        Success = false,
                        ErrorMessage = $"InflateDeflate precomputed cache is missing or incompatible for sequence '{input.SequenceId}'. Build cache from Tools before running the edit."
                    };
                }

                EnsureAffinityAvailable(executionContext, cacheCenters);

                var resolvedEffectors = ResolveEffectorsFromAffinity(
                    input,
                    executionContext.AffinityCalculation?.GetCentersAffinity());
                input.SelectedCenterIndices = resolvedEffectors.Indices;
                input.CenterTranslations = resolvedEffectors.Translations;

                if (input.SelectedCenterIndices.Length == 0)
                {
                    return new MethodExecutionResult
                    {
                        Success = false,
                        ErrorMessage = "InflateDeflate affinity planner resolved no affected centers for the current request."
                    };
                }

                var transformations = Enumerable
                    .Repeat(DualQuaternion.Identity(), centers[input.FrameIndex].Length)
                    .ToArray();

                for (var i = 0; i < input.SelectedCenterIndices.Length; i++)
                {
                    var centerIndex = input.SelectedCenterIndices[i];
                    if (centerIndex < 0 || centerIndex >= transformations.Length)
                    {
                        return new MethodExecutionResult
                        {
                            Success = false,
                            ErrorMessage = $"InflateDeflate selected center index {centerIndex} is outside valid range 0..{transformations.Length - 1}."
                        };
                    }

                    var translation = input.CenterTranslations[i];
                    transformations[centerIndex] = DualQuaternion.Translation(new Vector3(translation.X, translation.Y, translation.Z));
                }

                executionContext.MeshEditor.Deform(
                    sequence,
                    centers,
                    input.SelectedCenterIndices,
                    transformations,
                    input.FrameIndex,
                    out var deformedSequence,
                    out var deformedCenters);
                for (var i = 0; i < input.Frames.Length; i++)
                {
                    input.Frames[i].centers = deformedCenters[i];
                    input.Frames[i].vertices = deformedSequence.Meshes[i].Vertices;
                }
            }
            finally
            {
                _operationGate.Release();
            }

            return new MethodExecutionResult
            {
                Success = true,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Validates user-controlled input before any cache or deformation work starts.
        /// </summary>
        private static MethodExecutionResult ValidateInput(InflateDeflateMethodInput input)
        {
            if (input == null)
                return MethodExecutionResult.Failed("InflateDeflate input is missing.");

            if (input.Frames == null || input.Frames.Length == 0)
                return MethodExecutionResult.Failed("InflateDeflate runtime data are missing.");

            if (input.FrameIndex < 0 || input.FrameIndex >= input.Frames.Length)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate frame index is outside loaded frame range."
                };
            }

            var centers = input.Frames[input.FrameIndex]?.centers;
            if (centers == null || centers.Length == 0)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = "InflateDeflate frame centers are missing."
                };
            }

            if (input.SelectedCenterIndex < 0 || input.SelectedCenterIndex >= centers.Length)
            {
                return new MethodExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"InflateDeflate selected center index {input.SelectedCenterIndex} is outside valid range 0..{centers.Length - 1}."
                };
            }

            if (input.Strength <= 0f)
                return MethodExecutionResult.Failed("InflateDeflate strength must be positive.");

            return null;
        }

        /// <summary>
        /// Ensures the affinity matrix is present; cache should normally provide it.
        /// </summary>
        private static void EnsureAffinityAvailable(
            CachedExecutionContext executionContext,
            Vector3[][] centers)
        {
            if (executionContext?.AffinityCalculation == null || centers == null || centers.Length == 0)
                return;

            if (executionContext.AffinityCalculation.GetCentersAffinity() == null)
                executionContext.AffinityCalculation.CalculateCentersAffinity(centers);
        }

        /// <summary>
        /// Builds moving and fixed effectors from affinity to the selected reference center.
        /// </summary>
        private static InflateDeflateResolvedEffectors ResolveEffectorsFromAffinity(
            InflateDeflateMethodInput input,
            float[,] affinity)
        {
            const int minMovingEffectorCount = 16;
            const int maxMovingEffectorCount = 256;
            const int targetFixedEffectorCount = 32;
            const int fixedEffectorPoolSize = 160;
            const float minCandidateAffinity = 1e-6f;
            const float minRelativeMovingAffinity = 0.38f;
            const float minMovingInfluence = 0.55f;
            const float inflateTranslationScale = 1.35f;
            const float deflateTranslationScale = 0.65f;

            if (input?.Frames == null ||
                input.FrameIndex < 0 ||
                input.FrameIndex >= input.Frames.Length ||
                affinity == null)
            {
                return new InflateDeflateResolvedEffectors();
            }

            var frame = input.Frames[input.FrameIndex];
            var centers = frame?.centers;
            if (centers == null ||
                centers.Length == 0 ||
                input.SelectedCenterIndex < 0 ||
                input.SelectedCenterIndex >= centers.Length ||
                affinity.GetLength(0) != centers.Length ||
                affinity.GetLength(1) != centers.Length)
            {
                return new InflateDeflateResolvedEffectors();
            }

            var selectedCenter = centers[input.SelectedCenterIndex];
            var candidates = BuildAffinityCandidates(centers, input.SelectedCenterIndex, affinity, selectedCenter);
            candidates.RemoveAll(static candidate => !float.IsFinite(candidate.Affinity) || candidate.Affinity <= minCandidateAffinity);
            if (candidates.Count == 0)
                return new InflateDeflateResolvedEffectors();

            candidates.Sort(static (a, b) =>
            {
                var affinityComparison = b.Affinity.CompareTo(a.Affinity);
                if (affinityComparison != 0)
                    return affinityComparison;

                var distanceComparison = a.Distance.CompareTo(b.Distance);
                if (distanceComparison != 0)
                    return distanceComparison;

                return a.Index.CompareTo(b.Index);
            });

            var maxCandidateAffinity = candidates[0].Affinity;
            var minMovingAffinity = Math.Max(minCandidateAffinity, maxCandidateAffinity * minRelativeMovingAffinity);
            var movingCandidates = candidates
                .Where(candidate => candidate.Affinity >= minMovingAffinity)
                .Take(maxMovingEffectorCount)
                .ToList();

            if (movingCandidates.Count < Math.Min(minMovingEffectorCount, candidates.Count))
            {
                movingCandidates = candidates
                    .Take(Math.Min(minMovingEffectorCount, candidates.Count))
                    .ToList();
            }

            var fixedCandidates = SelectFixedEffectors(
                candidates,
                movingCandidates.Count,
                targetFixedEffectorCount,
                fixedEffectorPoolSize);

            var expansionOrigin = ResolveAffinityCentroid(selectedCenter, movingCandidates);

            if (movingCandidates.Count == 0)
                return new InflateDeflateResolvedEffectors();

            var minAffinity = movingCandidates.Min(candidate => candidate.Affinity);
            var maxAffinity = movingCandidates.Max(candidate => candidate.Affinity);

            var directionSign = input.Mode == InflateDeflateMode.Inflate ? 1f : -1f;
            var translationScale = input.Mode == InflateDeflateMode.Inflate
                ? inflateTranslationScale
                : deflateTranslationScale;
            var localScale = ResolveAverageDistanceFromOrigin(movingCandidates, expansionOrigin);
            var indices = new List<int>(movingCandidates.Count + fixedCandidates.Count);
            var translations = new List<Vector3>(movingCandidates.Count + fixedCandidates.Count);

            var translationMagnitude = input.Strength * localScale * translationScale;

            for (var i = 0; i < movingCandidates.Count; i++)
            {
                var candidate = movingCandidates[i];
                var offset = candidate.Position - expansionOrigin;

                var normalizedAffinity = Normalize(candidate.Affinity, minAffinity, maxAffinity);
                var influence = minMovingInfluence + (1f - minMovingInfluence) * SmoothStep(normalizedAffinity);
                var translation = ResolveRadialTranslation(offset, directionSign, translationMagnitude, influence);

                indices.Add(candidate.Index);
                translations.Add(translation);
            }

            for (var i = 0; i < fixedCandidates.Count; i++)
            {
                indices.Add(fixedCandidates[i].Index);
                translations.Add(Vector3.Zero);
            }

            return new InflateDeflateResolvedEffectors
            {
                Indices = indices.ToArray(),
                Translations = translations.ToArray()
            };
        }

        /// <summary>
        /// Converts an effector offset from the expansion origin to an inflate or deflate translation.
        /// </summary>
        private static Vector3 ResolveRadialTranslation(
            Vector3 offset,
            float directionSign,
            float translationMagnitude,
            float influence)
        {
            return offset.LengthSquared() >= 1e-8f
                ? Vector3.Normalize(offset) * (directionSign * translationMagnitude * influence)
                : Vector3.Zero;
        }

        /// <summary>
        /// Computes the weighted expansion origin from selected center and moving candidates.
        /// </summary>
        private static Vector3 ResolveAffinityCentroid(
            Vector3 selectedCenter,
            IReadOnlyList<AffinityCandidate> candidates)
        {
            var weightedPositionSum = selectedCenter;
            var weightSum = 1f;

            if (candidates == null)
                return selectedCenter;

            for (var i = 0; i < candidates.Count; i++)
            {
                var weight = Math.Max(candidates[i].Affinity, 0f);
                if (weight <= 1e-8f)
                    continue;

                weightedPositionSum += candidates[i].Position * weight;
                weightSum += weight;
            }

            return weightSum > 1e-8f
                ? weightedPositionSum / weightSum
                : selectedCenter;
        }

        /// <summary>
        /// Normalizes a value to 0..1 while handling a collapsed range.
        /// </summary>
        private static float Normalize(float value, float min, float max)
        {
            var span = max - min;
            if (span <= 1e-8f)
                return 1f;

            return Math.Clamp((value - min) / span, 0f, 1f);
        }

        /// <summary>
        /// Smooths normalized affinity so translation influence changes gradually.
        /// </summary>
        private static float SmoothStep(float value)
        {
            var t = Math.Clamp(value, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// Selects lower-affinity zero-translation effectors that stabilize the deformation boundary.
        /// </summary>
        private static List<AffinityCandidate> SelectFixedEffectors(
            IReadOnlyList<AffinityCandidate> candidates,
            int startIndex,
            int targetCount,
            int poolSize)
        {
            var fixedCandidates = new List<AffinityCandidate>(Math.Max(0, targetCount));
            if (candidates == null || targetCount <= 0 || startIndex >= candidates.Count)
                return fixedCandidates;

            var available = candidates.Count - startIndex;
            var sampledPoolSize = Math.Min(Math.Max(targetCount, poolSize), available);
            var endExclusive = startIndex + sampledPoolSize;
            var sampleCount = Math.Min(targetCount, sampledPoolSize);
            if (sampleCount <= 0)
                return fixedCandidates;

            if (sampleCount == 1)
            {
                fixedCandidates.Add(candidates[startIndex]);
                return fixedCandidates;
            }

            for (var i = 0; i < sampleCount; i++)
            {
                var t = (float)i / (sampleCount - 1);
                var index = startIndex + (int)MathF.Round(t * (endExclusive - startIndex - 1));
                fixedCandidates.Add(candidates[index]);
            }

            return fixedCandidates;
        }

        /// <summary>
        /// Estimates local patch scale from average distance of moving effectors to the expansion origin.
        /// </summary>
        private static float ResolveAverageDistanceFromOrigin(
            IReadOnlyList<AffinityCandidate> candidates,
            Vector3 origin)
        {
            if (candidates == null || candidates.Count == 0)
                return 0f;

            var sum = 0f;
            var count = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                var distance = Vector3.Distance(candidates[i].Position, origin);
                if (distance <= 1e-8f)
                    continue;

                sum += distance;
                count++;
            }

            return count > 0
                ? sum / count
                : 0f;
        }

        /// <summary>
        /// Creates affinity candidates for all centers except the selected reference center.
        /// </summary>
        private static List<AffinityCandidate> BuildAffinityCandidates(
            Vector3[] centers,
            int selectedCenterIndex,
            float[,] affinity,
            Vector3 selectedCenter)
        {
            var candidates = new List<AffinityCandidate>(Math.Max(0, centers.Length - 1));
            for (var i = 0; i < centers.Length; i++)
            {
                if (i == selectedCenterIndex)
                    continue;

                candidates.Add(new AffinityCandidate
                {
                    Index = i,
                    Position = centers[i],
                    Affinity = affinity[selectedCenterIndex, i],
                    Distance = Vector3.Distance(centers[i], selectedCenter)
                });
            }

            return candidates;
        }

        /// <summary>
        /// Reuses a hydrated execution context when the sequence cache key has not changed.
        /// </summary>
        private CachedExecutionContext GetOrCreateExecutionContext(
            InflateDeflateMethodInput input,
            out bool cacheHit,
            out string cacheHydrationState)
        {
            var cacheKey = BuildCacheKey(input);
            cacheHit = false;
            cacheHydrationState = "not_attempted";

            lock (_contextLock)
            {
                if (_cachedExecutionContext != null && _cachedExecutionContext.CacheKey == cacheKey)
                {
                    cacheHit = true;
                    cacheHydrationState = "context_reuse";
                    return _cachedExecutionContext;
                }

                _cachedExecutionContext = CreateExecutionContext(cacheKey);
                TryHydrateExecutionContextFromStreamingAssets(input, _cachedExecutionContext, out var loadError, out cacheHydrationState);
                if (!string.IsNullOrWhiteSpace(loadError))
                    UnityEngine.Debug.LogWarning(loadError);

                return _cachedExecutionContext;
            }
        }

        /// <summary>
        /// Precomputes affinity, Kabsch and surface weights and writes them to StreamingAssets.
        /// </summary>
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
                SelectedCenterIndex = -1,
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

        /// <summary>
        /// Builds an in-memory cache key from sequence identity and frame dimensions.
        /// </summary>
        private static string BuildCacheKey(InflateDeflateMethodInput input)
        {
            var builder = new StringBuilder();
            var frames = ResolveCacheFrames(input);
            builder.Append(input.MethodKind);
            builder.Append('|');
            builder.Append(input.SequenceId ?? string.Empty);
            builder.Append('|');
            builder.Append(frames?.Length ?? 0);

            if (frames == null)
                return builder.ToString();

            for (var i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];
                builder.Append('|');
                builder.Append(frame?.centers?.Length ?? 0);
                builder.Append(':');
                builder.Append(frame?.vertices?.Length ?? 0);
                builder.Append(':');
                builder.Append(frame?.faces?.Length ?? 0);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Creates the tvm-editing-master object graph used by inflate/deflate.
        /// </summary>
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

        /// <summary>
        /// Loads precomputed cache bundle and imports it into the execution context.
        /// </summary>
        private void TryHydrateExecutionContextFromStreamingAssets(
            InflateDeflateMethodInput input,
            CachedExecutionContext context,
            out string errorMessage,
            out string hydrationState)
        {
            errorMessage = string.Empty;
            hydrationState = "not_attempted";

            if (input?.Frames == null ||
                input.Frames.Length == 0 ||
                context == null ||
                string.IsNullOrWhiteSpace(input.SequenceId))
            {
                hydrationState = "skipped";
                return;
            }

            var rootPath = UnityEngine.Application.streamingAssetsPath;
            if (!InflateDeflateCacheStore.TryLoadBundle(rootPath, input.SequenceId, out var bundle, out var loadError))
            {
                if (!string.IsNullOrWhiteSpace(loadError) && !loadError.Contains("was not found"))
                    errorMessage = loadError;
                hydrationState = "miss";

                UnityEngine.Debug.LogWarning(
                    $"InflateDeflateCache: precomputed cache was not found for sequence '{input.SequenceId}'. " +
                    $"{loadError}");
                return;
            }

            var manifest = BuildCacheManifest(input, context);
            if (!bundle.Manifest.IsCompatibleWith(manifest))
            {
                errorMessage = $"InflateDeflate cache manifest mismatch for sequence '{input.SequenceId}'.";
                hydrationState = "manifest_mismatch";
                UnityEngine.Debug.LogWarning(
                    $"InflateDeflateCache: manifest mismatch for sequence '{input.SequenceId}'. " +
                    $"expected=[schema={manifest.SchemaVersion}, frames={manifest.FrameCount}, centers={manifest.CenterCount}, vertices={manifest.VertexCount}, faces={manifest.FaceCount}, neighbors={manifest.Neighbors}, shape={manifest.Shape}, epsilon={manifest.LimitEpsilon}, split={manifest.MaxSplitIterations}] " +
                    $"actual=[schema={bundle.Manifest.SchemaVersion}, frames={bundle.Manifest.FrameCount}, centers={bundle.Manifest.CenterCount}, vertices={bundle.Manifest.VertexCount}, faces={bundle.Manifest.FaceCount}, neighbors={bundle.Manifest.Neighbors}, shape={bundle.Manifest.Shape}, epsilon={bundle.Manifest.LimitEpsilon}, split={bundle.Manifest.MaxSplitIterations}].");
                return;
            }

            HydrateExecutionContext(context, bundle);
            hydrationState = "hit";
        }

        /// <summary>
        /// Performs offline cache precomputation used by the editor cache builder.
        /// </summary>
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

        /// <summary>
        /// Copies cached affinity, transform propagation and surface weights into runtime objects.
        /// </summary>
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

        /// <summary>
        /// Returns original frames for cache-related data, falling back to runtime frames when needed.
        /// </summary>
        private static Frame[] ResolveCacheFrames(InflateDeflateMethodInput input)
        {
            return input?.CacheFrames != null && input.CacheFrames.Length > 0
                ? input.CacheFrames
                : input?.Frames ?? Array.Empty<Frame>();
        }

        /// <summary>
        /// Collects all precomputed cache data into a serializable bundle.
        /// </summary>
        private static InflateDeflateCacheBundle BuildCacheBundle(CachedExecutionContext context, InflateDeflateMethodInput input)
        {
            var bundle = new InflateDeflateCacheBundle
            {
                Manifest = BuildCacheManifest(input, context),
                Affinity = context.AffinityCalculation?.GetCentersAffinity(),
                NeighborIndices = context.TransformPropagation?.GetNeighborIndices()
            };

            var frames = ResolveCacheFrames(input);
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

        /// <summary>
        /// Builds metadata used to reject incompatible cache files.
        /// </summary>
        private static InflateDeflateCacheManifest BuildCacheManifest(InflateDeflateMethodInput input, CachedExecutionContext context)
        {
            var frames = ResolveCacheFrames(input);
            var firstFrame = frames != null && frames.Length > 0 ? frames[0] : null;
            return new InflateDeflateCacheManifest
            {
                SchemaVersion = InflateDeflateCacheManifest.CurrentSchemaVersion,
                SequenceId = input.SequenceId ?? string.Empty,
                FrameCount = frames?.Length ?? 0,
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
            /// <summary>
            /// Key describing the cached sequence data loaded into this context.
            /// </summary>
            public string CacheKey { get; set; }

            /// <summary>
            /// tvm-editing-master mesh editor instance.
            /// </summary>
            public MeshEditor MeshEditor { get; set; }

            /// <summary>
            /// Affinity calculator holding the cached center affinity matrix.
            /// </summary>
            public DistanceDirectionAffinityCalculation AffinityCalculation { get; set; }

            /// <summary>
            /// Surface deformer holding cached vertex weights.
            /// </summary>
            public CustomSurfaceDeformation SurfaceDeformation { get; set; }

            /// <summary>
            /// Transform propagator holding cached Kabsch neighbor weights.
            /// </summary>
            public KabschTransformPropagation TransformPropagation { get; set; }
        }

        private struct AffinityCandidate
        {
            /// <summary>
            /// Center index in the edited frame.
            /// </summary>
            public int Index;

            /// <summary>
            /// Center position in the edited frame.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// Affinity to the selected reference center.
            /// </summary>
            public float Affinity;

            /// <summary>
            /// Euclidean distance from the selected reference center.
            /// </summary>
            public float Distance;
        }

    }
}
