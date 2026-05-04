using System.Collections.Generic;
using System.Text;

namespace TvmVr2.Core.Methods.InflateDeflate.Profiling
{
    public sealed class InflateDeflateQuickProfile
    {
        public int Iteration { get; set; }
        public int FrameIndex { get; set; }
        public int CenterIndex { get; set; }
        public int AffectedCenterCount { get; set; }
        public int MovingEffectorCount { get; set; }
        public int CandidatePoolCount { get; set; }
        public int DiscardedCandidateCount { get; set; }
        public float PatchMinAffinity { get; set; }
        public float PatchMaxAffinity { get; set; }
        public float ShapeElongation { get; set; }
        public float TranslationMagnitudeMax { get; set; }
        public float TranslationMagnitudeAverage { get; set; }
        public bool ExecutionContextCacheHit { get; set; }
        public string CacheHydrationState { get; set; } = string.Empty;
        public bool AffinityWasAvailableBeforeEnsure { get; set; }
        public bool AffinityCalculatedDuringRun { get; set; }
        public int TotalAffectedFrames { get; set; }
        public double ResolveEffectorsMs { get; set; }
        public double PrepareSequenceMs { get; set; }
        public double PrepareTransformsMs { get; set; }
        public double DeformMs { get; set; }
        public double DeformCloneInputsMs { get; set; }
        public double DeformResolveNewPositionsMs { get; set; }
        public double DeformAffinityMs { get; set; }
        public double DeformCenterDeformationMs { get; set; }
        public double DeformEditedSurfaceMs { get; set; }
        public double DeformPropagateTransformsMs { get; set; }
        public double DeformPropagateSurfaceMs { get; set; }
        public double DeformPropagateTotalMs { get; set; }
        public int DeformPropagatedSurfaceFrames { get; set; }
        public double DeformSurfaceEditedComputeWeightsMs { get; set; }
        public double DeformSurfaceEditedBlendVerticesMs { get; set; }
        public double DeformSurfaceEditedResampleMs { get; set; }
        public bool DeformSurfaceEditedUsedCachedWeights { get; set; }
        public double DeformSurfacePropagatedComputeWeightsMs { get; set; }
        public double DeformSurfacePropagatedBlendVerticesMs { get; set; }
        public double DeformSurfacePropagatedResampleMs { get; set; }
        public int DeformSurfacePropagatedCacheMisses { get; set; }
        public int DeformSurfacePropagatedCacheHits { get; set; }
        public double DeformSurfacePropagatedMinCallTotalMs { get; set; }
        public double DeformSurfacePropagatedMaxCallTotalMs { get; set; }
        public int DeformSurfacePropagatedMaxCallFrameIndex { get; set; }
        public double DeformSurfacePropagatedAverageCallTotalMs { get; set; }
        public double WriteBackMs { get; set; }
        public double AdapterTotalMs { get; set; }
        public double CoreTotalMs { get; set; }
        public double UnityApplyMs { get; set; }
        public double TotalMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public string ToLogString(string header)
        {
            var builder = new StringBuilder();
            builder.AppendLine(header);
            builder.AppendLine($"Iteration: {Iteration}");
            builder.AppendLine($"FrameIndex: {FrameIndex}");
            builder.AppendLine($"CenterIndex: {CenterIndex}");
            builder.AppendLine($"AffectedCenters: {AffectedCenterCount}");
            builder.AppendLine("Planner.Selection: sparseAffinity");
            builder.AppendLine("Planner.RadiusIgnored: True");
            builder.AppendLine("Planner.AnchorEffectors: 0");
            builder.AppendLine($"Planner.MovingEffectors: {MovingEffectorCount}");
            builder.AppendLine($"Planner.CandidatePool: {CandidatePoolCount}");
            builder.AppendLine($"Planner.DiscardedCandidates: {DiscardedCandidateCount}");
            builder.AppendLine($"Planner.PatchMinAffinity: {PatchMinAffinity:F4}");
            builder.AppendLine($"Planner.PatchMaxAffinity: {PatchMaxAffinity:F4}");
            builder.AppendLine($"Planner.ShapeElongation: {ShapeElongation:F3}");
            builder.AppendLine($"Planner.TranslationMagnitudeMax: {TranslationMagnitudeMax:F6}");
            builder.AppendLine($"Planner.TranslationMagnitudeAverage: {TranslationMagnitudeAverage:F6}");
            builder.AppendLine($"Cache.ExecutionContextHit: {ExecutionContextCacheHit}");
            builder.AppendLine($"Cache.HydrationState: {CacheHydrationState}");
            builder.AppendLine($"Cache.AffinityAvailableBeforeEnsure: {AffinityWasAvailableBeforeEnsure}");
            builder.AppendLine($"Cache.AffinityCalculatedDuringRun: {AffinityCalculatedDuringRun}");
            builder.AppendLine($"ResolveEffectors: {ResolveEffectorsMs:F2} ms");
            builder.AppendLine($"PrepareSequence: {PrepareSequenceMs:F2} ms");
            builder.AppendLine($"PrepareTransforms: {PrepareTransformsMs:F2} ms");
            builder.AppendLine($"Deform: {DeformMs:F2} ms");
            builder.AppendLine($"Deform.CloneInputs: {DeformCloneInputsMs:F2} ms");
            builder.AppendLine($"Deform.ResolveNewPositions: {DeformResolveNewPositionsMs:F2} ms");
            builder.AppendLine($"Deform.Affinity: {DeformAffinityMs:F2} ms");
            builder.AppendLine($"Deform.CenterDeformation: {DeformCenterDeformationMs:F2} ms");
            builder.AppendLine($"Deform.EditedSurface: {DeformEditedSurfaceMs:F2} ms");
            builder.AppendLine($"Deform.PropagateTransforms: {DeformPropagateTransformsMs:F2} ms");
            builder.AppendLine($"Deform.PropagateSurface: {DeformPropagateSurfaceMs:F2} ms");
            builder.AppendLine($"Deform.PropagateTotal: {DeformPropagateTotalMs:F2} ms");
            builder.AppendLine($"Deform.PropagatedSurfaceFrames: {DeformPropagatedSurfaceFrames}");
            builder.AppendLine($"Deform.TotalAffectedFrames: {TotalAffectedFrames}");
            builder.AppendLine($"Deform.SurfaceEdited.ComputeWeights: {DeformSurfaceEditedComputeWeightsMs:F2} ms");
            builder.AppendLine($"Deform.SurfaceEdited.BlendVertices: {DeformSurfaceEditedBlendVerticesMs:F2} ms");
            builder.AppendLine($"Deform.SurfaceEdited.Resample: {DeformSurfaceEditedResampleMs:F2} ms");
            builder.AppendLine($"Deform.SurfaceEdited.UsedCachedWeights: {DeformSurfaceEditedUsedCachedWeights}");
            builder.AppendLine($"Deform.SurfacePropagated.ComputeWeights: {DeformSurfacePropagatedComputeWeightsMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.BlendVertices: {DeformSurfacePropagatedBlendVerticesMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.Resample: {DeformSurfacePropagatedResampleMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.CacheMisses: {DeformSurfacePropagatedCacheMisses}");
            builder.AppendLine($"Deform.SurfacePropagated.CacheHits: {DeformSurfacePropagatedCacheHits}");
            builder.AppendLine($"Deform.SurfacePropagated.CallTotalMin: {DeformSurfacePropagatedMinCallTotalMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.CallTotalMax: {DeformSurfacePropagatedMaxCallTotalMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.CallTotalAvg: {DeformSurfacePropagatedAverageCallTotalMs:F2} ms");
            builder.AppendLine($"Deform.SurfacePropagated.CallTotalMaxFrame: {DeformSurfacePropagatedMaxCallFrameIndex}");
            builder.AppendLine($"WriteBack: {WriteBackMs:F2} ms");
            builder.AppendLine($"AdapterTotal: {AdapterTotalMs:F2} ms");
            builder.AppendLine($"CoreTotal: {CoreTotalMs:F2} ms");
            builder.AppendLine($"UnityApply: {UnityApplyMs:F2} ms");
            builder.AppendLine($"Total: {TotalMs:F2} ms");

            if (!Success && !string.IsNullOrWhiteSpace(ErrorMessage))
                builder.AppendLine($"Error: {ErrorMessage}");

            return builder.ToString();
        }

