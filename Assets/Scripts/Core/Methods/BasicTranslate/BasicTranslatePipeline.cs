using UnityEngine;

namespace TvmVr2.Core.Methods.BasicTranslate
{
    /// <summary>
    /// Coordinates center, temporal and surface deformation for Basic Translate.
    /// </summary>
    public sealed class BasicTranslatePipeline
    {
        /// <summary>
        /// Creates a Basic Translate pipeline.
        /// </summary>
        public BasicTranslatePipeline(
            GaussianCenterDeformer centerDeformer = null,
            KabschSequenceDeformer sequenceDeformer = null,
            NeighborhoodSurfaceDeformer surfaceDeformer = null)
        {
        }

        /// <summary>
        /// Moves one center in the edited frame and propagates the center deformation across the sequence.
        /// </summary>
        public bool ApplyCenterEdit(
            Frame[] frames,
            int centerIndex,
            int frameIndex,
            UnityEngine.Vector3 targetPosition,
            float centerSigma = 1f,
            int sequenceNeighborCount = 4)
        {
            if (frames == null || frames.Length == 0)
                return false;

            if (frameIndex < 0 || frameIndex >= frames.Length)
                return false;

            var targetFrame = frames[frameIndex];
            if (targetFrame?.centers == null || centerIndex < 0 || centerIndex >= targetFrame.centers.Length)
                return false;

            var allCenters = new System.Numerics.Vector3[frames.Length][];
            for (var i = 0; i < frames.Length; i++)
            {
                allCenters[i] = frames[i].centers;
            }

            var numericsTarget = new System.Numerics.Vector3(targetPosition.x, targetPosition.y, targetPosition.z);
            var translation = numericsTarget - frames[frameIndex].centers[centerIndex];
            var centerDeformer = new GaussianCenterDeformer(centerSigma);
            var sequenceDeformer = new KabschSequenceDeformer(sequenceNeighborCount);
            var deformedSequence = sequenceDeformer.DeformSequence(
                centerIndex,
                frameIndex,
                translation,
                allCenters[frameIndex],
                allCenters,
                centerDeformer);

            for (var i = 0; i < frames.Length; i++)
            {
                frames[i].centers = deformedSequence[i];
            }

            return true;
        }

        /// <summary>
        /// Rebuilds mesh vertices from the current center positions.
        /// </summary>
        public bool RebuildSurface(Frame[] frames, int surfaceNeighborCount = 6)
        {
            if (frames == null)
                return false;

            var surfaceDeformer = new NeighborhoodSurfaceDeformer(surfaceNeighborCount);
            for (var i = 0; i < frames.Length; i++)
            {
                frames[i].vertices = surfaceDeformer.DeformSurface(frames[i]);
            }

            return true;
        }
    }
}
