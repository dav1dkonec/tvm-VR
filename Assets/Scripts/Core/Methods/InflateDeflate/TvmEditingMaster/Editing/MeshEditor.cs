using System.Linq;
using System.Numerics;
using TVMEditor.Editing.AffinityCalculation;
using TVMEditor.Editing.CenterDeformation;
using TVMEditor.Editing.Looping;
using TVMEditor.Editing.SurfaceDeformation;
using TVMEditor.Editing.TransformPropagation;
using TVMEditor.Structures;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace TVMEditor.Editing
{
    public class MeshEditor
    {
        public IAffinityCalculation AffinityCalculation { get; set; }
        public ICenterDeformation CenterDeformation { get; set; }
        public ILooping Looping { get; set; }
        public ISurfaceDeformation SurfaceDeformation { get; set; }
        public ITransformPropagation TransformPropagation { get; set; }

        public MeshEditor(
            IAffinityCalculation affinityCalculation,
            ICenterDeformation centerDeformation,
            ILooping looping,
            ISurfaceDeformation surfaceDeformation,
            ITransformPropagation transformPropagation)
        {
            AffinityCalculation = affinityCalculation;
            CenterDeformation = centerDeformation;
            Looping = looping;
            SurfaceDeformation = surfaceDeformation;
            TransformPropagation = transformPropagation;
        }

        public void Deform(
            TriangleMeshSequence sequence,
            Vector3[][] centers,
            int[] indices,
            DualQuaternion[] transformations,
            int frameIndex,
            out TriangleMeshSequence deformedSequence,
            out Vector3[][] deformedCenters)
        {
            Deform(
                sequence,
                centers,
                indices,
                transformations,
                frameIndex,
                out deformedSequence,
                out deformedCenters,
                out _);
        }

        public void Deform(
            TriangleMeshSequence sequence,
            Vector3[][] centers,
            int[] indices,
            DualQuaternion[] transformations,
            int frameIndex,
            out TriangleMeshSequence deformedSequence,
            out Vector3[][] deformedCenters,
            out MeshEditorDeformProfile profile)
        {
            profile = new MeshEditorDeformProfile();
            var totalTimer = Stopwatch.StartNew();
            var stageTimer = Stopwatch.StartNew();
            var customSurface = SurfaceDeformation as CustomSurfaceDeformation;
            customSurface?.ResetProfiling();
            sequence = sequence.Clone();
            centers = (Vector3[][])centers.Clone();
            stageTimer.Stop();
            profile.CloneInputsMs = stageTimer.Elapsed.TotalMilliseconds;

            stageTimer.Restart();
            var newPositions = new Vector3[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                newPositions[i] = transformations[indices[i]].Transform(centers[frameIndex][indices[i]]);
            }
            stageTimer.Stop();
            profile.ResolveNewPositionsMs = stageTimer.Elapsed.TotalMilliseconds;

            stageTimer.Restart();
            if (AffinityCalculation != null && AffinityCalculation.GetCentersAffinity() == null)
                AffinityCalculation.CalculateCentersAffinity(centers);
            stageTimer.Stop();
            profile.AffinityMs = stageTimer.Elapsed.TotalMilliseconds;

            stageTimer.Restart();
            var newCenters = CenterDeformation.DeformCenters(centers[frameIndex], indices, newPositions, ref transformations);
            stageTimer.Stop();
            profile.CenterDeformationMs = stageTimer.Elapsed.TotalMilliseconds;

            stageTimer.Restart();
            sequence.Meshes[frameIndex] = SurfaceDeformation.DeformSurface(
                sequence.Meshes[frameIndex].Vertices,
                sequence.Meshes[frameIndex].Faces,
                centers[frameIndex],
                newCenters,
                frameIndex,
                transformations);
            stageTimer.Stop();
            profile.EditedSurfaceMs = stageTimer.Elapsed.TotalMilliseconds;

            Propagate(sequence, ref centers, frameIndex, centers[frameIndex], newCenters, transformations, profile);
            deformedCenters = centers;
            deformedSequence = sequence;
            ApplyCustomSurfaceProfiles(frameIndex, customSurface, profile);
            totalTimer.Stop();
            profile.TotalMs = totalTimer.Elapsed.TotalMilliseconds;
        }

        public void Propagate(TriangleMeshSequence sequence, ref Vector3[][] centers, int srcIndex, Vector3[] oldCenters, Vector3[] newCenters, DualQuaternion[] transformations)
        {
            Propagate(sequence, ref centers, srcIndex, oldCenters, newCenters, transformations, null);
        }

        public void Propagate(TriangleMeshSequence sequence, ref Vector3[][] centers, int srcIndex, Vector3[] oldCenters, Vector3[] newCenters, DualQuaternion[] transformations, MeshEditorDeformProfile profile)
        {
            var stageTimer = Stopwatch.StartNew();
            var newPropagatedCenters = TransformPropagation.PropagateTransform(
                centers,
                srcIndex,
                oldCenters,
                newCenters,
                transformations,
                out var propagatedTransformations);
            stageTimer.Stop();

            if (profile != null)
                profile.PropagateTransformsMs = stageTimer.Elapsed.TotalMilliseconds;

            stageTimer.Restart();
            var propagatedSurfaceFrames = 0;
            for (var f = 0; f < sequence.Meshes.Length; f++)
            {
                if (srcIndex == f || !TransformPropagation.FrameIsAffected(srcIndex, f))
                    continue;

                propagatedSurfaceFrames++;
                sequence.Meshes[f] = SurfaceDeformation.DeformSurface(
                    sequence.Meshes[f].Vertices,
                    sequence.Meshes[f].Faces,
                    centers[f],
                    newPropagatedCenters[f],
                    f,
                    propagatedTransformations[f]);
            }
            stageTimer.Stop();

            centers = newPropagatedCenters;
            if (profile != null)
            {
                profile.PropagateSurfaceMs = stageTimer.Elapsed.TotalMilliseconds;
                profile.PropagatedSurfaceFrames = propagatedSurfaceFrames;
                profile.PropagateTotalMs = profile.PropagateTransformsMs + profile.PropagateSurfaceMs;
            }
        }

        private static void ApplyCustomSurfaceProfiles(int editedFrameIndex, CustomSurfaceDeformation customSurface, MeshEditorDeformProfile profile)
        {
            if (customSurface == null || profile == null)
                return;

            var callProfiles = customSurface.CallProfiles.ToArray();
            if (callProfiles.Length == 0)
                return;

            var editedProfile = callProfiles.FirstOrDefault(p => p.FrameIndex == editedFrameIndex);
            if (editedProfile != null)
            {
                profile.SurfaceEditedComputeWeightsMs = editedProfile.ComputeWeightsMs;
                profile.SurfaceEditedBlendVerticesMs = editedProfile.BlendVerticesMs;
                profile.SurfaceEditedResampleMs = editedProfile.ResampleMs;
                profile.SurfaceEditedUsedCachedWeights = editedProfile.UsedCachedWeights;
            }

            var propagatedProfiles = callProfiles.Where(p => p.FrameIndex != editedFrameIndex).ToArray();
            if (propagatedProfiles.Length == 0)
                return;

            profile.SurfacePropagatedComputeWeightsMs = propagatedProfiles.Sum(p => p.ComputeWeightsMs);
            profile.SurfacePropagatedBlendVerticesMs = propagatedProfiles.Sum(p => p.BlendVerticesMs);
            profile.SurfacePropagatedResampleMs = propagatedProfiles.Sum(p => p.ResampleMs);
            profile.SurfacePropagatedCacheMisses = propagatedProfiles.Count(p => !p.UsedCachedWeights);
        }
    }
}