        public static InflateDeflateQuickProfile Average(IReadOnlyList<InflateDeflateQuickProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0)
                return new InflateDeflateQuickProfile();

            var average = new InflateDeflateQuickProfile
            {
                Iteration = profiles.Count,
                FrameIndex = profiles[0].FrameIndex,
                CenterIndex = profiles[0].CenterIndex,
                CacheHydrationState = profiles[0].CacheHydrationState,
                Success = true
            };

            foreach (var profile in profiles)
            {
                average.AffectedCenterCount += profile.AffectedCenterCount;
                average.MovingEffectorCount += profile.MovingEffectorCount;
                average.CandidatePoolCount += profile.CandidatePoolCount;
                average.DiscardedCandidateCount += profile.DiscardedCandidateCount;
                average.PatchMinAffinity += profile.PatchMinAffinity;
                average.PatchMaxAffinity += profile.PatchMaxAffinity;
                average.ShapeElongation += profile.ShapeElongation;
                average.TranslationMagnitudeMax += profile.TranslationMagnitudeMax;
                average.TranslationMagnitudeAverage += profile.TranslationMagnitudeAverage;
                average.ExecutionContextCacheHit |= profile.ExecutionContextCacheHit;
                average.AffinityWasAvailableBeforeEnsure |= profile.AffinityWasAvailableBeforeEnsure;
                average.AffinityCalculatedDuringRun |= profile.AffinityCalculatedDuringRun;
                average.TotalAffectedFrames += profile.TotalAffectedFrames;
                average.ResolveEffectorsMs += profile.ResolveEffectorsMs;
                average.PrepareSequenceMs += profile.PrepareSequenceMs;
                average.PrepareTransformsMs += profile.PrepareTransformsMs;
                average.DeformMs += profile.DeformMs;
                average.DeformCloneInputsMs += profile.DeformCloneInputsMs;
                average.DeformResolveNewPositionsMs += profile.DeformResolveNewPositionsMs;
                average.DeformAffinityMs += profile.DeformAffinityMs;
                average.DeformCenterDeformationMs += profile.DeformCenterDeformationMs;
                average.DeformEditedSurfaceMs += profile.DeformEditedSurfaceMs;
                average.DeformPropagateTransformsMs += profile.DeformPropagateTransformsMs;
                average.DeformPropagateSurfaceMs += profile.DeformPropagateSurfaceMs;
                average.DeformPropagateTotalMs += profile.DeformPropagateTotalMs;
                average.DeformPropagatedSurfaceFrames += profile.DeformPropagatedSurfaceFrames;
                average.DeformSurfaceEditedComputeWeightsMs += profile.DeformSurfaceEditedComputeWeightsMs;
                average.DeformSurfaceEditedBlendVerticesMs += profile.DeformSurfaceEditedBlendVerticesMs;
                average.DeformSurfaceEditedResampleMs += profile.DeformSurfaceEditedResampleMs;
                average.DeformSurfaceEditedUsedCachedWeights |= profile.DeformSurfaceEditedUsedCachedWeights;
                average.DeformSurfacePropagatedComputeWeightsMs += profile.DeformSurfacePropagatedComputeWeightsMs;
                average.DeformSurfacePropagatedBlendVerticesMs += profile.DeformSurfacePropagatedBlendVerticesMs;
                average.DeformSurfacePropagatedResampleMs += profile.DeformSurfacePropagatedResampleMs;
                average.DeformSurfacePropagatedCacheMisses += profile.DeformSurfacePropagatedCacheMisses;
                average.DeformSurfacePropagatedCacheHits += profile.DeformSurfacePropagatedCacheHits;
                average.DeformSurfacePropagatedMinCallTotalMs += profile.DeformSurfacePropagatedMinCallTotalMs;
                average.DeformSurfacePropagatedMaxCallTotalMs += profile.DeformSurfacePropagatedMaxCallTotalMs;
                average.DeformSurfacePropagatedAverageCallTotalMs += profile.DeformSurfacePropagatedAverageCallTotalMs;
                average.WriteBackMs += profile.WriteBackMs;
                average.AdapterTotalMs += profile.AdapterTotalMs;
                average.CoreTotalMs += profile.CoreTotalMs;
                average.UnityApplyMs += profile.UnityApplyMs;
                average.TotalMs += profile.TotalMs;
                average.Success &= profile.Success;
            }

