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

        public bool IsStreamingAssetsCacheEnabled => _useStreamingAssetsCache;

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

        public void InvalidateFrameCaches(int[] frameIndices)
        {
            if (frameIndices == null || frameIndices.Length == 0)
                return;

            lock (_contextLock)
            {
                if (_cachedExecutionContext == null)
                    return;

                for (var i = 0; i < frameIndices.Length; i++)
                {
                    var frameIndex = frameIndices[i];
                    _cachedExecutionContext.SurfaceDeformation?.InvalidateFrameCache(frameIndex);
                    _cachedExecutionContext.TransformPropagation?.InvalidateFrameCache(frameIndex);
                }

                UnityEngine.Debug.Log(
                    $"InflateDeflateCache: invalidated runtime frame caches for {frameIndices.Length} frame(s) " +
                    $"[{string.Join(", ", frameIndices)}].");
            }
        }

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
                Radius = 0f,
                Strength = 0f,
                Mode = InflateDeflateMode.Inflate
            };

            var cacheKey = BuildCacheKey(input);
            var resetTimer = Stopwatch.StartNew();

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

            resetTimer.Stop();
            UnityEngine.Debug.Log(
                $"InflateDeflateCache: execution context reset for sequence '{sequenceId}' in {resetTimer.Elapsed.TotalMilliseconds:F2} ms " +
                $"(hydrationState={cacheHydrationState}).");

            return string.Equals(cacheHydrationState, "hit", StringComparison.Ordinal);
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
            var centers = input.Frames.Select(frame => frame.centers).ToArray();
            var cacheFrames = ResolveCacheFrames(input);
            var cacheCenters = cacheFrames.Select(frame => frame.centers).ToArray();

            _operationGate.Wait();
            try
            {
                var executionContext = GetOrCreateExecutionContext(input, out var executionContextCacheHit, out var cacheHydrationState);
                profile.ExecutionContextCacheHit = executionContextCacheHit;
                profile.CacheHydrationState = cacheHydrationState;
                if (!executionContextCacheHit && !string.Equals(cacheHydrationState, "hit", StringComparison.Ordinal))
                    PrecomputeCache(executionContext, cacheFrames);

                EnsureAffinityAvailable(executionContext, cacheCenters, out var affinityWasAvailableBeforeEnsure, out var affinityCalculatedDuringRun);
                profile.AffinityWasAvailableBeforeEnsure = affinityWasAvailableBeforeEnsure;
                profile.AffinityCalculatedDuringRun = affinityCalculatedDuringRun;

                var resolvedEffectors = ResolveEffectorsFromAffinity(
                    input,
                    executionContext.AffinityCalculation?.GetCentersAffinity(),
                    out var plannerDiagnostics);
                input.SelectedCenterIndices = resolvedEffectors.Indices;
                input.CenterTranslations = resolvedEffectors.Translations;
                profile.AffectedCenterCount = input.SelectedCenterIndices.Length;
                profile.ActivePatchCount = plannerDiagnostics.ActivePatchCount;
                profile.TransitionRingCount = plannerDiagnostics.TransitionRingCount;
                profile.PreferredCandidateCount = plannerDiagnostics.PreferredCandidateCount;
                profile.FallbackCandidateCount = plannerDiagnostics.FallbackCandidateCount;
                profile.GuardRadius = plannerDiagnostics.GuardRadius;
                profile.PatchMinAffinity = plannerDiagnostics.PatchMinAffinity;
                profile.PatchMaxAffinity = plannerDiagnostics.PatchMaxAffinity;
                profile.TranslationMagnitudeMax = plannerDiagnostics.TranslationMagnitudeMax;
                profile.TranslationMagnitudeAverage = plannerDiagnostics.TranslationMagnitudeAverage;

                UnityEngine.Debug.Log(
                    $"InflateDeflatePlanner: selectedCenter={input.SelectedCenterIndex}, mode={input.Mode}, " +
                    $"affectedCenters={profile.AffectedCenterCount}, activePatch={profile.ActivePatchCount}, " +
                    $"transitionRing={profile.TransitionRingCount}, preferredCandidates={profile.PreferredCandidateCount}, " +
                    $"fallbackCandidates={profile.FallbackCandidateCount}, guardRadius={profile.GuardRadius:F4}, " +
                    $"patchMinAffinity={profile.PatchMinAffinity:F4}, patchMaxAffinity={profile.PatchMaxAffinity:F4}, " +
                    $"maxTranslation={profile.TranslationMagnitudeMax:F6}, avgTranslation={profile.TranslationMagnitudeAverage:F6}, " +
                    $"cacheContextHit={profile.ExecutionContextCacheHit}, cacheHydration={profile.CacheHydrationState}, " +
                    $"affinityCalculatedDuringRun={profile.AffinityCalculatedDuringRun}.");

                if (input.SelectedCenterIndices.Length == 0)
                {
                    profile.Success = false;
                    profile.ErrorMessage = "InflateDeflate affinity planner resolved no affected centers for the current request.";
                    return new MethodExecutionResult
                    {
                        Success = false,
                        ErrorMessage = profile.ErrorMessage
                    };
                }

                stageTimer.Restart();
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
                profile.DeformSurfacePropagatedCacheHits = deformProfile.SurfacePropagatedCacheHits;
                profile.DeformSurfacePropagatedMinCallTotalMs = deformProfile.SurfacePropagatedMinCallTotalMs;
                profile.DeformSurfacePropagatedMaxCallTotalMs = deformProfile.SurfacePropagatedMaxCallTotalMs;
                profile.DeformSurfacePropagatedMaxCallFrameIndex = deformProfile.SurfacePropagatedMaxCallFrameIndex;
                profile.DeformSurfacePropagatedAverageCallTotalMs = deformProfile.SurfacePropagatedAverageCallTotalMs;
                profile.TotalAffectedFrames = 1 + profile.DeformPropagatedSurfaceFrames;

                UnityEngine.Debug.Log(
                    $"InflateDeflateDeform: frame={input.FrameIndex}, affectedFrames={profile.TotalAffectedFrames}, " +
                    $"propagatedSurfaceFrames={profile.DeformPropagatedSurfaceFrames}, deformMs={profile.DeformMs:F2}, " +
                    $"affinityMs={profile.DeformAffinityMs:F2}, centerDeformationMs={profile.DeformCenterDeformationMs:F2}, " +
                    $"editedSurfaceMs={profile.DeformEditedSurfaceMs:F2}, propagateTransformsMs={profile.DeformPropagateTransformsMs:F2}, " +
                    $"propagateSurfaceMs={profile.DeformPropagateSurfaceMs:F2}, writeBackPending=true.");

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
            profile.TotalMs = profile.AdapterTotalMs;
            profile.Success = true;
            UnityEngine.Debug.Log(BuildDiagnosticsLog(input, profile));
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

            if (input.Radius <= 0f || input.Strength <= 0f)
                return MethodExecutionResult.NotImplemented("InflateDeflate radius and strength must be positive.");

            return null;
        }

        private static void EnsureAffinityAvailable(
            CachedExecutionContext executionContext,
            Vector3[][] centers,
            out bool affinityWasAvailableBeforeEnsure,
            out bool affinityCalculatedDuringRun)
        {
            affinityWasAvailableBeforeEnsure = false;
            affinityCalculatedDuringRun = false;

            if (executionContext?.AffinityCalculation == null || centers == null || centers.Length == 0)
                return;

            affinityWasAvailableBeforeEnsure = executionContext.AffinityCalculation.GetCentersAffinity() != null;
            if (executionContext.AffinityCalculation.GetCentersAffinity() == null)
            {
                executionContext.AffinityCalculation.CalculateCentersAffinity(centers);
                affinityCalculatedDuringRun = true;
            }
        }

        private static InflateDeflateResolvedEffectors ResolveEffectorsFromAffinity(
            InflateDeflateMethodInput input,
            float[,] affinity,
            out AffinityPlannerDiagnostics diagnostics)
        {
            diagnostics = default;

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
            var activeCount = ResolveActiveCountFromRadius(centers, selectedCenter, input.Radius, input.Mode);
            var transitionCount = ResolveTransitionCount(input.Mode, activeCount, centers.Length - 1);
            var guardRadius = input.Radius * (input.Mode == InflateDeflateMode.Deflate ? 1.75f : 1.5f);

            var candidates = BuildAffinityCandidates(centers, input.SelectedCenterIndex, affinity, selectedCenter);
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

            var preferred = new List<AffinityCandidate>(candidates.Count);
            var fallback = new List<AffinityCandidate>(candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance <= guardRadius)
                    preferred.Add(candidates[i]);
                else
                    fallback.Add(candidates[i]);
            }

            diagnostics.ActivePatchCount = activeCount;
            diagnostics.TransitionRingCount = transitionCount;
            diagnostics.PreferredCandidateCount = preferred.Count;
            diagnostics.FallbackCandidateCount = fallback.Count;
            diagnostics.GuardRadius = guardRadius;

            var ordered = new List<AffinityCandidate>(candidates.Count);
            ordered.AddRange(preferred);
            ordered.AddRange(fallback);

            activeCount = Math.Min(activeCount, ordered.Count);
            transitionCount = Math.Min(transitionCount, Math.Max(0, ordered.Count - activeCount));
            if (activeCount <= 0)
                return new InflateDeflateResolvedEffectors();

            var selectedCandidates = new List<AffinityCandidate>(1 + activeCount + transitionCount);
            for (var i = 0; i < activeCount + transitionCount; i++)
                selectedCandidates.Add(ordered[i]);

            var minAffinity = selectedCandidates.Min(candidate => candidate.Affinity);
            var maxAffinity = selectedCandidates.Max(candidate => candidate.Affinity);
            diagnostics.PatchMinAffinity = minAffinity;
            diagnostics.PatchMaxAffinity = maxAffinity;

            var expansionOrigin = ResolveExpansionOrigin(centers, selectedCenter, input.Radius);
            var activePatchRadius = ResolvePatchRadius(selectedCandidates, 0, activeCount, expansionOrigin);
            var supportPatchRadius = ResolvePatchRadius(selectedCandidates, activeCount, transitionCount, expansionOrigin);
            var directionSign = input.Mode == InflateDeflateMode.Inflate ? 1f : -1f;
            var indices = new List<int>(1 + selectedCandidates.Count);
            var translations = new List<Vector3>(1 + selectedCandidates.Count);

            var selectedTranslation = Vector3.Zero;
            var selectedOffset = selectedCenter - expansionOrigin;
            if (selectedOffset.LengthSquared() >= 1e-8f)
            {
                var selectedInfluence = input.Mode == InflateDeflateMode.Inflate
                    ? ComputeInflateSpatialInfluence(false, 0f)
                    : ComputeDeflateSpatialInfluence(false, 0f);
                selectedTranslation = selectedOffset * (directionSign * input.Strength * selectedInfluence);
            }

            indices.Add(input.SelectedCenterIndex);
            translations.Add(selectedTranslation);

            for (var i = 0; i < selectedCandidates.Count; i++)
            {
                var candidate = selectedCandidates[i];
                var translation = Vector3.Zero;
                var inSupportSubset = i >= activeCount;
                var offset = candidate.Position - expansionOrigin;
                if (offset.LengthSquared() >= 1e-8f)
                {
                    var distanceFromOrigin = offset.Length();
                    var supportDistance = ComputeSupportDistance(distanceFromOrigin, activePatchRadius, supportPatchRadius);
                    var influence = input.Mode == InflateDeflateMode.Inflate
                        ? ComputeInflateSpatialInfluence(inSupportSubset, supportDistance)
                        : ComputeDeflateSpatialInfluence(inSupportSubset, supportDistance);
                    translation = offset * (directionSign * input.Strength * influence);
                }

                indices.Add(candidate.Index);
                translations.Add(translation);
            }

            ResolveTranslationDiagnostics(translations, out _, out var originalAverageTranslationMagnitude);
            SmoothEffectorTranslations(indices, translations, affinity);
            PreserveAverageTranslationMagnitude(translations, originalAverageTranslationMagnitude);
            ResolveTranslationDiagnostics(translations, out diagnostics.TranslationMagnitudeMax, out diagnostics.TranslationMagnitudeAverage);

            return new InflateDeflateResolvedEffectors
            {
                Indices = indices.ToArray(),
                Translations = translations.ToArray()
            };
        }

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

        private static void SmoothEffectorTranslations(
            IReadOnlyList<int> indices,
            List<Vector3> translations,
            float[,] affinity)
        {
            const int smoothingIterations = 2;
            const float smoothingBlend = 0.35f;
            const float selfWeight = 0.35f;

            if (indices == null || translations == null || affinity == null || indices.Count != translations.Count)
                return;

            for (var iteration = 0; iteration < smoothingIterations; iteration++)
            {
                var source = translations.ToArray();
                for (var i = 0; i < source.Length; i++)
                {
                    var centerIndex = indices[i];
                    if (centerIndex < 0 || centerIndex >= affinity.GetLength(0))
                        continue;

                    var weightedSum = source[i] * selfWeight;
                    var weightSum = selfWeight;

                    for (var j = 0; j < source.Length; j++)
                    {
                        if (i == j)
                            continue;

                        var neighborIndex = indices[j];
                        if (neighborIndex < 0 || neighborIndex >= affinity.GetLength(1))
                            continue;

                        var weight = Math.Max(affinity[centerIndex, neighborIndex], 0f);
                        if (weight <= 1e-6f)
                            continue;

                        weightedSum += source[j] * weight;
                        weightSum += weight;
                    }

                    if (weightSum <= 1e-8f)
                        continue;

                    var smoothed = weightedSum / weightSum;
                    translations[i] = source[i] * (1f - smoothingBlend) + smoothed * smoothingBlend;
                }
            }
        }

        private static void ResolveTranslationDiagnostics(
            IReadOnlyList<Vector3> translations,
            out float translationMagnitudeMax,
            out float translationMagnitudeAverage)
        {
            var translationMagnitudeSum = 0f;
            var translationMagnitudeCount = 0;
            translationMagnitudeMax = 0f;

            if (translations == null)
            {
                translationMagnitudeAverage = 0f;
                return;
            }

            for (var i = 0; i < translations.Count; i++)
            {
                var translationMagnitude = translations[i].Length();
                if (translationMagnitude <= 1e-8f)
                    continue;

                translationMagnitudeSum += translationMagnitude;
                translationMagnitudeCount++;
                if (translationMagnitude > translationMagnitudeMax)
                    translationMagnitudeMax = translationMagnitude;
            }

            translationMagnitudeAverage = translationMagnitudeCount > 0
                ? translationMagnitudeSum / translationMagnitudeCount
                : 0f;
        }

        private static void PreserveAverageTranslationMagnitude(
            List<Vector3> translations,
            float targetAverageMagnitude)
        {
            if (translations == null || targetAverageMagnitude <= 1e-8f)
                return;

            ResolveTranslationDiagnostics(translations, out _, out var currentAverageMagnitude);
            if (currentAverageMagnitude <= 1e-8f)
                return;

            var scale = Math.Clamp(targetAverageMagnitude / currentAverageMagnitude, 0.75f, 1.6f);
            for (var i = 0; i < translations.Count; i++)
                translations[i] *= scale;
        }

        private static float ResolvePatchRadius(
            IReadOnlyList<AffinityCandidate> selectedCandidates,
            int startIndex,
            int count,
            Vector3 origin)
        {
            var endIndex = Math.Min(startIndex + count, selectedCandidates?.Count ?? 0);
            var radius = 0f;
            for (var i = Math.Max(0, startIndex); i < endIndex; i++)
            {
                var distance = Vector3.Distance(selectedCandidates[i].Position, origin);
                if (distance > radius)
                    radius = distance;
            }

            return radius;
        }

        private static float ComputeSupportDistance(
            float distanceFromOrigin,
            float activePatchRadius,
            float supportPatchRadius)
        {
            var supportSpan = supportPatchRadius - activePatchRadius;
            if (supportSpan <= 1e-8f)
                return 0f;

            return Math.Clamp((distanceFromOrigin - activePatchRadius) / supportSpan, 0f, 1f);
        }

        private static Vector3 ResolveExpansionOrigin(
            IReadOnlyList<Vector3> centers,
            Vector3 selectedCenter,
            float radius)
        {
            if (centers == null || centers.Count == 0 || radius <= 0f)
                return selectedCenter;

            var positionSum = Vector3.Zero;
            var count = 0;
            for (var i = 0; i < centers.Count; i++)
            {
                if (Vector3.Distance(centers[i], selectedCenter) > radius)
                    continue;

                positionSum += centers[i];
                count++;
            }

            return count > 0
                ? positionSum / count
                : selectedCenter;
        }

        private static int ResolveActiveCountFromRadius(
            Vector3[] centers,
            Vector3 selectedCenter,
            float radius,
            InflateDeflateMode mode)
        {
            var nearbyCount = 0;
            for (var i = 0; i < centers.Length; i++)
            {
                if (Vector3.Distance(centers[i], selectedCenter) <= radius)
                    nearbyCount++;
            }

            var effectorsInRadius = Math.Max(nearbyCount - 1, 0);
            if (effectorsInRadius == 0)
                return Math.Min(Math.Max(3, centers.Length - 1), centers.Length - 1);

            if (mode == InflateDeflateMode.Deflate)
            {
                var conservativeDeflateCount = Math.Max(3, (int)MathF.Ceiling(effectorsInRadius * 0.65f));
                return Math.Clamp(conservativeDeflateCount, 1, centers.Length - 1);
            }

            return Math.Clamp(effectorsInRadius, 1, centers.Length - 1);
        }

        private static int ResolveTransitionCount(InflateDeflateMode mode, int activeCount, int maxAvailable)
        {
            if (maxAvailable <= activeCount)
                return 0;

            if (mode == InflateDeflateMode.Inflate)
                return 0;

            var desired = Math.Max(2, (int)MathF.Ceiling(activeCount * 0.6f));
            return Math.Clamp(desired, 0, maxAvailable - activeCount);
        }

        private static float NormalizeAffinity(float affinity, float minAffinity, float maxAffinity)
        {
            var span = maxAffinity - minAffinity;
            if (span <= 1e-8f)
                return 1f;

            return Math.Clamp((affinity - minAffinity) / span, 0f, 1f);
        }

        private static float ComputeInflateSpatialInfluence(bool inTransitionRing, float normalizedSupportDistance)
        {
            return inTransitionRing
                ? 0.34f * (1f - normalizedSupportDistance) + 0.10f * normalizedSupportDistance
                : 2.55f;
        }

        private static float ComputeDeflateSpatialInfluence(bool inTransitionRing, float normalizedSupportDistance)
        {
            return inTransitionRing
                ? 0.16f * (1f - normalizedSupportDistance) + 0.06f * normalizedSupportDistance
                : 0.55f;
        }

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
                    UnityEngine.Debug.Log(
                        $"InflateDeflateCache: execution context cache hit for sequence '{input.SequenceId}' (key={cacheKey}).");
                    return _cachedExecutionContext;
                }

                UnityEngine.Debug.Log(
                    $"InflateDeflateCache: execution context cache miss for sequence '{input.SequenceId}' (key={cacheKey}); creating new context.");
                _cachedExecutionContext = CreateExecutionContext(cacheKey);
                TryHydrateExecutionContextFromStreamingAssets(input, _cachedExecutionContext, out var loadError, out cacheHydrationState);
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
                SelectedCenterIndex = -1,
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

        private void TryHydrateExecutionContextFromStreamingAssets(
            InflateDeflateMethodInput input,
            CachedExecutionContext context,
            out string errorMessage,
            out string hydrationState)
        {
            errorMessage = string.Empty;
            hydrationState = "not_attempted";

            if (!_useStreamingAssetsCache ||
                input?.Frames == null ||
                input.Frames.Length == 0 ||
                context == null ||
                string.IsNullOrWhiteSpace(input.SequenceId))
            {
                hydrationState = "skipped";
                UnityEngine.Debug.Log(
                    $"InflateDeflateCache: streaming-assets cache hydrate skipped for sequence '{input?.SequenceId ?? string.Empty}' " +
                    $"(enabled={_useStreamingAssetsCache}, hasFrames={input?.Frames != null && input.Frames.Length > 0}).");
                return;
            }

            var rootPath = UnityEngine.Application.streamingAssetsPath;
            var hydrateTimer = Stopwatch.StartNew();
            if (!InflateDeflateCacheStore.TryLoadBundle(rootPath, input.SequenceId, out var bundle, out var loadError))
            {
                hydrateTimer.Stop();
                if (!string.IsNullOrWhiteSpace(loadError) && !loadError.Contains("was not found"))
                    errorMessage = loadError;
                hydrationState = "miss";

                UnityEngine.Debug.Log(
                    $"InflateDeflateCache: cache miss for sequence '{input.SequenceId}' after {hydrateTimer.Elapsed.TotalMilliseconds:F2} ms. " +
                    $"{loadError}");
                return;
            }

            var manifest = BuildCacheManifest(input, context);
            if (!bundle.Manifest.IsCompatibleWith(manifest))
            {
                hydrateTimer.Stop();
                errorMessage = $"InflateDeflate cache manifest mismatch for sequence '{input.SequenceId}'.";
                hydrationState = "manifest_mismatch";
                UnityEngine.Debug.LogWarning(
                    $"InflateDeflateCache: manifest mismatch for sequence '{input.SequenceId}' after {hydrateTimer.Elapsed.TotalMilliseconds:F2} ms. " +
                    $"expected=[schema={manifest.SchemaVersion}, frames={manifest.FrameCount}, centers={manifest.CenterCount}, vertices={manifest.VertexCount}, faces={manifest.FaceCount}, neighbors={manifest.Neighbors}, shape={manifest.Shape}, epsilon={manifest.LimitEpsilon}, split={manifest.MaxSplitIterations}] " +
                    $"actual=[schema={bundle.Manifest.SchemaVersion}, frames={bundle.Manifest.FrameCount}, centers={bundle.Manifest.CenterCount}, vertices={bundle.Manifest.VertexCount}, faces={bundle.Manifest.FaceCount}, neighbors={bundle.Manifest.Neighbors}, shape={bundle.Manifest.Shape}, epsilon={bundle.Manifest.LimitEpsilon}, split={bundle.Manifest.MaxSplitIterations}].");
                return;
            }

            HydrateExecutionContext(context, bundle);
            hydrateTimer.Stop();
            hydrationState = "hit";
            UnityEngine.Debug.Log(
                $"InflateDeflateCache: cache hit for sequence '{input.SequenceId}' hydrated in {hydrateTimer.Elapsed.TotalMilliseconds:F2} ms " +
                $"(frames={bundle.Manifest.FrameCount}, centers={bundle.Manifest.CenterCount}, vertices={bundle.Manifest.VertexCount}, faces={bundle.Manifest.FaceCount}).");
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

        private static Frame[] ResolveCacheFrames(InflateDeflateMethodInput input)
        {
            return input?.CacheFrames != null && input.CacheFrames.Length > 0
                ? input.CacheFrames
                : input?.Frames ?? Array.Empty<Frame>();
        }

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
            public string CacheKey { get; set; }
            public MeshEditor MeshEditor { get; set; }
            public DistanceDirectionAffinityCalculation AffinityCalculation { get; set; }
            public CustomSurfaceDeformation SurfaceDeformation { get; set; }
            public KabschTransformPropagation TransformPropagation { get; set; }
        }

        private struct AffinityPlannerDiagnostics
        {
            public int ActivePatchCount;
            public int TransitionRingCount;
            public int PreferredCandidateCount;
            public int FallbackCandidateCount;
            public float GuardRadius;
            public float PatchMinAffinity;
            public float PatchMaxAffinity;
            public float TranslationMagnitudeMax;
            public float TranslationMagnitudeAverage;
        }

        private struct AffinityCandidate
        {
            public int Index;
            public Vector3 Position;
            public float Affinity;
            public float Distance;
        }

        private static string BuildDiagnosticsLog(InflateDeflateMethodInput input, InflateDeflateQuickProfile profile)
        {
            var builder = new StringBuilder();
            builder.AppendLine("InflateDeflate Diagnostics");
            builder.AppendLine($"Sequence: {input.SequenceId}");
            builder.AppendLine($"Frame: {input.FrameIndex}");
            builder.AppendLine($"SelectedCenter: {input.SelectedCenterIndex}");
            builder.AppendLine($"Mode: {input.Mode}");
            builder.AppendLine($"Radius: {input.Radius:F4}");
            builder.AppendLine($"Strength: {input.Strength:F4}");
            builder.AppendLine($"Cache.ContextHit: {profile.ExecutionContextCacheHit}");
            builder.AppendLine($"Cache.HydrationState: {profile.CacheHydrationState}");
            builder.AppendLine($"Cache.AffinityAvailableBeforeEnsure: {profile.AffinityWasAvailableBeforeEnsure}");
            builder.AppendLine($"Cache.AffinityCalculatedDuringRun: {profile.AffinityCalculatedDuringRun}");
            builder.AppendLine($"Planner.AffectedCenters: {profile.AffectedCenterCount}");
            builder.AppendLine($"Planner.ActivePatch: {profile.ActivePatchCount}");
            builder.AppendLine($"Planner.TransitionRing: {profile.TransitionRingCount}");
            builder.AppendLine($"Planner.PreferredCandidates: {profile.PreferredCandidateCount}");
            builder.AppendLine($"Planner.FallbackCandidates: {profile.FallbackCandidateCount}");
            builder.AppendLine($"Planner.GuardRadius: {profile.GuardRadius:F4}");
            builder.AppendLine($"Planner.PatchMinAffinity: {profile.PatchMinAffinity:F4}");
            builder.AppendLine($"Planner.PatchMaxAffinity: {profile.PatchMaxAffinity:F4}");
            builder.AppendLine($"Planner.TranslationMagnitudeMax: {profile.TranslationMagnitudeMax:F6}");
            builder.AppendLine($"Planner.TranslationMagnitudeAverage: {profile.TranslationMagnitudeAverage:F6}");
            builder.AppendLine($"Frames.PropagatedSurface: {profile.DeformPropagatedSurfaceFrames}");
            builder.AppendLine($"Frames.TotalAffected: {profile.TotalAffectedFrames}");
            builder.AppendLine($"Timing.ResolveEffectors: {profile.ResolveEffectorsMs:F2} ms");
            builder.AppendLine($"Timing.PrepareSequence: {profile.PrepareSequenceMs:F2} ms");
            builder.AppendLine($"Timing.PrepareTransforms: {profile.PrepareTransformsMs:F2} ms");
            builder.AppendLine($"Timing.Deform: {profile.DeformMs:F2} ms");
            builder.AppendLine($"Timing.Deform.Affinity: {profile.DeformAffinityMs:F2} ms");
            builder.AppendLine($"Timing.Deform.CenterDeformation: {profile.DeformCenterDeformationMs:F2} ms");
            builder.AppendLine($"Timing.Deform.EditedSurface: {profile.DeformEditedSurfaceMs:F2} ms");
            builder.AppendLine($"Timing.Deform.PropagateTransforms: {profile.DeformPropagateTransformsMs:F2} ms");
            builder.AppendLine($"Timing.Deform.PropagateSurface: {profile.DeformPropagateSurfaceMs:F2} ms");
            builder.AppendLine($"Timing.WriteBack: {profile.WriteBackMs:F2} ms");
            builder.AppendLine($"Timing.AdapterTotal: {profile.AdapterTotalMs:F2} ms");
            builder.AppendLine($"Timing.Total: {profile.TotalMs:F2} ms");
            return builder.ToString();
        }
    }
}