            average.AffectedCenterCount /= profiles.Count;
            average.MovingEffectorCount /= profiles.Count;
            average.CandidatePoolCount /= profiles.Count;
            average.DiscardedCandidateCount /= profiles.Count;
            average.PatchMinAffinity /= profiles.Count;
            average.PatchMaxAffinity /= profiles.Count;
            average.ShapeElongation /= profiles.Count;
            average.TranslationMagnitudeMax /= profiles.Count;
            average.TranslationMagnitudeAverage /= profiles.Count;
            average.TotalAffectedFrames /= profiles.Count;
            average.ResolveEffectorsMs /= profiles.Count;
            average.PrepareSequenceMs /= profiles.Count;
            average.PrepareTransformsMs /= profiles.Count;
            average.DeformMs /= profiles.Count;
            average.DeformCloneInputsMs /= profiles.Count;
            average.DeformResolveNewPositionsMs /= profiles.Count;
            average.DeformAffinityMs /= profiles.Count;
            average.DeformCenterDeformationMs /= profiles.Count;
            average.DeformEditedSurfaceMs /= profiles.Count;
            average.DeformPropagateTransformsMs /= profiles.Count;
            average.DeformPropagateSurfaceMs /= profiles.Count;
            average.DeformPropagateTotalMs /= profiles.Count;
            average.DeformPropagatedSurfaceFrames /= profiles.Count;
            average.DeformSurfaceEditedComputeWeightsMs /= profiles.Count;
            average.DeformSurfaceEditedBlendVerticesMs /= profiles.Count;
            average.DeformSurfaceEditedResampleMs /= profiles.Count;
            average.WriteBackMs /= profiles.Count;
            average.AdapterTotalMs /= profiles.Count;
            average.CoreTotalMs /= profiles.Count;
            average.UnityApplyMs /= profiles.Count;
            average.TotalMs /= profiles.Count;
            average.DeformSurfacePropagatedComputeWeightsMs /= profiles.Count;
            average.DeformSurfacePropagatedBlendVerticesMs /= profiles.Count;
            average.DeformSurfacePropagatedResampleMs /= profiles.Count;
            average.DeformSurfacePropagatedCacheMisses /= profiles.Count;
            average.DeformSurfacePropagatedCacheHits /= profiles.Count;
            average.DeformSurfacePropagatedMinCallTotalMs /= profiles.Count;
            average.DeformSurfacePropagatedMaxCallTotalMs /= profiles.Count;
            average.DeformSurfacePropagatedAverageCallTotalMs /= profiles.Count;
            average.DeformSurfacePropagatedMaxCallFrameIndex = profiles[0].DeformSurfacePropagatedMaxCallFrameIndex;

            if (!average.Success)
                average.ErrorMessage = "At least one iteration failed.";

            return average;
        }
    }
}
